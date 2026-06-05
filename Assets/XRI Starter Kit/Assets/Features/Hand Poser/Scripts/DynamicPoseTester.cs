// Editor test utility — safe to remove in production
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Drop on any GameObject alongside a HandAnimator to test dynamic posing in Play Mode.
    /// Assign a closed/grip PoseScriptableObject to m_TargetPose (e.g. Pose_HandGunBottom)
    /// for the whole-hand blend test, and a fist pose to HandAnimator.ClosedPose for the
    /// per-finger curl test, then use the Odin buttons below to trigger the tests.
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

        [Header("Per-Finger Curl (Phase 1) — requires HandAnimator.ClosedPose")]
        [SerializeField, Range(0f, 1f)] private float m_ThumbCurl  = 0f;
        [SerializeField, Range(0f, 1f)] private float m_IndexCurl  = 0f;
        [SerializeField, Range(0f, 1f)] private float m_MiddleCurl = 0f;
        [SerializeField, Range(0f, 1f)] private float m_RingCurl   = 0f;
        [SerializeField, Range(0f, 1f)] private float m_PinkyCurl  = 0f;

        #endregion

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
        /// Poses each finger independently using HandAnimator.SetFingerCurls.
        /// Requires HandAnimator.ClosedPose to be assigned.
        /// </summary>
        [Button("Test Finger Curls")]
        private void TestFingerCurls()
        {
            if (!ValidateForTest()) return;
            if (!m_HandAnimator.ClosedPose)
            {
                Debug.LogError("[DynamicPoseTester] HandAnimator.ClosedPose is not assigned. Assign a fist pose to use per-finger curl.");
                return;
            }

            m_HandAnimator.SetFingerCurls(new[] { m_ThumbCurl, m_IndexCurl, m_MiddleCurl, m_RingCurl, m_PinkyCurl });
            Debug.Log($"[DynamicPoseTester] Finger curls applied — thumb={m_ThumbCurl:F2} index={m_IndexCurl:F2} middle={m_MiddleCurl:F2} ring={m_RingCurl:F2} pinky={m_PinkyCurl:F2}");
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
