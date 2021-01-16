using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    /// <summary>
    /// Dont use this if you plan on using multiple headsets. This is for single headset and prototyping/testing
    /// </summary>
    public class GrabInteractableButtonEvents : MonoBehaviour
    {
        [SerializeField] private XRGrabInteractable xrGrabInteractable = null;
        [SerializeField] private ActionBasedController controller = null;
        [SerializeField] private InputHelpers.Button activationButton = InputHelpers.Button.SecondaryButton;
        [SerializeField] private float activationThreshold = .5f;
        [SerializeField] private bool toggleOnActivate = true;
        private bool isButtonHeld;
        public UnityEvent OnButtonPressed;
        public UnityEvent OnButtonReleased;

        private void Start()
        {
            xrGrabInteractable.onSelectEntered.AddListener(OnGrabbed);
            xrGrabInteractable.onSelectExited.AddListener(OnRelease);
        }

        private void OnRelease(XRBaseInteractor interactor)
        {
            controller = null;
        }

        private void OnGrabbed(XRBaseInteractor interactor)
        {
            controller = interactor.GetComponent<ActionBasedController>();
        }

        private void Update()
        {
            if (controller)
                CheckController(controller, ref isButtonHeld);
        }

        private void CheckController(ActionBasedController controller, ref bool isGripped)
        {
        //     if (!controller.inputDevice.IsPressed(activationButton, out bool isActive, activationThreshold)) return;
        //
        //     if (!isGripped && isActive)
        //     {
        //         isGripped = true;
        //         OnButtonPressed.Invoke();
        //     }
        //     else if (isGripped && !isActive && !toggleOnActivate)
        //     {
        //         OnButtonReleased.Invoke();
        //     }
        //
        //     if (!isActive)
        //         isGripped = false;
         }
    }
}