// Copyright (c) MikeNspired. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class XRHandPoserHoverActivate : HandPoser
    {
        [SerializeField] private XRGrabInteractable mainInteractable;
        private Rigidbody rb;
        private HandAnimator currentHand;

        public UnityEvent OnActivate;
        public UnityEvent OnDeactivate;

        protected override void Awake()
        {
            base.Awake();
            OnValidate();

            mainInteractable.onSelectExit.AddListener((x) => ReleaseHand());
        }

        private void OnValidate()
        {
            if (!rb)
                rb = GetComponent<Rigidbody>();
            if (!mainInteractable)
                mainInteractable = GetComponentInParent<XRGrabInteractable>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentHand) return;
            var hand = other.GetComponent<HandReference>();
            if (!hand) return;
            if (hand.GetComponent<XRDirectInteractor>().selectTarget) return;

            currentHand = hand.hand;

            SetAttachForInstantaneous(currentHand);
            BeginNewHandPoses(currentHand);
            OnActivate.Invoke();
            currentHand.NewPoseStarting += ReleaseHand;
        }


        private void ReleaseHand(bool isGrabbingItem)
        {
            if (!isGrabbingItem) return;

            ReleaseHand();
        }

        private void ReleaseHand()
        {
            if (currentHand == null) return;

            currentHand.NewPoseStarting -= ReleaseHand;
            currentHand = null;
            Release();
            rb.ResetCenterOfMass();
            OnDeactivate.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            var hand = other.GetComponent<HandReference>();
            if (!hand) return;
            if (hand.GetComponent<XRDirectInteractor>().selectTarget) return;
            ReleaseHand();
        }


        private void SetAttachForInstantaneous(HandAnimator hand)
        {
            if (!hand) return;
            if (!CheckIfCorrectHand(hand)) return;

            //Instantaneous movement uses the rigidbody center of mass as the attachment point. This updates that to the left or right attachpoint
            var position = hand.handType == LeftRight.Left ? leftHandAttach.position : rightHandAttach.position;
            rb.transform.InverseTransformPoint(position);
        }

        private void MoveHandToPoseTransforms(HandAnimator hand)
        {
            //Determines if the left or right hand is grabbed, and then sends over the proper attachment point to be assigned to the XRGrabInteractable.
            var attachPoint = hand.handType == LeftRight.Left ? leftHandAttach : rightHandAttach;
            hand.MoveHandToTarget(attachPoint, 0, false);
        }

        protected override void BeginNewHandPoses(HandAnimator hand)
        {
            if (!hand) return;
            if (!CheckIfCorrectHand(hand)) return;

            base.BeginNewHandPoses(hand);

            MoveHandToPoseTransforms(hand);
        }

        private bool CheckIfCorrectHand(HandAnimator hand)
        {
            if (leftHandPose && hand.handType == LeftRight.Left)
                return true;
            if (rightHandPose && hand.handType == LeftRight.Right)
                return true;
            return false;
        }
    }
}