// Editor test utility — safe to remove in production
using Sirenix.OdinInspector;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Drop on any GameObject alongside a HandAnimator to test SetJointsDirect in Play Mode.
    /// Assign a closed/grip PoseScriptableObject to m_TargetPose (e.g. Pose_HandGunBottom),
    /// then use the right-click context menu to trigger test blends.
    /// </summary>
    public class DynamicPoseTester : MonoBehaviour
    {
        #region Fields

        [SerializeField] private HandAnimator m_Hand;

        [Tooltip("A closed or grip pose from the project (e.g. Pose_HandGunBottom). " +
                 "Test Partial Curl lerps between DefaultPose and this pose at m_BlendAmount.")]
        [SerializeField] private PoseScriptableObject m_TargetPose;

        [Tooltip("How far to curl: 0 = open (DefaultPose), 1 = fully closed (TargetPose).")]
        [SerializeField] [Range(0f, 1f)] private float m_BlendAmount = 0.5f;

        [Tooltip("Seconds to blend to the target pose.")]
        [SerializeField] private float m_AnimationTime = 0.5f;

        #endregion

        #region Context Menu Tests

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

            if (!m_Hand.DefaultPose)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator.DefaultPose is not assigned.");
                return;
            }

            var openJoints   = m_Hand.DefaultPose.joints;
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

            m_Hand.SetJointsDirect(data, m_AnimationTime);
            Debug.Log($"[DynamicPoseTester] Partial curl at blend={m_BlendAmount:F2}, duration={m_AnimationTime}s");
        }

        /// <summary>
        /// Returns the hand to its DefaultPose via SetJointsDirect.
        /// </summary>
        [Button("Test Return to Default Pose")]
        private void TestReturnToDefault()
        {
            if (!ValidateForTest()) return;

            if (!m_Hand.DefaultPose)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator.DefaultPose is not assigned.");
                return;
            }

            m_Hand.SetJointsDirect(m_Hand.DefaultPose.joints, m_AnimationTime);
            Debug.Log($"[DynamicPoseTester] Returning to DefaultPose over {m_AnimationTime}s");
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
            if (!m_Hand)
            {
                Debug.LogError("[DynamicPoseTester] No HandAnimator assigned.");
                return false;
            }
            if (m_Hand.currentJoints.Count == 0)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator has no joints — did SetBones() run?");
                return false;
            }
            return true;
        }

        private void OnValidate()
        {
            if (!m_Hand)
                m_Hand = GetComponentInChildren<HandAnimator>();
        }

        #endregion
    }
}
