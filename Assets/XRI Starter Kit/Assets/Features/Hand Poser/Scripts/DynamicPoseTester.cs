using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Phase 0 test harness. Builds a lerped JointData[] between the hand's DefaultPose and
    /// a ClosedPose at CurlAmount, then drives it through HandAnimator.SetJointsDirect so the
    /// hand blends to the result over AnimationTime seconds.
    ///
    /// How to use:
    ///   1. Add this component to the same GameObject as (or a parent of) your HandAnimator.
    ///   2. Assign HandAnimator, ClosedPose, and optionally override the defaults.
    ///   3. In Play Mode press TestKey (default: Space) OR use the context-menu "Trigger Test Pose".
    /// </summary>
    public class DynamicPoseTester : MonoBehaviour
    {
        [Tooltip("The HandAnimator to drive. Its DefaultPose is used as the open/start pose.")]
        [SerializeField] HandAnimator handAnimator;

        [Tooltip("The closed/fist PoseScriptableObject to curl toward.")]
        [SerializeField] PoseScriptableObject closedPose;

        [Tooltip("Blend amount: 0 = open (DefaultPose), 1 = fully closed (ClosedPose).")]
        [Range(0f, 1f)]
        [SerializeField] float curlAmount = 0.5f;

        [Tooltip("Seconds the hand takes to blend into the test pose.")]
        [SerializeField] float animationTime = 0.3f;

        [Tooltip("Key that triggers the test pose in Play Mode.")]
        [SerializeField] KeyCode testKey = KeyCode.Space;

        void Update()
        {
            if (Input.GetKeyDown(testKey))
                TriggerTestPose();
        }

        [ContextMenu("Trigger Test Pose")]
        public void TriggerTestPose()
        {
            if (!handAnimator)
            {
                Debug.LogWarning("DynamicPoseTester: HandAnimator is not assigned.");
                return;
            }

            var openPose = handAnimator.DefaultPose;
            if (!openPose)
            {
                Debug.LogWarning("DynamicPoseTester: HandAnimator.DefaultPose is not assigned.");
                return;
            }

            if (!closedPose)
            {
                Debug.LogWarning("DynamicPoseTester: ClosedPose is not assigned.");
                return;
            }

            var jointData = BuildLerpedJointData(openPose, closedPose, curlAmount);
            if (jointData == null) return;

            handAnimator.SetJointsDirect(jointData, animationTime);
        }

        static PoseScriptableObject.JointData[] BuildLerpedJointData(
            PoseScriptableObject open,
            PoseScriptableObject closed,
            float t)
        {
            if (open.joints == null || open.joints.Length == 0)
            {
                Debug.LogWarning("DynamicPoseTester: open pose has no joints.");
                return null;
            }

            if (closed.joints == null || closed.joints.Length == 0)
            {
                Debug.LogWarning("DynamicPoseTester: closed pose has no joints.");
                return null;
            }

            var result = new PoseScriptableObject.JointData[open.joints.Length];
            for (int i = 0; i < open.joints.Length; i++)
            {
                var openJoint = open.joints[i];

                // Find the matching joint in the closed pose by name
                PoseScriptableObject.JointData closedJoint = default;
                bool found = false;
                foreach (var cj in closed.joints)
                {
                    if (cj.jointName == openJoint.jointName)
                    {
                        closedJoint = cj;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // No closed-pose entry — keep the open position unchanged
                    result[i] = openJoint;
                    continue;
                }

                result[i] = new PoseScriptableObject.JointData
                {
                    jointName = openJoint.jointName,
                    localPosition = Vector3.Lerp(openJoint.localPosition, closedJoint.localPosition, t),
                    localRotation = Quaternion.Lerp(openJoint.localRotation, closedJoint.localRotation, t)
                };
            }

            return result;
        }
    }
}
