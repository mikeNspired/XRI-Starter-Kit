using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// The main script that animates the hands. Located on the hand model, and as a child of the controller.
    /// Uses poses stored in PoseScriptableObjects, which contain joint data.
    /// </summary>
    public class HandAnimator : MonoBehaviour
    {
        [Tooltip("Draws spheres to see joints")]
        [SerializeField] bool drawHelperSpheres;

        [Tooltip("Left or Right hand, used for determining which attachpoint needed")]
        public LeftRight handType;

        public Pose RootBone;
        public PoseScriptableObject DefaultPose;
        public PoseScriptableObject AnimationPose;
        public PoseScriptableObject SecondButtonPose;

        [Tooltip("Time hand skeleton animates to next pose")]
        public float animationTimeToNewPose = .1f;

        [Tooltip("Time to move hand to the item being grabbed")]
        public float handMoveToTargetAnimationTime = .1f;

        public float triggerAnimationValue, gripAnimationValue;
        public bool isGrabbingObject;

        public List<Transform> currentJoints = new List<Transform>();
        List<Transform> goalPoseJoints = new List<Transform>();

        Vector3 originalPosition;
        Quaternion originalRotation;
        Transform originalParent;

        IEnumerator AnimateToPoseAnimation;
        IEnumerator AnimateByTriggerValue;

        // Store the original default & animation poses to restore later
        PoseScriptableObject originalPose;
        PoseScriptableObject originalAnimationPose;

        public UnityAction<bool> NewPoseStarting = delegate {};

        // Fields for finger slider logic
        public PoseScriptableObject defaultPose, goalPose;
        public Transform thumbTopTransform, indexTopTransform, middleTopTransform, ringTopTransform, pinkyTopTransform;

        public void AnimateToCurrent() => AnimateInstantly(DefaultPose);

        void Awake()
        {
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
            originalParent = transform.parent;

            originalPose = DefaultPose;
            originalAnimationPose = AnimationPose;

            if (!DefaultPose && HandPoserSettings.Instance)
                DefaultPose = HandPoserSettings.Instance.DefaultPose;

            SetBones();
            AnimateInstantly(DefaultPose);
        }

        void OnEnable() => Application.onBeforeRender += OnBeforeRender;
        void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

        public void SetAnimationValue(float val) => triggerAnimationValue = val;
        public void StartAnimationPosing() => StartAnimationByButtonValue(ControllerButtons.Trigger);
        public void StartSecondaryPosing() => StartAnimationByButtonValue(ControllerButtons.Grip);
        public void SetSecondaryValue(float val) => gripAnimationValue = val;

        public void ReturnToDefaultPosing()
        {
            isGrabbingObject = false;
            BeginNewPoses(DefaultPose, AnimationPose, false);
        }

        public void SetBones()
        {
            currentJoints.Clear();
            JointUtility.GatherTransformsForPose(RootBone.transform, currentJoints);
        }

        public void SetPoses(PoseScriptableObject primaryPose, PoseScriptableObject animationPose)
        {
            DefaultPose = primaryPose;
            AnimationPose = animationPose;
        }

        public void BeginNewPoses(PoseScriptableObject primaryPose, PoseScriptableObject animationPose, bool isGrabbing)
        {
            isGrabbingObject = isGrabbing;
            NewPoseStarting.Invoke(isGrabbingObject);

            SetJointPositions(DefaultPose, goalPoseJoints);
            TransformStruct[] oldPose = CopyTransformData(goalPoseJoints);

            AnimationPose = animationPose;
            DefaultPose = primaryPose;

            SetJointPositions(primaryPose, goalPoseJoints);
            TransformStruct[] newPose = CopyTransformData(goalPoseJoints);

            if (AnimateByTriggerValue != null) StopCoroutine(AnimateByTriggerValue);
            if (AnimateToPoseAnimation != null) StopCoroutine(AnimateToPoseAnimation);

            AnimateToPoseAnimation = AnimateToPoseOverTime(oldPose, newPose);
            StartCoroutine(AnimateToPoseAnimation);
        }

        void StartAnimationByButtonValue(ControllerButtons button)
        {
            if (button == ControllerButtons.Grip && isGrabbingObject) return;
            var newPose = (button == ControllerButtons.Trigger) ? AnimationPose :
                          (button == ControllerButtons.Grip) ? SecondButtonPose : null;

            if (!newPose) return;
            if (AnimateByTriggerValue != null) StopCoroutine(AnimateByTriggerValue);

            AnimateByTriggerValue = AnimateToPoseByValue2(newPose, button);
            StartCoroutine(AnimateByTriggerValue);
        }

        IEnumerator AnimateToPoseOverTime(TransformStruct[] originalPose, TransformStruct[] newPose)
        {
            float timer = 0;
            while (timer <= animationTimeToNewPose + Time.deltaTime)
            {
                for (int i = 0; i < currentJoints.Count; i++)
                {
                    var joint = currentJoints[i];
                    if (!joint) continue;

                    var pos = Vector3.Lerp(originalPose[i].position, newPose[i].position, timer / animationTimeToNewPose);
                    var rot = Quaternion.Lerp(originalPose[i].rotation, newPose[i].rotation, timer / animationTimeToNewPose);
                    SetNewJoint(ref joint, pos, rot);
                }
                timer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }
        }

        IEnumerator AnimateToPoseByValue2(PoseScriptableObject newPose, ControllerButtons button)
        {
            SetJointPositions(DefaultPose, goalPoseJoints);
            TransformStruct[] startingPose = CopyTransformData(goalPoseJoints);

            SetJointPositions(newPose, goalPoseJoints);
            yield return null;

            while (true)
            {
                float value = (button == ControllerButtons.Trigger) ? triggerAnimationValue :
                              (button == ControllerButtons.Grip) ? gripAnimationValue : 0;

                for (int i = 0; i < currentJoints.Count; i++)
                {
                    var joint = currentJoints[i];
                    if (!joint) continue;

                    var goalPos = goalPoseJoints[i].localPosition;
                    var goalRot = goalPoseJoints[i].localRotation;

                    var pos = Vector3.Lerp(startingPose[i].position, goalPos, value);
                    var rot = Quaternion.Lerp(startingPose[i].rotation, goalRot, value);

                    SetNewJoint(ref joint, pos, rot);
                }
                yield return null;
            }
        }

        public void AnimateInstantly(PoseScriptableObject pose)
        {
            if (currentJoints.Count == 0 || !currentJoints[0])
                SetBones();

            if (AnimateToPoseAnimation != null) StopCoroutine(AnimateToPoseAnimation);
            if (AnimateByTriggerValue != null) StopCoroutine(AnimateByTriggerValue);

            SetJointPositions(pose, goalPoseJoints);
            AnimateInstant(goalPoseJoints);
        }

        void AnimateInstant(List<Transform> goalPose)
        {
            for (int i = 0; i < currentJoints.Count; i++)
            {
                var joint = currentJoints[i];
                if (!joint || i >= goalPose.Count || !goalPose[i]) continue;
                SetNewJoint(ref joint, goalPose[i].localPosition, goalPose[i].localRotation);
            }
        }

        IEnumerator AnimateHandToPosition;
        IEnumerator WaitForObjectToBeClose;

        public void MoveHandToTarget(Transform attachPoint, float interactableAttachEaseInTime, bool waitForHandToAnimateToPosition)
        => StartCoroutine(MoveHandToTargetIE(attachPoint, interactableAttachEaseInTime, waitForHandToAnimateToPosition));

        IEnumerator MoveHandToTargetIE(Transform attachPoint, float interactableAttachEaseInTime, bool waitForHandToAnimateToPosition)
        {
            if (waitForHandToAnimateToPosition)
                yield return new WaitForSeconds(interactableAttachEaseInTime);

            transform.parent = null;

            if (AnimateHandToPosition != null) StopCoroutine(AnimateHandToPosition);
            AnimateHandToPosition = AnimateHandToTransform(handMoveToTargetAnimationTime, attachPoint);
            yield return StartCoroutine(AnimateHandToPosition);

            StartHandPositionTracking(attachPoint);
        }

        public void ReturnAnimationsToOriginal()
        {
            DefaultPose = originalPose;
            AnimationPose = originalAnimationPose;
        }

        public void ReturnHandToPlayer()
        {
            StopAllCoroutines();
            transform.parent = originalParent;

            StopHandPositionTracking();
            if (AnimateHandToPosition != null) StopCoroutine(AnimateHandToPosition);

            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
        }

        IEnumerator AnimateHandToTransform(float animationLength, Transform newTransform)
        {
            float timer = 0;
            var startPos = transform.position;
            var startRot = transform.rotation;

            while (timer < animationLength + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(startPos, newTransform.position, timer / animationLength);
                var newRotation = Quaternion.Lerp(startRot, newTransform.rotation, timer / animationLength);

                transform.SetPositionAndRotation(newPosition, newRotation);

                yield return new WaitForSeconds(Time.deltaTime);
                timer += Time.deltaTime;
            }
            transform.SetPositionAndRotation(newTransform.position, newTransform.rotation);
        }

        IEnumerator AnimateHandTransformLocal(float animationLength, TransformStruct newTransform)
        {
            float timer = 0;
            var startPos = transform.localPosition;
            var startRot = transform.localRotation;

            while (timer < animationLength + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(startPos, newTransform.position, timer / animationLength);
                var newRotation = Quaternion.Lerp(startRot, newTransform.rotation, timer / animationLength);

                transform.localPosition = newPosition;
                transform.localRotation = newRotation;

                yield return new WaitForSeconds(Time.deltaTime);
                timer += Time.deltaTime;
            }
            transform.localPosition = newTransform.position;
            transform.localRotation = newTransform.rotation;
        }

        void StartHandPositionTracking(Transform target)
        {
            setPosition = true;
            handPositionTarget = target;
        }

        void StopHandPositionTracking()
        {
            setPosition = false;
            handPositionTarget = null;
        }

        Transform handPositionTarget;
        bool setPosition;

        [BeforeRenderOrder(102)]
        void OnBeforeRender()
        {
            if (setPosition && handPositionTarget)
                transform.SetPositionAndRotation(handPositionTarget.position, handPositionTarget.rotation);
        }

        void SetNewJoint(ref Transform joint, Vector3 newPosition, Quaternion newRotation)
        {
            joint.localPosition = newPosition;
            joint.localEulerAngles = newRotation.eulerAngles;
        }

        TransformStruct[] CopyTransformData(List<Transform> joints)
        {
            var transforms = new TransformStruct[joints.Count];
            for (int i = 0; i < joints.Count; i++)
            {
                var j = joints[i];
                transforms[i].SetTransformStruct(j.localPosition, j.localRotation, Vector3.one);
            }
            return transforms;
        }

        void SetJointPositions(PoseScriptableObject poseAsset, List<Transform> jointList)
        {
            if (!poseAsset)
            {
                Debug.LogError("No PoseScriptableObject assigned. Cannot set joint positions.");
                return;
            }

            if (!RootBone)
            {
                Debug.LogError("RootBone is not assigned. Cannot set joint positions.");
                return;
            }

            jointList.Clear();

            // Gather all child joints under RootBone
            List<Transform> allJoints = new List<Transform>();
            GatherAllChildJoints(RootBone.transform, allJoints);

            // Match joints from PoseScriptableObject to actual transforms
            foreach (var poseAssetJoint in poseAsset.joints)
            {
                var match = allJoints.Find(joint => joint.name == poseAssetJoint.jointName);
                if (match)
                {
                    match.localPosition = poseAssetJoint.localPosition;
                    match.localRotation = poseAssetJoint.localRotation;
                    jointList.Add(match);
                }
                else
                {
                    Debug.LogWarning($"Transform not found in hierarchy: {poseAssetJoint.jointName}");
                }
            }
        }
        
        void GatherAllChildJoints(Transform parent, List<Transform> jointList)
        {
            if (parent == null || jointList == null) return;

            // Add the current transform to the list
            jointList.Add(parent);

            // Recurse into children
            for (int i = 0; i < parent.childCount; i++)
            {
                GatherAllChildJoints(parent.GetChild(i), jointList);
            }
        }

        public void SetPoseByValue(Transform currentJoint, PoseScriptableObject startPose, PoseScriptableObject endPose,
            float value)
        {
            if (!startPose || !endPose)
            {
                Debug.LogWarning("Missing PoseScriptableObject. Skipping SetPoseByValue.");
                return;
            }

            // Find the corresponding joints in the start and end poses
            var startJoint = startPose.joints.FirstOrDefault(j => j.jointName == currentJoint.name);
            var endJoint = endPose.joints.FirstOrDefault(j => j.jointName == currentJoint.name);

            if (startJoint.jointName == null || endJoint.jointName == null)
            {
                Debug.LogWarning($"Joint {currentJoint.name} not found in one of the poses. Skipping.");
                return;
            }

            // Interpolate the root joint's position and rotation
            var newPosition = Vector3.Lerp(startJoint.localPosition, endJoint.localPosition, value);
            var newRotation = Quaternion.Lerp(startJoint.localRotation, endJoint.localRotation, value);
            SetNewJoint(ref currentJoint, newPosition, newRotation);

            // Recursively process child joints using JointUtility
            var jointsInHand = new List<Transform>();
            JointUtility.GatherTransformsForPose(currentJoint, jointsInHand);

            for (int i = 0; i < jointsInHand.Count; i++)
            {
                var joint = jointsInHand[i];

                var matchingStartJoint = startPose.joints.FirstOrDefault(j => j.jointName == joint.name);
                var matchingEndJoint = endPose.joints.FirstOrDefault(j => j.jointName == joint.name);

                if (matchingStartJoint.jointName == null || matchingEndJoint.jointName == null) continue;

                var childNewPosition = Vector3.Lerp(matchingStartJoint.localPosition, matchingEndJoint.localPosition, value);
                var childNewRotation = Quaternion.Lerp(matchingStartJoint.localRotation, matchingEndJoint.localRotation, value);
                SetNewJoint(ref joint, childNewPosition, childNewRotation);
            }
        }


       

        private void OnDrawGizmosSelected()
        {
            if (!RootBone) return;
            if (drawHelperSpheres)
                RootBone.DrawJoints(RootBone.transform);
            else
                RootBone.debugSpheresEnabled = false;
        }
    }
}
