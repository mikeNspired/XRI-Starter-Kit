using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class ClimbGrabPoint : VelocityTracker
    {
        [SerializeField] private XRBaseInteractable xrGrabInteractable;
        [SerializeField] private PlayerClimbingXR playerClimbingXR;
        private XRBaseController xRController;
        private XRDirectInteractor xRDirectInteractor;
        public float hapticDuration = .1f;
        public float hapticStrength = .5f;

        protected new void Start()
        {
            base.Start();
            OnValidate();
            xrGrabInteractable.onSelectEntered.AddListener(OnSelect);
            xrGrabInteractable.onSelectExited.AddListener(OnSelectExit);
        }

        private void OnValidate()
        {
            if (!playerClimbingXR)
                playerClimbingXR = FindObjectOfType<PlayerClimbingXR>();
            if (!xrGrabInteractable)
                xrGrabInteractable = GetComponent<XRBaseInteractable>();
        }

        private void OnSelect(XRBaseInteractor interactor)
        {
            xRController = interactor.GetComponentInParent<ActionBasedController>();
            xRDirectInteractor = interactor.GetComponent<XRDirectInteractor>();
            if (!xRDirectInteractor) return;

            if (xRDirectInteractor)
                SetClimberHand(xRController);


            //controllerVelocity.SetController(interactor.transform);
            SetTrackedObject(interactor.GetComponentInParent<XROrigin>().transform);
            StartTracking();

            interactor.GetComponentInParent<XRBaseController>().SendHapticImpulse(hapticStrength, hapticDuration);
        }

        private void SetClimberHand(XRBaseController controller)
        {
            playerClimbingXR.SetClimbHand(controller);
        }

        private void OnSelectExit(XRBaseInteractor interactor)
        {
            xRController = interactor.GetComponentInParent<XRBaseController>();
            xRDirectInteractor = interactor.GetComponent<XRDirectInteractor>();

            if (xRDirectInteractor)
            {
                playerClimbingXR.RemoveClimbHand(xRController);

                playerClimbingXR.SetReleasedVelocity(CurrentSmoothedVelocity);
                StopTracking();
            }
        }
    }
}