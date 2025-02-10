using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MikeNspired.XRIStarterKit
{
    public class PlayerCrouch : MonoBehaviour
    {
        [SerializeField] private InputActionReference crouchLeftHand, crouchRightHand;
        [SerializeField] private float crouchOffSetReduction = .65f;
        public XROrigin xrOrigin;
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

            if (!xrOrigin) xrOrigin = GetComponent<XROrigin>();
            if (!xrOrigin) xrOrigin = GetComponentInParent<XROrigin>();
            crouchOffset = xrOrigin.CameraYOffset;
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
            switch (isCrouched)
            {
                case true:
                    xrOrigin.CameraYOffset = crouchOffset;
                    isCrouched = !isCrouched;
                    break;
                case false:
                    xrOrigin.CameraYOffset = crouchOffset * crouchOffSetReduction;
                    isCrouched = !isCrouched;
                    break;
            }
        }
    }
}