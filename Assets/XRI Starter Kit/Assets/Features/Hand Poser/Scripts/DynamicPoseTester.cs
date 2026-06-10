// Editor test utility — safe to remove in production
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Drop on any GameObject alongside a HandAnimator to test SetJointsDirect in Play Mode.
    /// Assign a closed/grip PoseScriptableObject to m_TargetPose (e.g. Pose_HandGunBottom),
    /// then use the Odin buttons to trigger test blends.
    /// Per-finger curl verification uses the HandAnimator inspector's built-in Finger Sliders section.
    /// </summary>
    public class DynamicPoseTester : MonoBehaviour
    {
        #region Fields

        [FormerlySerializedAs("m_Hand")]
        [SerializeField] private HandAnimator m_HandAnimator;

        [Tooltip("A closed or grip pose from the project (e.g. Pose_HandGunBottom). " +
                 "Test Partial Curl lerps between DefaultPose and this pose at m_BlendAmount.")]
        [SerializeField] private PoseScriptableObject m_TargetPose;

        [Tooltip("How far to curl: 0 = open (DefaultPose), 1 = fully closed (TargetPose).")]
        [SerializeField] [Range(0f, 1f)] private float m_BlendAmount = 0.5f;

        [Tooltip("Seconds to blend to the target pose.")]
        [SerializeField] private float m_AnimationTime = 0.5f;

        [Header("Curl Sweep Solver Test")]
        [Tooltip("The object whose colliders fingers will curl toward and stop at.")]
        [SerializeField] private GameObject m_Target;

        [Tooltip("Number of t steps per finger sweep (higher = more accurate, more expensive).")]
        [SerializeField] [Range(5, 30)] private int m_SolverStepCount = 15;

        [Tooltip("Radius of the sphere probe at each fingertip during the sweep.")]
        [SerializeField] [Range(0.001f, 0.05f)] private float m_SolverProbeRadius = 0.01f;

        #endregion

        private CurlSweepSolver m_LastSolver;

        #region Button Tests

        /// <summary>
        /// Lerps each joint between DefaultPose and TargetPose at m_BlendAmount,
        /// then applies the result via SetJointsDirect over m_AnimationTime seconds.
        /// </summary>
        [Button("Test Partial Curl")]
        private void TestPartialCurl()
        {
            if (!ValidateForTest()) return;

            if (!m_TargetPose)
            {
                Debug.LogError("[DynamicPoseTester] Assign a closed/grip pose to m_TargetPose in the Inspector.");
                return;
            }

            if (!m_HandAnimator.DefaultPose)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator.DefaultPose is not assigned.");
                return;
            }

            var openJoints   = m_HandAnimator.DefaultPose.joints;
            var closedJoints = m_TargetPose.joints;

            // Build a blended JointData[] at m_BlendAmount between open and closed
            var data = new PoseScriptableObject.JointData[openJoints.Length];
            for (int i = 0; i < openJoints.Length; i++)
            {
                // Find matching closed joint by name; fall back to open data if not found
                var openJoint = openJoints[i];
                var closedRot = openJoint.localRotation;
                var closedPos = openJoint.localPosition;

                for (int j = 0; j < closedJoints.Length; j++)
                {
                    if (closedJoints[j].jointName == openJoint.jointName)
                    {
                        closedRot = closedJoints[j].localRotation;
                        closedPos = closedJoints[j].localPosition;
                        break;
                    }
                }

                data[i] = new PoseScriptableObject.JointData
                {
                    jointName     = openJoint.jointName,
                    localPosition = Vector3.Lerp(openJoint.localPosition, closedPos, m_BlendAmount),
                    localRotation = Quaternion.Lerp(openJoint.localRotation, closedRot, m_BlendAmount)
                };
            }

            m_HandAnimator.SetJointsDirect(data, m_AnimationTime);
            Debug.Log($"[DynamicPoseTester] Partial curl at blend={m_BlendAmount:F2}, duration={m_AnimationTime}s");
        }

        /// <summary>
        /// Returns the hand to its DefaultPose via SetJointsDirect.
        /// </summary>
        [Button("Test Return to Default Pose")]
        private void TestReturnToDefault()
        {
            if (!ValidateForTest()) return;

            if (!m_HandAnimator.DefaultPose)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator.DefaultPose is not assigned.");
                return;
            }

            m_HandAnimator.SetJointsDirect(m_HandAnimator.DefaultPose.joints, m_AnimationTime);
            Debug.Log($"[DynamicPoseTester] Returning to DefaultPose over {m_AnimationTime}s");
        }

        /// <summary>
        /// Runs the CurlSweepSolver against m_Target's colliders and applies the result
        /// via SetJointsDirect. Gizmos show each finger's final probe position.
        /// Must be in Play Mode.
        /// </summary>
        [Button("Test Curl Sweep Solver")]
        private void TestCurlSweepSolver()
        {
            if (!ValidateForTest()) return;

            if (!m_Target)
            {
                Debug.LogError("[DynamicPoseTester] Assign a target GameObject to m_Target.");
                return;
            }
            if (!m_HandAnimator.ClosedPose)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator.ClosedPose is not assigned. Assign a fist pose.");
                return;
            }
            if (!m_HandAnimator.DefaultPose)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator.DefaultPose is not assigned.");
                return;
            }

            // Solid colliders only, matching XRHandPoser's gather: triggers can't stop fingers and
            // would corrupt the fingertip-gap measurements.
            var colliders = System.Array.FindAll(
                m_Target.GetComponentsInChildren<Collider>(),
                c => c.enabled && !c.isTrigger);
            if (colliders.Length == 0)
                Debug.LogWarning($"[DynamicPoseTester] '{m_Target.name}' has no solid Colliders — solver will fully close all fingers.");

            var ctx = new HandSolveContext
            {
                hand            = m_HandAnimator,
                fingerMap       = m_HandAnimator.fingerMap,
                openPose        = m_HandAnimator.DefaultPose,
                closedPose      = m_HandAnimator.ClosedPose,
                targetColliders = colliders,
                targetMask      = 1 << m_Target.layer,
                stepCount       = m_SolverStepCount,
                probeRadius     = m_SolverProbeRadius
            };

            m_LastSolver = new CurlSweepSolver();
            var result = m_LastSolver.Solve(ctx);

            if (result != null && result.Length > 0)
            {
                m_HandAnimator.SetJointsDirect(result, m_AnimationTime);
                Debug.Log($"[DynamicPoseTester] Curl sweep: solved {result.Length} joints against '{m_Target.name}'.");
            }
            else
            {
                Debug.LogWarning("[DynamicPoseTester] Solver returned no joint data.");
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (m_LastSolver?.LastSolveProbePositions == null) return;

            for (int i = 0; i < 5; i++)
            {
                Gizmos.color = m_LastSolver.LastSolveContacted[i] ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(m_LastSolver.LastSolveProbePositions[i], m_SolverProbeRadius);
            }
        }

        #endregion

        #region Private Helpers

        private bool ValidateForTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DynamicPoseTester] Must be in Play Mode.");
                return false;
            }
            if (!m_HandAnimator)
            {
                Debug.LogError("[DynamicPoseTester] No HandAnimator assigned.");
                return false;
            }
            if (m_HandAnimator.currentJoints.Count == 0)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator has no joints — did SetBones() run?");
                return false;
            }
            return true;
        }

        private void OnValidate()
        {
            if (!m_HandAnimator)
                m_HandAnimator = GetComponentInChildren<HandAnimator>();
        }

        #endregion
    }
}
