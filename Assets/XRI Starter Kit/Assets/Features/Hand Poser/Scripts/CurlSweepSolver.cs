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

        public PoseScriptableObject.JointData[] Solve(HandSolveContext _ctx)
        {
            if (!Validate(_ctx)) return null;

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

            var tFinals = new float[5];

            try
            {
                // Start every finger at open pose so sweeps are independent
                for (int i = 0; i < 5; i++)
                    _ctx.hand.SetFingerCurl(i, 0f, _ctx.openPose, _ctx.closedPose);

                for (int fingerIdx = 0; fingerIdx < 5; fingerIdx++)
                {
                    var chain = _ctx.fingerMap.Finger(fingerIdx);
                    if (chain == null || chain.Count == 0)
                    {
                        tFinals[fingerIdx] = 0f;
                        LastSolveProbePositions[fingerIdx] = Vector3.zero;
                        LastSolveContacted[fingerIdx] = false;
                        continue;
                    }

                    // Probe at the deepest joint (actual fingertip bone)
                    var tip = chain[chain.Count - 1];
                    float prevT = 0f;
                    Vector3 prevProbePos = tip.position;
                    bool contacted = false;

                    for (int step = 1; step <= _ctx.stepCount; step++)
                    {
                        float t = (float)step / _ctx.stepCount;
                        _ctx.hand.SetFingerCurl(fingerIdx, t, _ctx.openPose, _ctx.closedPose);

                        Vector3 probePos = tip.position;

                        if (OverlapsTarget(probePos, _ctx))
                        {
                            // Lock at the last position before contact
                            tFinals[fingerIdx] = prevT;
                            LastSolveProbePositions[fingerIdx] = prevProbePos;
                            LastSolveContacted[fingerIdx] = true;
                            contacted = true;
                            break;
                        }

                        prevT = t;
                        prevProbePos = probePos;
                    }

                    if (!contacted)
                    {
                        tFinals[fingerIdx] = 1f;
                        LastSolveProbePositions[fingerIdx] = tip.position;
                        LastSolveContacted[fingerIdx] = false;
                    }

                    // Reset before the next finger's sweep so chains don't interfere
                    _ctx.hand.SetFingerCurl(fingerIdx, 0f, _ctx.openPose, _ctx.closedPose);
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

            return BuildJointData(_ctx, tFinals);
        }

        #region Private Helpers

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

        private static PoseScriptableObject.JointData[] BuildJointData(HandSolveContext _ctx, float[] _tFinals)
        {
            var openDict = new Dictionary<string, PoseScriptableObject.JointData>(_ctx.openPose.joints.Length);
            foreach (var jd in _ctx.openPose.joints) openDict[jd.jointName] = jd;

            var closedDict = new Dictionary<string, PoseScriptableObject.JointData>(_ctx.closedPose.joints.Length);
            foreach (var jd in _ctx.closedPose.joints) closedDict[jd.jointName] = jd;

            var result = new List<PoseScriptableObject.JointData>();
            var seen = new HashSet<string>();

            for (int i = 0; i < 5; i++)
            {
                float t = _tFinals[i];
                var chain = _ctx.fingerMap.Finger(i);
                if (chain == null) continue;

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
