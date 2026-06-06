// Author MikeNspired. 

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// The main script to setup the hand for animations.
    /// Its main purpose is to quickly setup hand poses for each item, and then assign those poses to the hand when the item is grabbed.
    /// This script is driven by the XRGrabInteractable to be used with UnityXR. It uses the onSelectEnter and onSelectExit to work.
    /// </summary>
    public class XRHandPoser : HandPoser
    {
        public XRBaseInteractable interactable;
        public bool MaintainHandOnObject = true;
        public bool WaitTillEaseInTimeToMaintainPosition = true;
        public bool overrideEaseTime = false;
        public float easeInTimeOverride = 0;

        [Header("Dynamic Pose Gate")]
        [SerializeField] private float positionThreshold = 0.05f;
        [SerializeField] private float rotationThreshold = 30f;

        protected override void Awake()
        {
            base.Awake();
            OnValidate();
            SubscribeToSelection();
        }

        private void SubscribeToSelection()
        {
            //Set hand animation on grab
            interactable.selectEntered.AddListener(TryStartPosing);

            //Set to default animations when item is released
            interactable.selectExited.AddListener(TryReleaseHand);
        }

        private void TryStartPosing(SelectEnterEventArgs x)
        {
            var handRef = x.interactorObject.transform.GetComponentInParent<HandReference>();
            if (!handRef) return;

            if (handRef.NearFarInteractor != null && handRef.NearFarInteractor.interactionAttachController.hasOffset)
            {
                Debug.Log("Hand poser skipped also");
                return; // Skip hand posing for far interactions
            }

            // Phase 3: log the routing decision before any pose is applied.
            // Both branches fall through to BeginNewHandPoses — no behavioral change yet.
            LogGrabDecision(x.interactorObject.transform, handRef.Hand);
            BeginNewHandPoses(handRef.Hand);
        }

        // Computes authored-vs-dynamic and logs the decision.  No pose is changed here.
        private void LogGrabDecision(Transform interactorTransform, HandAnimator hand)
        {
            if (!CheckIfPoseExistForHand(hand))
            {
                Debug.Log($"[XRHandPoser] {gameObject.name} | DYNAMIC — no authored pose for {hand.handType} hand");
                return;
            }

            var authoredAttach = hand.handType == LeftRight.Left ? leftHandAttach : rightHandAttach;
            if (!authoredAttach)
            {
                Debug.Log($"[XRHandPoser] {gameObject.name} | AUTHORED — no attach transform found, treating as on-axis");
                return;
            }

            float offsetPos   = Vector3.Distance(interactorTransform.position, authoredAttach.position);
            float offsetAngle = Quaternion.Angle(interactorTransform.rotation, authoredAttach.rotation);

            bool isDynamic = offsetPos > positionThreshold || offsetAngle > rotationThreshold;

            if (isDynamic)
                Debug.Log($"[XRHandPoser] {gameObject.name} | DYNAMIC — pos={offsetPos:F3}m (threshold {positionThreshold}m), angle={offsetAngle:F1}° (threshold {rotationThreshold}°)");
            else
                Debug.Log($"[XRHandPoser] {gameObject.name} | AUTHORED — pos={offsetPos:F3}m, angle={offsetAngle:F1}°");
        }

        private void TryReleaseHand(SelectExitEventArgs x)
        {
            if (!x.interactorObject.transform.GetComponentInParent<HandReference>()) return;
            Release();
        }

        private void MoveHandToPoseTransforms(HandAnimator hand)
        {
            //Determines if the left or right hand is grabbed, and then sends over the proper attachment point to be assigned to the XRGrabInteractable.
            var attachPoint = hand.handType == LeftRight.Left ? leftHandAttach : rightHandAttach;
            hand.MoveHandToTarget(attachPoint, GetEaseInTime(), WaitTillEaseInTimeToMaintainPosition);
        }

        protected override void BeginNewHandPoses(HandAnimator hand)
        {
            if (!hand || !CheckIfPoseExistForHand(hand)) return;

            base.BeginNewHandPoses(hand);

            if (MaintainHandOnObject) MoveHandToPoseTransforms(hand);
        }

        private bool CheckIfPoseExistForHand(HandAnimator hand)
        {
            if (leftHandPose && hand.handType == LeftRight.Left)
                return true;
            if (rightHandPose && hand.handType == LeftRight.Right)
                return true;
            return false;
        }

        private float GetEaseInTime()
        {
            float time = 0;
            interactable.TryGetComponent(out XRGrabInteractable xrGrabInteractable);
            if (xrGrabInteractable)
                time = xrGrabInteractable.attachEaseInTime;
            if (overrideEaseTime)
                time = easeInTimeOverride;

            return time;
        }
        private void OnValidate()
        {
            if (!interactable)
                interactable = GetComponent<XRBaseInteractable>();
            if (!interactable)
                interactable = GetComponentInParent<XRBaseInteractable>();
            if (!interactable)
                Debug.LogWarning(gameObject + " XRGrabPoser does not have an XRGrabInteractable assigned." + "  (Parent name) " + transform.parent);
        }
    }
}