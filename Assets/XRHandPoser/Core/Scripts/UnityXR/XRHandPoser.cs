// Copyright (c) MikeNspired. All Rights Reserved.

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    /// <summary>
    /// The main script to setup the hand for animations.
    /// Its main purpose is to quickly setup hand poses for each item, and then assign those poses to the hand when the item is grabbed.
    /// This script is driven by the XRGrabInteractable to be used with UnityXR. It uses the onSelectEnter and onSelectExit to work.
    /// </summary>
    public class XRHandPoser : HandPoser
    {
        public XRGrabInteractable interactable;
        public bool MoveHandToObject = false;
        public bool DisableHandAttachTransforms = false;

        private Rigidbody rb;

        protected override void Awake()
        {
            base.Awake();

            OnValidate();

            SubscribeToSelection();
        }

        private void SubscribeToSelection()
        {
            //Set hand animation on grab
            interactable.onSelectEntered.AddListener(x => SetAttachForInstantaneous(x.GetComponent<HandReference>()?.Hand));
            interactable.onSelectEntered.AddListener(x => BeginNewHandPoses(x.GetComponent<HandReference>()?.Hand));

            //Set to default animations when item is released
            interactable.onSelectExited.AddListener(x => Release());
            interactable.onSelectExited.AddListener(x => rb.ResetCenterOfMass());
        }

        private void OnValidate()
        {
            if (!interactable)
                interactable = GetComponent<XRGrabInteractable>();

            if (!interactable)
                Debug.LogWarning(gameObject + " XRGrabPoser does not have an XRGrabInteractable assigned." + "  (Parent name) " + transform.parent);

            if (!rb && interactable)
                rb = interactable.GetComponent<Rigidbody>();
        }

        private void SetAttachForInstantaneous(HandAnimator hand)
        {
            if (!hand) return;
            if (interactable.movementType != XRBaseInteractable.MovementType.Instantaneous) return;
            if (!CheckIfPoseExistForHand(hand)) return;

            //Instantaneous movement uses the rigidbody center of mass as the attachment point. This updates that to the left or right attachpoint
            var position = hand.handType == LeftRight.Left ? leftHandAttach.position : rightHandAttach.position;
            position = rb.transform.InverseTransformPoint(position);
            interactable.GetComponent<Rigidbody>().centerOfMass = position;
            MoveHandToObject = false;
        }

        private void MoveHandToPoseTransforms(HandAnimator hand)
        {
            //Determines if the left or right hand is grabbed, and then sends over the proper attachment point to be assigned to the XRGrabInteractable.
            var attachPoint = hand.handType == LeftRight.Left ? leftHandAttach : rightHandAttach;
            hand.MoveHandToTarget(attachPoint, interactable.attachEaseInTime, MoveHandToObject);
        }

        protected override void BeginNewHandPoses(HandAnimator hand)
        {
            if (!hand || !CheckIfPoseExistForHand(hand)) return;

            base.BeginNewHandPoses(hand);

            if (MoveHandToObject)
                MoveHandToPoseTransforms(hand);
        }

        private bool CheckIfPoseExistForHand(HandAnimator hand)
        {
            if (leftHandPose && hand.handType == LeftRight.Left)
                return true;
            if (rightHandPose && hand.handType == LeftRight.Right)
                return true;
            return false;
        }
    }
}