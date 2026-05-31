using XR.Interaction.Toolkit.Samples;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    public class ClimbGrabPoint : VelocityTracker
    {
        [SerializeField] private XRBaseInteractable xrGrabInteractable;
        [SerializeField] private PlayerClimbingXR playerClimbingXR;

        // Haptic settings
        public float hapticDuration = .1f;
        public float hapticStrength = .5f;

        public XRBaseInteractable GrabInteractable => xrGrabInteractable;

        protected new void Start()
        {
            base.Start();
            OnValidate();

            xrGrabInteractable.selectEntered.AddListener(OnSelect);
            xrGrabInteractable.selectExited.AddListener(OnSelectExit);
        }

        private void OnValidate()
        {
            if (!playerClimbingXR)
                playerClimbingXR = FindFirstObjectByType<PlayerClimbingXR>();
            if (!xrGrabInteractable)
                xrGrabInteractable = GetComponent<XRBaseInteractable>();
        }

        private void OnSelect(SelectEnterEventArgs args)
        {
            var interactor = args.interactorObject as XRBaseInteractor;
            if (interactor == null) return;

            var interactorTransform = interactor.transform;
            var controller = interactorTransform.GetComponentInParent<ControllerInputActionManager>();
            var xrOrigin = interactorTransform.GetComponentInParent<XROrigin>();
            var controllerHaptic = controller.GetComponent<HapticImpulsePlayer>();

            // **Pass the grabbed object's transform so we can track it**
            if (controller != null)
            {
                playerClimbingXR.SetClimbHand(controller, xrGrabInteractable.transform);
            }

            // Begin velocity tracking for fling logic
            if (xrOrigin != null)
                SetTrackedObject(xrOrigin.transform);

            StartTracking();

            // Fire haptic
            if (controllerHaptic != null)
                controllerHaptic.SendHapticImpulse(hapticStrength, hapticDuration);
        }

        private void OnSelectExit(SelectExitEventArgs args)
        {
            var interactor = args.interactorObject as XRBaseInteractor;
            if (interactor == null) return;

            var controller = interactor.transform.GetComponentInParent<ControllerInputActionManager>();
            if (controller != null)
            {
                // Tell the climbing script that this hand is releasing
                playerClimbingXR.RemoveClimbHand(controller);

                // Provide fling velocity on release
                playerClimbingXR.SetReleasedVelocity(CurrentSmoothedVelocity);

                // Stop our velocity tracker
                StopTracking();
            }
        }
    }
}
