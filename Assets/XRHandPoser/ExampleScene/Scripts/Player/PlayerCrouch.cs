using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    //Thanks to David Goodman 
    public class PlayerCrouch : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference crouchLeftHand, crouchRightHand;
        [SerializeField] private float crouchOffSetReduction = .65f;
        public XRRig xrRig;
        private bool leftIsGripped, rightIsGripped, isCrouched;
        private float crouchOffset;

        private void Awake()
        {
            OnValidate();
            crouchLeftHand.GetInputAction().performed += x => CrouchToggle();
            crouchRightHand.GetInputAction().performed += x => CrouchToggle();
        }

        private void OnValidate()
        {
            if (!gameObject.activeInHierarchy) return;

            if (!xrRig) xrRig = GetComponent<XRRig>();
            if (!xrRig) xrRig = GetComponentInParent<XRRig>();   
            crouchOffset = xrRig.cameraYOffset;
        }
        private void OnEnable()
        {
            crouchLeftHand.EnableAction();
            crouchRightHand.EnableAction();
        } 

        private void OnDisable()
        { 
            crouchLeftHand.DisableAction();
            crouchRightHand.DisableAction();
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
        
    }
}