using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Sweeps each finger from open (t=0) to closed (t=1) in discrete steps,
    /// sphere-testing the fingertip against target colliders at each step.
    /// Locks t at the last step before first contact; fully closes if no contact is found.
    /// Kinematic only — no physics forces, no IK, no ArticulationBodies.
    /// </summary>
    public class CurlSweepSolver : IHandPoseSolver
    {
        // Debug output: world position of each finger's probe at solve time (thumb=0 … pinky=4)
        public Vector3[] LastSolveProbePositions { get; private set; } = new Vector3[5];
        // true = finger stopped on contact, false = reached full close with no contact
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

            // Snapshot every joint's local pose before the sweep modifies anything
            var snapshotPos = new Vector3[jointCount];
            var snapshotRot = new Quaternion[jointCount];
            for (int i = 0; i < jointCount; i++)
            {
                snapshotPos[i] = joints[i].localPosition;
                snapshotRot[i] = joints[i].localRotation;
            }

            // Candidate closed poses: closedPose first, then any extras (deduped). Each finger
            // independently keeps whichever candidate ends with its fingertip closest to the target.
            var candidates = BuildCandidates(_ctx);

            var tFinals = new float[5];
            var chosenClosed = new PoseScriptableObject[5];

            try
            {
                for (int fingerIdx = 0; fingerIdx < 5; fingerIdx++)
                {
                    var chain = _ctx.fingerMap.Finger(fingerIdx);
                    if (chain == null || chain.Count == 0)
                    {
                        tFinals[fingerIdx] = 0f;
                        chosenClosed[fingerIdx] = candidates[0];
                        LastSolveProbePositions[fingerIdx] = Vector3.zero;
                        LastSolveContacted[fingerIdx] = false;
                        continue;
                    }

                    // Sample the last N joints (fingertip inward). Multi-sampling catches a
                    // finger that wraps the object even when the tip slips past the side.
                    var tip = chain[chain.Count - 1];
                    // Solver-only probe past the last joint (skeletons with no tip bone): the outermost
                    // sample and the gap anchor when present, else fall back to the last joint.
                    var tipProbe = _ctx.tipProbes != null && fingerIdx < _ctx.tipProbes.Length
                        ? _ctx.tipProbes[fingerIdx] : null;
                    var gapTip = tipProbe ? tipProbe : tip;
                    int samples = Mathf.Clamp(_ctx.samplesPerFinger, 1, chain.Count);
                    int sampleStart = chain.Count - samples;

                    bool bestContacted = false;
                    float bestGap = float.PositiveInfinity;
                    float bestT = 1f;
                    Vector3 bestProbe = gapTip.position;
                    var bestPose = candidates[0];
                    bool anyEvaluated = false;

                    foreach (var cp in candidates)
                    {
                        // Sweep this candidate from open to closed, stopping before first contact.
                        bool contacted = false;
                        float prevT = 0f;
                        for (int step = 1; step <= _ctx.stepCount; step++)
                        {
                            float t = (float)step / _ctx.stepCount;
                            _ctx.hand.SetFingerCurl(fingerIdx, t, _ctx.openPose, cp);

                            if (AnySampleOverlaps(chain, sampleStart, tipProbe, _ctx)) { contacted = true; break; }
                            prevT = t;
                        }

                        float lockedT = contacted ? prevT : 1f;
                        _ctx.hand.SetFingerCurl(fingerIdx, lockedT, _ctx.openPose, cp);
                        Vector3 probeAtLocked = gapTip.position;
                        float gap = TipGap(probeAtLocked, _ctx);

                        // Prefer a candidate that makes contact; among equals, the snugger fingertip.
                        if (!anyEvaluated || IsBetter(contacted, gap, bestContacted, bestGap))
                        {
                            anyEvaluated   = true;
                            bestContacted  = contacted;
                            bestGap        = gap;
                            bestT          = lockedT;
                            bestProbe      = probeAtLocked;
                            bestPose       = cp;
                        }

                        // Reset this finger to open before the next candidate so sweeps are independent.
                        _ctx.hand.SetFingerCurl(fingerIdx, 0f, _ctx.openPose, cp);
                    }

                    tFinals[fingerIdx]              = bestT;
                    chosenClosed[fingerIdx]         = bestPose;
                    LastSolveProbePositions[fingerIdx] = bestProbe;
                    LastSolveContacted[fingerIdx]   = bestContacted;

                    if (_ctx.collectDebug)
                        RecordFingerDebug(fingerIdx, chain, sampleStart, bestT, bestPose, bestContacted, _ctx);
                }
            }
            finally
            {
                // Always restore the hand to its pre-solve state
                for (int i = 0; i < jointCount; i++)
                {
                    var joint = joints[i];
                    if (!joint) continue;
                    joint.localPosition = snapshotPos[i];
                    joint.localRotation = snapshotRot[i];
                }
            }

            if (_ctx.collectDebug) _debug.hasData = true;

            return BuildJointData(_ctx, tFinals, chosenClosed);
        }

        // Re-poses the finger at its locked t and records each sampled joint's world position and
        // nearest surface point/normal into the debug buffer (gizmos only). Single t per finger, so
        // every sampled joint shares the finger's locked t and contact state.
        private void RecordFingerDebug(
            int _fingerIdx,
            List<Transform> _chain,
            int _sampleStart,
            float _t,
            PoseScriptableObject _closedPose,
            bool _contacted,
            HandSolveContext _ctx)
        {
            var dbg = _debug.fingers[_fingerIdx];
            if (_chain == null || _chain.Count == 0)
            {
                dbg.state = FingerSolveState.NoData;
                dbg.probeCount = 0;
                return;
            }

            // Re-apply the chosen pose so recorded positions match the returned result (the candidate
            // loop resets each finger to open after evaluating it).
            _ctx.hand.SetFingerCurl(_fingerIdx, _t, _ctx.openPose, _closedPose ? _closedPose : _ctx.closedPose);

            int sampleStart = Mathf.Clamp(_sampleStart, 0, _chain.Count - 1);
            int count = _chain.Count - sampleStart;
            dbg.EnsureCapacity(count);
            dbg.probeRadius  = _ctx.probeRadius;
            dbg.chosenClosed = _closedPose ? _closedPose : _ctx.closedPose;
            dbg.state        = _contacted ? FingerSolveState.Contacted : FingerSolveState.NoContact;

            int w = 0;
            for (int j = sampleStart; j < _chain.Count; j++)
            {
                var joint = _chain[j];
                Vector3 pos = joint ? joint.position : Vector3.zero;
                Vector3 surface = NearestSurfacePoint(pos, _ctx, out bool hasSurface);
                dbg.probes[w++] = new JointProbe
                {
                    position      = pos,
                    lockedT       = _t,
                    contacted     = _contacted,
                    contactPoint  = hasSurface ? surface : pos,
                    contactNormal = hasSurface ? (pos - surface).normalized : Vector3.zero,
                };
            }
            dbg.probeCount = w;
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

        // closedPose is always candidate 0; extra closedPoses are appended (deduped, non-null).
        private static List<PoseScriptableObject> BuildCandidates(HandSolveContext _ctx)
        {
            var list = new List<PoseScriptableObject> { _ctx.closedPose };
            if (_ctx.closedPoses != null)
                foreach (var cp in _ctx.closedPoses)
                    if (cp && !list.Contains(cp)) list.Add(cp);
            return list;
        }

        // Contact beats no-contact; among equals, the smaller fingertip gap wins.
        private static bool IsBetter(bool _contacted, float _gap, bool _bestContacted, float _bestGap)
        {
            if (_contacted != _bestContacted) return _contacted;
            return _gap < _bestGap;
        }

        // Distance from the fingertip to the nearest point on any target collider.
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

        #region Private Helpers

        // True if any of the sampled joints (sampleStart..tip), or the fingertip probe past the last joint,
        // overlaps the target this step.
        private static bool AnySampleOverlaps(List<Transform> _chain, int _sampleStart, Transform _tip, HandSolveContext _ctx)
        {
            for (int j = _sampleStart; j < _chain.Count; j++)
            {
                var joint = _chain[j];
                if (joint && OverlapsTarget(joint.position, _ctx)) return true;
            }
            if (_tip && OverlapsTarget(_tip.position, _ctx)) return true;
            return false;
        }

        private static bool OverlapsTarget(Vector3 _center, HandSolveContext _ctx)
        {
            if (_ctx.targetColliders == null || _ctx.targetColliders.Length == 0) return false;

            var hits = Physics.OverlapSphere(_center, _ctx.probeRadius, _ctx.targetMask, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                for (int j = 0; j < _ctx.targetColliders.Length; j++)
                {
                    if (hit == _ctx.targetColliders[j]) return true;
                }
            }
            return false;
        }

        private static PoseScriptableObject.JointData[] BuildJointData(HandSolveContext _ctx, float[] _tFinals, PoseScriptableObject[] _chosenClosed)
        {
            var openDict = new Dictionary<string, PoseScriptableObject.JointData>(_ctx.openPose.joints.Length);
            foreach (var jd in _ctx.openPose.joints) openDict[jd.jointName] = jd;

            // Each finger may use a different closed pose; cache the lookup per pose.
            var closedDicts = new Dictionary<PoseScriptableObject, Dictionary<string, PoseScriptableObject.JointData>>();

            var result = new List<PoseScriptableObject.JointData>();
            var seen = new HashSet<string>();

            for (int i = 0; i < 5; i++)
            {
                float t = _tFinals[i];
                var chain = _ctx.fingerMap.Finger(i);
                if (chain == null) continue;

                var closedPose = _chosenClosed[i] ? _chosenClosed[i] : _ctx.closedPose;
                if (!closedPose) continue;

                if (!closedDicts.TryGetValue(closedPose, out var closedDict))
                {
                    closedDict = new Dictionary<string, PoseScriptableObject.JointData>(closedPose.joints.Length);
                    foreach (var jd in closedPose.joints) closedDict[jd.jointName] = jd;
                    closedDicts[closedPose] = closedDict;
                }

                foreach (var joint in chain)
                {
                    if (!joint) continue;
                    string name = joint.name;
                    if (!seen.Add(name)) continue; // skip if already added (shared bone guard)

                    if (!openDict.TryGetValue(name, out var open)) continue;
                    if (!closedDict.TryGetValue(name, out var closed)) continue;

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
            if (_ctx == null)
            {
                Debug.LogError("[CurlSweepSolver] HandSolveContext is null.");
                return false;
            }
            if (!_ctx.hand)
            {
                Debug.LogError("[CurlSweepSolver] HandAnimator is null.");
                return false;
            }
            if (!_ctx.openPose)
            {
                Debug.LogError("[CurlSweepSolver] openPose is null.");
                return false;
            }
            if (!_ctx.closedPose)
            {
                Debug.LogError("[CurlSweepSolver] closedPose is null.");
                return false;
            }
            if (_ctx.fingerMap == null)
            {
                Debug.LogError("[CurlSweepSolver] fingerMap is null.");
                return false;
            }
            if (_ctx.hand.currentJoints == null || _ctx.hand.currentJoints.Count == 0)
            {
                Debug.LogError("[CurlSweepSolver] HandAnimator has no joints — did SetBones() run?");
                return false;
            }
            return true;
        }

        #endregion
    }
}
