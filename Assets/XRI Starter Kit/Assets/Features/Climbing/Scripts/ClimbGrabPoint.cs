using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace MikeNspired.UnityXRHandPoser
{
    public class ClimbGrabPoint : VelocityTracker
    {
        [SerializeField] private XRBaseInteractable xrGrabInteractable;
        [SerializeField] private PlayerClimbingXR playerClimbingXR;
        private XRDirectInteractor xRDirectInteractor;
        public float hapticDuration = .1f;
        public float hapticStrength = .5f;

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
            // Get the interactor and its associated components
            var interactor = args.interactorObject as XRDirectInteractor;
            if (interactor == null) return;

            var interactorTransform = args.interactorObject.transform;
            var controller = interactorTransform.GetComponentInParent<ControllerInputActionManager>();
            var xrOrigin = interactorTransform.GetComponentInParent<XROrigin>();
            var controllerHaptic = controller.GetComponent<HapticImpulsePlayer>();

            if (controller != null)
                SetClimberHand(controller);

            if (xrOrigin != null)
                SetTrackedObject(xrOrigin.transform);

            StartTracking();

            if (controllerHaptic != null)
                controllerHaptic.SendHapticImpulse(hapticStrength, hapticDuration);
        }

        private void SetClimberHand(ControllerInputActionManager controller) => playerClimbingXR.SetClimbHand(controller);

        private void OnSelectExit(SelectExitEventArgs args)
        {
            // Get the interactor and its associated components
            var interactor = (XRDirectInteractor)args.interactorObject;
            if (interactor == null) return;

            var controller = interactor.transform.GetComponentInParent<ControllerInputActionManager>();
            if (controller != null)
            {
                playerClimbingXR.RemoveClimbHand(controller);

                // Set the released velocity
                playerClimbingXR.SetReleasedVelocity(CurrentSmoothedVelocity);

                // Stop tracking the interactor
                StopTracking();
            }
        }
    }
}