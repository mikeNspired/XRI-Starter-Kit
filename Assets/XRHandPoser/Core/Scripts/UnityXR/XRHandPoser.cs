// Copyright (c) MikeNspired. All Rights Reserved.

using System;
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
        public XRBaseInteractable interactable;
        public bool MaintainHandOnObject = true;
        public bool WaitTillEaseInTimeToMaintainPosition = true;
        public bool DisableHandAttachTransforms = false;

        private Rigidbody _rigidBody;

        protected override void Awake()
        {
            base.Awake();

            OnValidate();

            SubscribeToSelection();
        }

        // private void Update()
        // {
        //         interactable = GetComponent<XRBaseInteractable>();    
        //     if (!interactable)
        //         interactable = GetComponentInParent<XRBaseInteractable>();
        // }

        private void SubscribeToSelection()
        {
            //Set hand animation on grab
            interactable.selectEntered.AddListener(TryStartPosing);

            //Set to default animations when item is released
            interactable.onSelectExited.AddListener(TryReleaseHand);
        }

        private void TryStartPosing(SelectEnterEventArgs x)
        {
            var hand = x.interactorObject.transform.GetComponentInParent<HandReference>();
            if (!hand) return;
            BeginNewHandPoses(hand.Hand);
        }

        private void TryReleaseHand(XRBaseInteractor x)
        {
            //Simple fix to get sockets to work
            //TODO add hand tracking, to possibly have one handposer instead of two, and to check if the hand released for two handed grabbing
            if (!x.GetComponentInParent<HandReference>()) return;
           
            Release();
           
            if (_rigidBody)
                _rigidBody.ResetCenterOfMass();
        }

        private void OnValidate()
        {
            if (!interactable)
                interactable = GetComponent<XRGrabInteractable>();
            if (!interactable)
                interactable = GetComponentInParent<XRGrabInteractable>();

            if (!interactable)
                Debug.LogWarning(gameObject + " XRGrabPoser does not have an XRGrabInteractable assigned." + "  (Parent name) " + transform.parent);

            if (!_rigidBody && interactable)
                _rigidBody = interactable.GetComponent<Rigidbody>();
        }

        private void MoveHandToPoseTransforms(HandAnimator hand)
        {
            CheckIfGrabInteractable(interactable, out var xrGrabInteractable);
            float attachEaseInTime = 0;
            if (xrGrabInteractable)
                attachEaseInTime = xrGrabInteractable.attachEaseInTime;

            //Determines if the left or right hand is grabbed, and then sends over the proper attachment point to be assigned to the XRGrabInteractable.
            var attachPoint = hand.handType == LeftRight.Left ? leftHandAttach : rightHandAttach;
            hand.MoveHandToTarget(attachPoint, attachEaseInTime, WaitTillEaseInTimeToMaintainPosition);
        }

        protected override void BeginNewHandPoses(HandAnimator hand)
        {
            if (!hand || !CheckIfPoseExistForHand(hand)) return;

            base.BeginNewHandPoses(hand);

            if (MaintainHandOnObject)
                MoveHandToPoseTransforms(hand);
        }

        private void SetAttachForInstantaneous(HandAnimator hand)
        {
            if (!hand) return;
            CheckIfGrabInteractable(interactable, out var xrGrabInteractable);
            if (!xrGrabInteractable || xrGrabInteractable.movementType != XRBaseInteractable.MovementType.Instantaneous) return;
            if (!CheckIfPoseExistForHand(hand)) return;

            //Instantaneous movement uses the rigidbody center of mass as the attachment point. This updates that to the left or right attachpoint
            var position = hand.handType == LeftRight.Left ? leftHandAttach.position : rightHandAttach.position;
            position = _rigidBody.transform.InverseTransformPoint(position);
            interactable.GetComponent<Rigidbody>().centerOfMass = position;
            MaintainHandOnObject = false;
        }

        private bool CheckIfPoseExistForHand(HandAnimator hand)
        {
            if (leftHandPose && hand.handType == LeftRight.Left)
                return true;
            if (rightHandPose && hand.handType == LeftRight.Right)
                return true;
            return false;
        }

        private static void CheckIfGrabInteractable(XRBaseInteractable xrBaseInteractable, out XRGrabInteractable xrGrabInteractable) =>
            xrBaseInteractable.TryGetComponent(out xrGrabInteractable);
    }
}