// Author MikeNspired. 

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace MikeNspired.UnityXRHandPoser
{
    /// <summary>
    /// Required on controllers for handposer to work.
    /// References the XRGrabinteractable because the hand will unparent it self when grabbed.
    /// This allows the scripts to quickly reference the hand.
    /// </summary>
    public class HandReference : MonoBehaviour
    {
        public HandAnimator Hand;
        public LeftRight LeftRight;
        public HandReference otherHand;
        public NearFarInteractor xrDirectInteractor;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Transform attachTransform;
        private HandPoser currentHandPoser;

        private void OnValidate()
        {
            if (!Hand)
                Hand = GetComponentInChildren<HandAnimator>();
            if (!xrDirectInteractor)
                xrDirectInteractor = GetComponent<NearFarInteractor>();
        }

        private void Start() => OnValidate();

        private void Awake()
        {
            xrDirectInteractor.selectEntered.AddListener(OnGrab);
            xrDirectInteractor.selectExited.AddListener(x => ResetAttachTransform());

            startPosition = xrDirectInteractor.attachTransform.localPosition;
            startRotation = xrDirectInteractor.attachTransform.localRotation;
            attachTransform = xrDirectInteractor.attachTransform;
        }


        private void OnGrab(SelectEnterEventArgs arg0)
        {
            currentHandPoser = arg0.interactableObject.transform.GetComponent<XRHandPoser>();
            if (!currentHandPoser)
                currentHandPoser = arg0.interactableObject.transform.GetComponentInChildren<XRHandPoser>();

            if (currentHandPoser)
            {
                var interactableAttach = LeftRight == LeftRight.Left
                    ? currentHandPoser.leftHandAttach
                    : currentHandPoser.rightHandAttach;

                Vector3 finalPosition = interactableAttach.localPosition * -1;
                Quaternion finalRotation = Quaternion.Inverse(interactableAttach.localRotation);

                finalPosition = RotatePointAroundPivot(finalPosition, Vector3.zero, finalRotation.eulerAngles);

                attachTransform.localPosition = finalPosition;
                attachTransform.localRotation = finalRotation;
            }

            attachTransform.parent = transform;
        }

        public void ResetAttachTransform()
        {
            attachTransform.parent = Hand.transform;
            attachTransform.localPosition = startPosition;
            attachTransform.localRotation = startRotation;
        }

        public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            Vector3 direction = point - pivot;
            direction = Quaternion.Euler(angles) * direction;
            return direction + pivot;
        }
    }
}