using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Per-finger contact posing in two passes. First a GROSS close curls the whole finger as a unit
    /// (every joint shares one blend t) until any part first touches the target — placing a natural
    /// uniform curve up to the contact. Then a DISTAL WRAP curls each joint past the contact further,
    /// one at a time, until its own segment meets the surface, so the fingertip wraps down onto a
    /// surface even when the knuckle is already resting against it — something a single blend value per
    /// finger cannot do. A joint past the grip that reaches nothing follows a gentle spiral rather than
    /// snapping to a fist (no distal "claw"). When multiple closed poses are supplied, each finger keeps
    /// whichever candidate ends with its fingertip closest to the target.
    ///
    /// Kinematic only — sphere tests + direct transform writes. No physics, no IK, no ArticulationBodies.
    /// </summary>
    public class ProgressiveCurlSolver : IHandPoseSolver
    {
        public Vector3[] LastSolveProbePositions { get; private set; } = new Vector3[5];
        public bool[] LastSolveContacted { get; private set; } = new bool[5];

        // Per-finger debug telemetry, filled only when _ctx.collectDebug. Reused across solves.
        private readonly HandSolveDebug _debug = new HandSolveDebug();
        public HandSolveDebug LastSolveDebug { get; private set; }

        public PoseScriptableObject.JointData[] Solve(HandSolveContext _ctx)
        {
            if (!Validate(_ctx)) return null;

            if (_ctx.collectDebug) { _debug.BeginSolve(); LastSolveDebug = _debug; }
            else LastSolveDebug = null;

            var joints = _ctx.hand.currentJoints;
            int jointCount = joints.Count;

            var snapshotPos = new Vector3[jointCount];
            var snapshotRot = new Quaternion[jointCount];
            for (int i = 0; i < jointCount; i++)
            {
                snapshotPos[i] = joints[i].localPosition;
                snapshotRot[i] = joints[i].localRotation;
            }

            var candidates = BuildCandidates(_ctx);
            var openDict = ToDict(_ctx.openPose);
            var closedDicts = new Dictionary<PoseScriptableObject, Dictionary<string, PoseScriptableObject.JointData>>();

            var fingerJointT  = new float[5][];                 // per-joint curl amount, indexed to the finger chain
            var fingerClosed  = new PoseScriptableObject[5];    // closed pose chosen per finger

            // Debug scratch (only when recording): per-joint contact for the current/winning candidate.
            bool[] jointHitScratch = null;
            bool[] bestJointHit    = null;
            if (_ctx.collectDebug)
            {
                int maxChain = 0;
                for (int f = 0; f < 5; f++)
                {
                    var c = _ctx.fingerMap.Finger(f);
                    if (c != null && c.Count > maxChain) maxChain = c.Count;
                }
                jointHitScratch = new bool[maxChain];
                bestJointHit    = new bool[maxChain];
            }

            try
            {
                for (int fingerIdx = 0; fingerIdx < 5; fingerIdx++)
                {
                    var chain = _ctx.fingerMap.Finger(fingerIdx);
                    var cal = PerFingerSolveSettings.Get(_ctx.fingerSettings, fingerIdx);
                    // A disabled finger is skipped entirely: no sweep, no joints in the result, so it
                    // keeps whatever pose its driver applies (authored pose on the grab path).
                    if (chain == null || chain.Count == 0 || !cal.solve)
                    {
                        fingerJointT[fingerIdx] = null;
                        fingerClosed[fingerIdx] = candidates[0];
                        LastSolveProbePositions[fingerIdx] = Vector3.zero;
                        LastSolveContacted[fingerIdx] = false;
                        continue;
                    }

                    var tip = chain[chain.Count - 1];
                    // Solver-only probe past the last joint (skeletons with no tip bone). When present it is
                    // the outermost contact point and the gap is measured from it; else fall back to the joint.
                    var tipProbe = TipProbe(_ctx, fingerIdx);
                    var gapTip = tipProbe ? tipProbe : tip;

                    bool bestContacted = false;
                    float bestGap = float.PositiveInfinity;
                    float[] bestT = null;
                    var bestPose = candidates[0];
                    Vector3 bestProbe = gapTip.position;
                    bool anyEvaluated = false;

                    foreach (var cp in candidates)
                    {
                        if (!closedDicts.TryGetValue(cp, out var closedDict))
                        {
                            closedDict = ToDict(cp);
                            closedDicts[cp] = closedDict;
                        }

                        var tj = CurlFinger(_ctx, cal, chain, tipProbe, openDict, closedDict, out bool contacted, jointHitScratch);
                        float gap = TipGap(gapTip.position, _ctx);

                        if (!anyEvaluated || IsBetter(contacted, gap, bestContacted, bestGap))
                        {
                            anyEvaluated  = true;
                            bestContacted = contacted;
                            bestGap       = gap;
                            bestT         = tj;
                            bestPose      = cp;
                            bestProbe     = gapTip.position;
                            if (_ctx.collectDebug)
                                System.Array.Copy(jointHitScratch, bestJointHit, chain.Count);
                        }
                    }

                    // A finger that touched nothing rests in the dedicated relaxed pose when one is
                    // supplied (t=1 toward it = exactly the authored rest shape), instead of a single
                    // curl value toward the fist.
                    if (!bestContacted && _ctx.relaxedPose && bestT != null)
                    {
                        bestPose = _ctx.relaxedPose;
                        for (int i = 0; i < bestT.Length; i++) bestT[i] = 1f;
                    }

                    fingerJointT[fingerIdx] = bestT;
                    fingerClosed[fingerIdx] = bestPose;
                    LastSolveProbePositions[fingerIdx] = bestProbe;
                    LastSolveContacted[fingerIdx] = bestContacted;

                    if (_ctx.collectDebug)
                        RecordFingerDebug(fingerIdx, chain, tipProbe, bestT, bestJointHit, bestPose, bestContacted,
                                          openDict, closedDicts, _ctx, cal);
                }
            }
            finally
            {
                for (int i = 0; i < jointCount; i++)
                {
                    var joint = joints[i];
                    if (!joint) continue;
                    joint.localPosition = snapshotPos[i];
                    joint.localRotation = snapshotRot[i];
                }
            }

            if (_ctx.collectDebug) _debug.hasData = true;

            return BuildJointData(_ctx, openDict, closedDicts, fingerJointT, fingerClosed);
        }

        #region Solve helpers

        // Poses one finger in two passes:
        //   A) Gross close — curl the whole finger as a unit (every joint shares one t) until any part
        //      first contacts. This places a natural uniform curve up to the contact, instead of
        //      sweeping the base alone and fisting it when the object sits further out ("base-slam").
        //   B) Distal wrap — past the contact joint, curl each remaining joint further until ITS own
        //      segment meets the surface, so the fingertip wraps onto/around the object.
        // Leaves the finger transforms at the solved pose and returns per-joint t. Kinematic only.
        // Per-finger calibration (_cal): probeRadiusScale scales every contact test, maxCurl hard-caps
        // every t this finger can reach (anti-overshoot), pressBias shifts contacted locks into/off the
        // surface (clamped to [0, maxCurl]).
        private float[] CurlFinger(
            HandSolveContext _ctx,
            FingerSolveCalibration _cal,
            List<Transform> _chain,
            Transform _tip,           // solver-only fingertip probe past the last joint, or null
            Dictionary<string, PoseScriptableObject.JointData> _openDict,
            Dictionary<string, PoseScriptableObject.JointData> _closedDict,
            out bool _contacted,
            bool[] _jointHit = null) // optional per-joint contact out-buffer (debug only)
        {
            int n = _chain.Count;
            var tj = new float[n];
            _contacted = false;
            if (_jointHit != null)
                for (int i = 0; i < n && i < _jointHit.Length; i++) _jointHit[i] = false;

            float follow  = Mathf.Max(0f, _ctx.distalFollowCurl);
            float radius  = _ctx.probeRadius * _cal.probeRadiusScale;
            float maxCurl = Mathf.Clamp01(_cal.maxCurl);

            // ── Pass A — gross close ──────────────────────────────────────────────────────────
            ApplyAll(_chain, _openDict, _closedDict, 0f, _ctx.jointLimits); // open, independent of the previous candidate

            float tGross = 0f;
            int contactJoint = -1;
            float prevT = 0f;
            for (int step = 1; step <= _ctx.stepCount; step++)
            {
                float t = maxCurl * step / _ctx.stepCount;
                ApplyAll(_chain, _openDict, _closedDict, t, _ctx.jointLimits);

                if (FirstContactingJoint(_chain, _tip, _ctx, radius) >= 0)
                {
                    // Refine the uniform t between prevT (whole finger clear) and t (something touches).
                    float lo = prevT, hi = t;
                    for (int k = 0; k < RefineIterations; k++)
                    {
                        float mid = (lo + hi) * 0.5f;
                        ApplyAll(_chain, _openDict, _closedDict, mid, _ctx.jointLimits);
                        if (FirstContactingJoint(_chain, _tip, _ctx, radius) >= 0) hi = mid; else lo = mid;
                    }
                    // Identify the touching joint at the just-contact pose (hi), then settle at lo (clear).
                    ApplyAll(_chain, _openDict, _closedDict, hi, _ctx.jointLimits);
                    contactJoint = FirstContactingJoint(_chain, _tip, _ctx, radius);
                    tGross = Mathf.Clamp(lo + _cal.pressBias, 0f, maxCurl);
                    ApplyAll(_chain, _openDict, _closedDict, tGross, _ctx.jointLimits);
                    break;
                }
                prevT = t;
            }

            // Touched nothing anywhere → relax to a gentle rest curl (natural, not a fist/claw).
            if (contactJoint < 0)
            {
                float rest = Mathf.Min(Mathf.Clamp01(_ctx.noContactCurl), maxCurl);
                for (int i = 0; i < n; i++)
                {
                    tj[i] = rest;
                    ApplyJoint(_chain[i], _openDict, _closedDict, rest, _ctx.jointLimits);
                }
                return tj;
            }

            _contacted = true;

            // Joints up to and including the contact joint hold the gross curl: they form the natural
            // curve up to where the finger meets the object, and the contact joint sits against it.
            for (int i = 0; i <= contactJoint && i < n; i++) tj[i] = tGross; // already applied at tGross
            if (_jointHit != null && contactJoint < _jointHit.Length) _jointHit[contactJoint] = true;

            // ── Pass B — distal wrap ──────────────────────────────────────────────────────────
            for (int i = contactJoint + 1; i < n; i++)
            {
                float tStart = tj[i - 1]; // continue from the previous joint's curl (monotonic spiral)
                bool isLeaf = (i == n - 1);

                // A leaf with NO tip probe is degenerate: rotating it moves nothing we can sphere-test.
                // Test its pivot once for the contact gate and continue the curl (no real sweep possible).
                // With a tip probe the leaf is sweepable (rotating it moves the tip), so fall through.
                if (isLeaf && !_tip)
                {
                    bool leafHit = SegmentOverlaps(_chain, i, i, _ctx, radius);
                    tj[i] = leafHit ? Mathf.Clamp(tStart + _cal.pressBias, 0f, maxCurl)
                                    : Mathf.Min(maxCurl, tStart + follow);
                    ApplyJoint(_chain[i], _openDict, _closedDict, tj[i], _ctx.jointLimits);
                    if (_jointHit != null && i < _jointHit.Length) _jointHit[i] = leafHit;
                    continue;
                }

                ApplyJoint(_chain[i], _openDict, _closedDict, tStart, _ctx.jointLimits);

                bool jointHit;
                if (WrapOverlaps(_chain, i, _tip, _ctx, radius))
                {
                    jointHit = true; // already touching at the start curl — lock there
                    tj[i] = Mathf.Clamp(tStart + _cal.pressBias, 0f, maxCurl);
                }
                else
                {
                    float lockedT = tStart;
                    jointHit = false;
                    for (int step = 1; step <= _ctx.stepCount; step++)
                    {
                        float t = Mathf.Lerp(tStart, maxCurl, (float)step / _ctx.stepCount);
                        ApplyJoint(_chain[i], _openDict, _closedDict, t, _ctx.jointLimits);
                        if (WrapOverlaps(_chain, i, _tip, _ctx, radius))
                        {
                            float lo = lockedT, hi = t;
                            for (int k = 0; k < RefineIterations; k++)
                            {
                                float mid = (lo + hi) * 0.5f;
                                ApplyJoint(_chain[i], _openDict, _closedDict, mid, _ctx.jointLimits);
                                if (WrapOverlaps(_chain, i, _tip, _ctx, radius)) hi = mid; else lo = mid;
                            }
                            lockedT = lo;
                            jointHit = true;
                            break;
                        }
                        lockedT = t;
                    }
                    // Contact → snug lock (plus press bias); no contact → gentle follow spiral.
                    tj[i] = jointHit ? Mathf.Clamp(lockedT + _cal.pressBias, 0f, maxCurl)
                                     : Mathf.Min(maxCurl, tStart + follow);
                }
                ApplyJoint(_chain[i], _openDict, _closedDict, tj[i], _ctx.jointLimits);
                if (_jointHit != null && i < _jointHit.Length) _jointHit[i] = jointHit;
            }

            return tj;
        }

        // Curl every joint in the finger to the same blend t (the uniform gross close).
        private static void ApplyAll(
            List<Transform> _chain,
            Dictionary<string, PoseScriptableObject.JointData> _openDict,
            Dictionary<string, PoseScriptableObject.JointData> _closedDict,
            float _t,
            HandJointLimits _limits)
        {
            for (int i = 0; i < _chain.Count; i++)
                ApplyJoint(_chain[i], _openDict, _closedDict, _t, _limits);
        }

        // Index of the most-proximal joint whose driven segment overlaps the target, or -1 if none.
        private static int FirstContactingJoint(List<Transform> _chain, Transform _tip, HandSolveContext _ctx, float _radius)
        {
            int n = _chain.Count;
            for (int i = 0; i < n; i++)
                if (WrapOverlaps(_chain, i, _tip, _ctx, _radius)) return i;
            return -1;
        }

        // Does the segment driven by curling joint i overlap the target? For an interior joint that is the
        // segment toward joint i+1; for the last joint WITH a tip probe it is the segment leaf-pivot→tip (so
        // the fingertip is what seats on the surface); for the last joint without a tip it is the pivot only.
        private static bool WrapOverlaps(List<Transform> _chain, int _i, Transform _tip, HandSolveContext _ctx, float _radius)
        {
            int n = _chain.Count;
            if (_i == n - 1)
                return _tip ? TipSegmentOverlaps(_chain[n - 1], _tip, _ctx, _radius)
                            : SegmentOverlaps(_chain, _i, _i, _ctx, _radius);
            return SegmentOverlaps(_chain, _i, _i + 1, _ctx, _radius);
        }

        // The finger's tip probe from the context (supplied by HandDynamicPoses), or null.
        private static Transform TipProbe(HandSolveContext _ctx, int _fingerIdx) =>
            _ctx.tipProbes != null && _fingerIdx < _ctx.tipProbes.Length ? _ctx.tipProbes[_fingerIdx] : null;

        // Tests the segment from the last joint's pivot out to the tip probe: the tip endpoint plus
        // samplesPerSegment-1 evenly spaced interior points (2 = the original midpoint + endpoint).
        private static bool TipSegmentOverlaps(Transform _leaf, Transform _tip, HandSolveContext _ctx, float _radius)
        {
            if (!_tip) return false;
            if (OverlapsTarget(_tip.position, _ctx, _radius)) return true;
            if (!_leaf) return false;
            int s = Mathf.Max(1, _ctx.samplesPerSegment);
            for (int k = 1; k < s; k++)
                if (OverlapsTarget(Vector3.Lerp(_leaf.position, _tip.position, (float)k / s), _ctx, _radius)) return true;
            return false;
        }

        // Re-poses the finger at its chosen per-joint t and records each joint's world position,
        // locked t, contact flag, and nearest surface point/normal into the debug buffer (gizmos only).
        private void RecordFingerDebug(
            int _fingerIdx,
            List<Transform> _chain,
            Transform _tip,
            float[] _tj,
            bool[] _jointHit,
            PoseScriptableObject _closedPose,
            bool _contacted,
            Dictionary<string, PoseScriptableObject.JointData> _openDict,
            Dictionary<PoseScriptableObject, Dictionary<string, PoseScriptableObject.JointData>> _closedDicts,
            HandSolveContext _ctx,
            FingerSolveCalibration _cal)
        {
            var dbg = _debug.fingers[_fingerIdx];
            if (_chain == null || _chain.Count == 0 || _tj == null)
            {
                dbg.state = FingerSolveState.NoData;
                dbg.probeCount = 0;
                return;
            }

            var closedPose = _closedPose ? _closedPose : _ctx.closedPose;
            if (!_closedDicts.TryGetValue(closedPose, out var closedDict))
            {
                closedDict = ToDict(closedPose);
                _closedDicts[closedPose] = closedDict;
            }

            int n = _chain.Count;
            bool hasTip = _tip;
            int total = hasTip ? n + 1 : n;   // append the tip probe as one extra sphere
            dbg.EnsureCapacity(total);
            dbg.probeRadius  = _ctx.probeRadius * _cal.probeRadiusScale;
            dbg.chosenClosed = closedPose;
            dbg.state        = _contacted ? FingerSolveState.Contacted : FingerSolveState.RelaxedFist;

            // Re-pose at the chosen per-joint t so recorded world positions match the returned pose
            // (the candidate loop may have left the chain at a different candidate's shape).
            for (int i = 0; i < n; i++)
                ApplyJoint(_chain[i], _openDict, closedDict, i < _tj.Length ? _tj[i] : 1f, _ctx.jointLimits);

            for (int i = 0; i < n; i++)
            {
                var joint = _chain[i];
                Vector3 pos = joint ? joint.position : Vector3.zero;
                Vector3 surface = NearestSurfacePoint(pos, _ctx, out bool hasSurface);
                dbg.probes[i] = new JointProbe
                {
                    position      = pos,
                    lockedT       = i < _tj.Length ? _tj[i] : 1f,
                    contacted     = _jointHit != null && i < _jointHit.Length && _jointHit[i],
                    contactPoint  = hasSurface ? surface : pos,
                    contactNormal = hasSurface ? (pos - surface).normalized : Vector3.zero,
                };
            }

            if (hasTip)
            {
                // The probe moved with the leaf during the re-pose above (it is its child), so its world
                // position is current. Reuse the leaf's lock-t/contact flag for the label.
                Vector3 tipPos = _tip.position;
                Vector3 surface = NearestSurfacePoint(tipPos, _ctx, out bool hasSurface);
                dbg.probes[n] = new JointProbe
                {
                    position      = tipPos,
                    lockedT       = n - 1 < _tj.Length ? _tj[n - 1] : 1f,
                    contacted     = _jointHit != null && n - 1 < _jointHit.Length && _jointHit[n - 1],
                    contactPoint  = hasSurface ? surface : tipPos,
                    contactNormal = hasSurface ? (tipPos - surface).normalized : Vector3.zero,
                };
            }
            dbg.probeCount = total;
        }

        // Nearest point on any target collider to _p (approx surface contact point for the gizmos).
        private static Vector3 NearestSurfacePoint(Vector3 _p, HandSolveContext _ctx, out bool _hasSurface)
        {
            _hasSurface = false;
            Vector3 best = _p;
            float min = float.PositiveInfinity;
            if (_ctx.targetColliders == null) return best;
            foreach (var col in _ctx.targetColliders)
            {
                if (!col || !SupportsClosestPoint(col)) continue;
                Vector3 cp = col.ClosestPoint(_p);
                float d = (cp - _p).sqrMagnitude;
                if (d < min) { min = d; best = cp; _hasSurface = true; }
            }
            return best;
        }

        // Collider.ClosestPoint only supports primitives and CONVEX mesh colliders; calling it on a
        // non-convex mesh or terrain logs a Unity error per call and returns the query point (gap 0),
        // which would both spam the console and corrupt candidate selection on environment geometry.
        private static bool SupportsClosestPoint(Collider _col) =>
            _col is BoxCollider || _col is SphereCollider || _col is CapsuleCollider ||
            (_col is MeshCollider mc && mc.convex);

        private const int RefineIterations = 5;

        // Sets a single joint's local pose to the lerp between its open and closed pose at t,
        // clamped into the optional per-joint limits (null = unclamped).
        private static void ApplyJoint(
            Transform _joint,
            Dictionary<string, PoseScriptableObject.JointData> _openDict,
            Dictionary<string, PoseScriptableObject.JointData> _closedDict,
            float _t,
            HandJointLimits _limits)
        {
            if (!_joint) return;
            string name = _joint.name;
            if (!_openDict.TryGetValue(name, out var open)) return;
            if (!_closedDict.TryGetValue(name, out var closed)) return;
            if (_limits) _t = _limits.Clamp(name, _t);

            _joint.localPosition = Vector3.Lerp(open.localPosition, closed.localPosition, _t);
            _joint.localRotation = Quaternion.Slerp(open.localRotation, closed.localRotation, _t);
        }

        // Tests the segment driven by joint i against the target: the segment endpoint plus
        // samplesPerSegment-1 evenly spaced interior points (2 = the original midpoint + endpoint;
        // higher catches thin geometry slipping between samples so edge-wraps read as a drape).
        private static bool SegmentOverlaps(List<Transform> _chain, int _i, int _probeIdx, HandSolveContext _ctx, float _radius)
        {
            var a = _chain[_i];
            var b = _chain[_probeIdx];
            if (!b) return false;

            if (OverlapsTarget(b.position, _ctx, _radius)) return true;
            if (!a || _probeIdx == _i) return false;
            int s = Mathf.Max(1, _ctx.samplesPerSegment);
            for (int k = 1; k < s; k++)
                if (OverlapsTarget(Vector3.Lerp(a.position, b.position, (float)k / s), _ctx, _radius)) return true;
            return false;
        }

        private static bool IsBetter(bool _contacted, float _gap, bool _bestContacted, float _bestGap)
        {
            if (_contacted != _bestContacted) return _contacted;
            return _gap < _bestGap;
        }

        private static float TipGap(Vector3 _tipPos, HandSolveContext _ctx)
        {
            if (_ctx.targetColliders == null || _ctx.targetColliders.Length == 0)
                return float.PositiveInfinity;

            float min = float.PositiveInfinity;
            foreach (var col in _ctx.targetColliders)
            {
                if (!col || !SupportsClosestPoint(col)) continue;
                float d = Vector3.Distance(_tipPos, col.ClosestPoint(_tipPos));
                if (d < min) min = d;
            }
            return min;
        }

        private static bool OverlapsTarget(Vector3 _center, HandSolveContext _ctx, float _radius)
        {
            if (_ctx.targetColliders == null || _ctx.targetColliders.Length == 0) return false;

            var hits = Physics.OverlapSphere(_center, _radius, _ctx.targetMask, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
                for (int j = 0; j < _ctx.targetColliders.Length; j++)
                    if (hit == _ctx.targetColliders[j]) return true;
            return false;
        }

        private static List<PoseScriptableObject> BuildCandidates(HandSolveContext _ctx)
        {
            var list = new List<PoseScriptableObject> { _ctx.closedPose };
            if (_ctx.closedPoses != null)
                foreach (var cp in _ctx.closedPoses)
                    if (cp && !list.Contains(cp)) list.Add(cp);
            return list;
        }

        private static Dictionary<string, PoseScriptableObject.JointData> ToDict(PoseScriptableObject _pose)
        {
            var dict = new Dictionary<string, PoseScriptableObject.JointData>(_pose.joints.Length);
            foreach (var jd in _pose.joints) dict[jd.jointName] = jd;
            return dict;
        }

        private static PoseScriptableObject.JointData[] BuildJointData(
            HandSolveContext _ctx,
            Dictionary<string, PoseScriptableObject.JointData> _openDict,
            Dictionary<PoseScriptableObject, Dictionary<string, PoseScriptableObject.JointData>> _closedDicts,
            float[][] _fingerJointT,
            PoseScriptableObject[] _fingerClosed)
        {
            var result = new List<PoseScriptableObject.JointData>();
            var seen = new HashSet<string>();

            for (int f = 0; f < 5; f++)
            {
                var chain = _ctx.fingerMap.Finger(f);
                var tj = _fingerJointT[f];
                if (chain == null || tj == null) continue;

                var closedPose = _fingerClosed[f] ? _fingerClosed[f] : _ctx.closedPose;
                if (!closedPose) continue;
                if (!_closedDicts.TryGetValue(closedPose, out var closedDict))
                {
                    closedDict = ToDict(closedPose);
                    _closedDicts[closedPose] = closedDict;
                }

                for (int i = 0; i < chain.Count; i++)
                {
                    var joint = chain[i];
                    if (!joint) continue;
                    string name = joint.name;
                    if (!seen.Add(name)) continue;

                    if (!_openDict.TryGetValue(name, out var open)) continue;
                    if (!closedDict.TryGetValue(name, out var closed)) continue;

                    float t = i < tj.Length ? tj[i] : 1f;
                    if (_ctx.jointLimits) t = _ctx.jointLimits.Clamp(name, t);
                    result.Add(new PoseScriptableObject.JointData
                    {
                        jointName     = name,
                        localPosition = Vector3.Lerp(open.localPosition, closed.localPosition, t),
                        localRotation = Quaternion.Slerp(open.localRotation, closed.localRotation, t)
                    });
                }
            }

            return result.ToArray();
        }

        private static bool Validate(HandSolveContext _ctx)
        {
            if (_ctx == null) { Debug.LogError("[ProgressiveCurlSolver] HandSolveContext is null."); return false; }
            if (!_ctx.hand) { Debug.LogError("[ProgressiveCurlSolver] HandAnimator is null."); return false; }
            if (!_ctx.openPose) { Debug.LogError("[ProgressiveCurlSolver] openPose is null."); return false; }
            if (!_ctx.closedPose) { Debug.LogError("[ProgressiveCurlSolver] closedPose is null."); return false; }
            if (_ctx.fingerMap == null) { Debug.LogError("[ProgressiveCurlSolver] fingerMap is null."); return false; }
            if (_ctx.hand.currentJoints == null || _ctx.hand.currentJoints.Count == 0)
            {
                Debug.LogError("[ProgressiveCurlSolver] HandAnimator has no joints — did SetBones() run?");
                return false;
            }
            return true;
        }

        #endregion
    }
}
