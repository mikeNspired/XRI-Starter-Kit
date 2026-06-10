using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Per-joint progressive curl. For each finger, curls one joint at a time from the base outward:
    /// a joint advances toward its closed pose until the segment it drives contacts the target, then
    /// it freezes and the next (more distal) joint continues. This lets the fingertip wrap down onto a
    /// surface even when the knuckle is already resting against it — something a single blend value per
    /// finger cannot do. When multiple closed poses are supplied, each finger keeps whichever candidate
    /// ends with its fingertip closest to the target.
    ///
    /// Kinematic only — sphere tests + direct transform writes. No physics, no IK, no ArticulationBodies.
    /// </summary>
    public class ProgressiveCurlSolver : IHandPoseSolver
    {
        public Vector3[] LastSolveProbePositions { get; private set; } = new Vector3[5];
        public bool[] LastSolveContacted { get; private set; } = new bool[5];

        public PoseScriptableObject.JointData[] Solve(HandSolveContext _ctx)
        {
            if (!Validate(_ctx)) return null;

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

            try
            {
                for (int fingerIdx = 0; fingerIdx < 5; fingerIdx++)
                {
                    var chain = _ctx.fingerMap.Finger(fingerIdx);
                    if (chain == null || chain.Count == 0)
                    {
                        fingerJointT[fingerIdx] = null;
                        fingerClosed[fingerIdx] = candidates[0];
                        LastSolveProbePositions[fingerIdx] = Vector3.zero;
                        LastSolveContacted[fingerIdx] = false;
                        continue;
                    }

                    var tip = chain[chain.Count - 1];

                    bool bestContacted = false;
                    float bestGap = float.PositiveInfinity;
                    float[] bestT = null;
                    var bestPose = candidates[0];
                    Vector3 bestProbe = tip.position;
                    bool anyEvaluated = false;

                    foreach (var cp in candidates)
                    {
                        if (!closedDicts.TryGetValue(cp, out var closedDict))
                        {
                            closedDict = ToDict(cp);
                            closedDicts[cp] = closedDict;
                        }

                        var tj = CurlFinger(_ctx, chain, openDict, closedDict, out bool contacted);
                        float gap = TipGap(tip.position, _ctx);

                        if (!anyEvaluated || IsBetter(contacted, gap, bestContacted, bestGap))
                        {
                            anyEvaluated  = true;
                            bestContacted = contacted;
                            bestGap       = gap;
                            bestT         = tj;
                            bestPose      = cp;
                            bestProbe     = tip.position;
                        }
                    }

                    fingerJointT[fingerIdx] = bestT;
                    fingerClosed[fingerIdx] = bestPose;
                    LastSolveProbePositions[fingerIdx] = bestProbe;
                    LastSolveContacted[fingerIdx] = bestContacted;
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

            return BuildJointData(_ctx, openDict, closedDicts, fingerJointT, fingerClosed);
        }

        #region Solve helpers

        // Curls one finger joint-by-joint (base→tip), freezing each joint when the segment it drives
        // contacts the target. Leaves the finger transforms at the solved pose and returns per-joint t.
        private float[] CurlFinger(
            HandSolveContext _ctx,
            List<Transform> _chain,
            Dictionary<string, PoseScriptableObject.JointData> _openDict,
            Dictionary<string, PoseScriptableObject.JointData> _closedDict,
            out bool _contacted)
        {
            int n = _chain.Count;
            var tj = new float[n];
            _contacted = false;

            // Start the whole finger open so each joint sweep is independent of the last pose.
            for (int i = 0; i < n; i++)
                ApplyJoint(_chain[i], _openDict, _closedDict, 0f);

            for (int i = 0; i < n; i++)
            {
                // Curling joint i rotates the segment toward joint i+1 (or the tip for the last joint).
                int probeIdx = Mathf.Min(i + 1, n - 1);

                float prevT = 0f;
                bool jointHit = false;
                for (int step = 1; step <= _ctx.stepCount; step++)
                {
                    float t = (float)step / _ctx.stepCount;
                    ApplyJoint(_chain[i], _openDict, _closedDict, t);

                    if (SegmentOverlaps(_chain, i, probeIdx, _ctx))
                    {
                        // Refine between prevT (clear) and t (overlapping) so the joint sits snug
                        // against the surface instead of a coarse sweep-step short of it.
                        float lo = prevT, hi = t;
                        for (int k = 0; k < RefineIterations; k++)
                        {
                            float mid = (lo + hi) * 0.5f;
                            ApplyJoint(_chain[i], _openDict, _closedDict, mid);
                            if (SegmentOverlaps(_chain, i, probeIdx, _ctx)) hi = mid; else lo = mid;
                        }
                        prevT = lo;
                        jointHit = true;
                        break;
                    }
                    prevT = t;
                }

                // Contact → lock just before penetration. No contact → keep curling toward closed so a
                // finger that contacted earlier still wraps its distal joints around a thin object.
                tj[i] = jointHit ? prevT : 1f;
                ApplyJoint(_chain[i], _openDict, _closedDict, tj[i]); // freeze at the locked amount
                if (jointHit) _contacted = true;
            }

            // Only when the finger touched nothing at all does it relax to a gentle rest curl
            // (not a full fist), so a finger that reaches nothing reads as natural rather than clawed.
            // A finger that contacted anywhere keeps the wrap computed above.
            if (!_contacted)
            {
                float rest = Mathf.Clamp01(_ctx.noContactCurl);
                for (int i = 0; i < n; i++)
                {
                    tj[i] = rest;
                    ApplyJoint(_chain[i], _openDict, _closedDict, rest);
                }
            }

            return tj;
        }

        private const int RefineIterations = 5;

        // Sets a single joint's local pose to the lerp between its open and closed pose at t.
        private static void ApplyJoint(
            Transform _joint,
            Dictionary<string, PoseScriptableObject.JointData> _openDict,
            Dictionary<string, PoseScriptableObject.JointData> _closedDict,
            float _t)
        {
            if (!_joint) return;
            string name = _joint.name;
            if (!_openDict.TryGetValue(name, out var open)) return;
            if (!_closedDict.TryGetValue(name, out var closed)) return;

            _joint.localPosition = Vector3.Lerp(open.localPosition, closed.localPosition, _t);
            _joint.localRotation = Quaternion.Slerp(open.localRotation, closed.localRotation, _t);
        }

        // Tests the segment driven by joint i (its endpoint and midpoint) against the target.
        private static bool SegmentOverlaps(List<Transform> _chain, int _i, int _probeIdx, HandSolveContext _ctx)
        {
            var a = _chain[_i];
            var b = _chain[_probeIdx];
            if (!b) return false;

            if (OverlapsTarget(b.position, _ctx)) return true;
            if (a && _probeIdx != _i && OverlapsTarget((a.position + b.position) * 0.5f, _ctx)) return true;
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
                if (!col) continue;
                float d = Vector3.Distance(_tipPos, col.ClosestPoint(_tipPos));
                if (d < min) min = d;
            }
            return min;
        }

        private static bool OverlapsTarget(Vector3 _center, HandSolveContext _ctx)
        {
            if (_ctx.targetColliders == null || _ctx.targetColliders.Length == 0) return false;

            var hits = Physics.OverlapSphere(_center, _ctx.probeRadius, _ctx.targetMask, QueryTriggerInteraction.Ignore);
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
