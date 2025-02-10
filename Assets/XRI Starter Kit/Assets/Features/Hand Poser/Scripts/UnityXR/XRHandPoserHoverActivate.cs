// Author MikeNspired.
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
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

            if (mainInteractable != null)
            {
                mainInteractable.selectExited.AddListener(OnSelectExited);
            }
        }

        private void OnValidate()
        {
            if (!mainInteractable)
                mainInteractable = GetComponentInParent<XRBaseInteractable>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentHand) return;

            var handReference = other.GetComponentInParent<HandReference>();
            if (handReference == null) return;

            var interactor = handReference.GetComponentInChildren<NearFarInteractor>();
            if (interactor != null && interactor.firstInteractableSelected != null) return;

            currentHand = handReference.Hand;
            BeginNewHandPoses(currentHand);
            OnActivate.Invoke();
            currentHand.NewPoseStarting += ReleaseHand;
        }

        private void OnTriggerExit(Collider other)
        {
            var handReference = other.GetComponentInParent<HandReference>();
            if (handReference == null) return;

            var interactor = handReference.GetComponentInChildren<NearFarInteractor>();
            if (interactor != null && interactor.firstInteractableSelected != null) return;
            ReleaseHand();
        }

        private void OnSelectExited(SelectExitEventArgs args) => ReleaseHand();

        private void ReleaseHand(bool isGrabbingItem)
        {
            if (isGrabbingItem) ReleaseHand();
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
            var attachPoint = hand.handType == LeftRight.Left ? leftHandAttach : rightHandAttach;
            hand.MoveHandToTarget(attachPoint, 0, false);
        }

        protected override void BeginNewHandPoses(HandAnimator hand)
        {
            if (!hand || !CheckIfCorrectHand(hand)) return;

            base.BeginNewHandPoses(hand);
            MoveHandToPoseTransforms(hand);
        }

        private bool CheckIfCorrectHand(HandAnimator hand) =>
            (leftHandPose && hand.handType == LeftRight.Left) ||
            (rightHandPose && hand.handType == LeftRight.Right);
    }
}
