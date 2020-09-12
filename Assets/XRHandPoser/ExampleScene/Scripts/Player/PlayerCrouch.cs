using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    //Thanks to David Goodman 
    public class PlayerCrouch : MonoBehaviour
    {
        [SerializeField] private XRController leftController, rightController;
        [SerializeField] private InputHelpers.Button activationButton;
        [SerializeField] private float activationThreshold = .5f, crouchOffSetReduction = .65f;
        [SerializeField] private bool toggleOnActivate;
        private XRRig xrRig;
        private bool leftIsGripped, rightIsGripped, isCrouched;
        private float crouchOffset;

    
        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            if (!xrRig) xrRig = GetComponent<XRRig>();
            crouchOffset = xrRig.cameraYOffset;
        }

        private void CrouchToggle()
        {
            if (isCrouched)
            {
                xrRig.cameraYOffset = crouchOffset;
                isCrouched = !isCrouched;
            }
            else if (!isCrouched)
            {
                xrRig.cameraYOffset = crouchOffset * crouchOffSetReduction;
                isCrouched = !isCrouched;
            }
        }

        private void Update()
        {
            if (leftController)
                CheckController(leftController, ref leftIsGripped);
            if (rightController)
                CheckController(rightController, ref rightIsGripped);
        }
        
        private void CheckController(XRController controller, ref bool isGripped)
        {
            if (!controller.inputDevice.IsPressed(activationButton, out bool isActive, activationThreshold)) return;

            if (!isGripped && isActive)
            {
                isGripped = true;
                CrouchToggle();
            }
            else if (isGripped && !isActive && !toggleOnActivate)
            {
                CrouchToggle();
            }

            if (!isActive)
                isGripped = false;
        }
    }
}

