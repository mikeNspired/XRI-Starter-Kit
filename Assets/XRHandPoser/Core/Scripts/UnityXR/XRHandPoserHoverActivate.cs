// Author MikeNspired. 
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class XRHandPoserHoverActivate : HandPoser
    {
        [SerializeField] private XRBaseInteractable mainInteractable;
        private HandAnimator currentHand;

        public UnityEvent OnActivate;
        public UnityEvent OnDeactivate;

        protected override void Awake()
        {
            base.Awake();
            OnValidate();

            mainInteractable.onSelectExited.AddListener((x) => ReleaseHand());
        }

        private void OnValidate()
        {
            if (!mainInteractable)
                mainInteractable = GetComponentInParent<XRBaseInteractable>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentHand) return;
            var hand = other.GetComponentInParent<HandReference>();
            if (!hand) return;
            if (hand.GetComponentInChildren<XRDirectInteractor>().selectTarget) return;
            
            currentHand = hand.Hand;

            BeginNewHandPoses(currentHand);
            OnActivate.Invoke();
            currentHand.NewPoseStarting += ReleaseHand;
        }
        
        private void OnTriggerExit(Collider other)
        {
            var hand = other.GetComponentInParent<HandReference>();
            if (!hand) return;
            if (hand.GetComponentInChildren<XRDirectInteractor>().selectTarget) return;
            ReleaseHand();
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
            OnDeactivate.Invoke();
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