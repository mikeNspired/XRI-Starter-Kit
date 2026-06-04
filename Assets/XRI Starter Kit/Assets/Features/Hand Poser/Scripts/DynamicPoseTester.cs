// Editor test utility — safe to remove in production
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Drop on any GameObject alongside a HandAnimator to test SetJointsDirect in Play Mode.
    /// Use the right-click context menu on this component to trigger test poses.
    /// </summary>
    public class DynamicPoseTester : MonoBehaviour
    {
        #region Fields

        [SerializeField] private HandAnimator m_Hand;
        [SerializeField] private float m_AnimationTime = 0.3f;

        private const float c_TestCurlDegrees = 45f;

        #endregion

        #region Context Menu Tests

        /// <summary>
        /// Applies a partial curl to every joint in the hand by rotating each joint
        /// an additional 45 degrees around its local Y axis. Run repeatedly to increase curl.
        /// </summary>
        [ContextMenu("Test Partial Curl")]
        private void TestPartialCurl()
        {
            if (!ValidateForTest()) return;

            var joints = m_Hand.currentJoints;
            var data = new PoseScriptableObject.JointData[joints.Count];

            for (int i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                data[i] = new PoseScriptableObject.JointData
                {
                    jointName  = joint.name,
                    localPosition = joint.localPosition,
                    // Skip root joint (palm); curl all finger joints
                    localRotation = (i == 0)
                        ? joint.localRotation
                        : joint.localRotation * Quaternion.Euler(0, c_TestCurlDegrees, 0)
                };
            }

            m_Hand.SetJointsDirect(data, m_AnimationTime);
            Debug.Log($"[DynamicPoseTester] SetJointsDirect — {data.Length} joints, blend {m_AnimationTime}s");
        }

        /// <summary>
        /// Resets the hand to its DefaultPose via SetJointsDirect to confirm the blend back works.
        /// </summary>
        [ContextMenu("Test Return to Default Pose")]
        private void TestReturnToDefault()
        {
            if (!ValidateForTest()) return;

            if (!m_Hand.DefaultPose)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator.DefaultPose is not assigned.");
                return;
            }

            m_Hand.SetJointsDirect(m_Hand.DefaultPose.joints, m_AnimationTime);
            Debug.Log($"[DynamicPoseTester] SetJointsDirect — returning to DefaultPose over {m_AnimationTime}s");
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
