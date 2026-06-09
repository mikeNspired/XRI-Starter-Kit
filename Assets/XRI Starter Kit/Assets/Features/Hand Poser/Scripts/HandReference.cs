using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    public class HandReference : MonoBehaviour
    {
        [field: SerializeField] public HandAnimator Hand { get; private set; }
        [field: SerializeField] public LeftRight LeftRight { get; private set; }
        [field: SerializeField] public HandReference OtherHand { get; private set; }
        [field: SerializeField] public NearFarInteractor NearFarInteractor { get; private set; }

        
        private Transform handModelAttach; // Extra transform match hand model attach transform
        private Transform attachTransform;
        private Vector3 originalLocalPos;
        private Quaternion originalLocalRot;
        private HandPoser currentHandPoser;


        private void OnValidate()
        {
            if (!Hand)
                Hand = GetComponentInChildren<HandAnimator>();
            if (!NearFarInteractor)
                NearFarInteractor = GetComponent<NearFarInteractor>();
        }

        private void Awake()
        {
            NearFarInteractor.selectEntered.AddListener(OnGrab);
            NearFarInteractor.selectExited.AddListener(_ => ResetAttachTransform());

            attachTransform = NearFarInteractor.attachTransform;
            originalLocalPos = attachTransform.localPosition;
            originalLocalRot = attachTransform.localRotation;

            // Create the dummy transform parented under the hand model
            handModelAttach = new GameObject("HandModelAttach" + name).transform;
            handModelAttach.SetParent(Hand.transform, false);
            handModelAttach.localPosition = Vector3.zero;
            handModelAttach.localRotation = Quaternion.identity;
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            // Skip if far interaction
            if (NearFarInteractor.interactionAttachController.hasOffset)
                return;

            FindHandPoser(args);
            if (!currentHandPoser)
                return;

            // Dynamic grab (off-axis / no authored pose): hold the object where it was grabbed
            // instead of snapping its authored grip into the palm. Aligning the interactor attach
            // to the object's CURRENT attach pose makes XRI's grab target == current → no teleport,
            // so the curl solver wraps the fingers onto the geometry where the hand actually is.
            // (Mechanic proven in isolation by HoldInPlaceOnGrab.) Authored path below is unchanged.
            if (currentHandPoser is XRHandPoser xrPoser && xrPoser.IsGrabDynamic(Hand))
            {
                var objectAttach = args.interactableObject.GetAttachTransform(args.interactorObject);
                if (objectAttach)
                    attachTransform.SetPositionAndRotation(objectAttach.position, objectAttach.rotation);
                return;
            }

            var interactableAttach = LeftRight == LeftRight.Left
                ? currentHandPoser.leftHandAttach
                : currentHandPoser.rightHandAttach;

            Vector3 finalPosition = interactableAttach.localPosition * -1;
            Quaternion finalRotation = Quaternion.Inverse(interactableAttach.localRotation);

            finalPosition = RotatePointAroundPivot(finalPosition, Vector3.zero, finalRotation.eulerAngles);

            handModelAttach.localPosition = finalPosition;
            handModelAttach.localRotation = finalRotation;

            // We read out its *world* position/rotation, and apply that to the XR attach transform:
            attachTransform.position = handModelAttach.position;
            attachTransform.rotation = handModelAttach.rotation;
        }

        private void FindHandPoser(SelectEnterEventArgs args)
        {
            currentHandPoser =
                args.interactableObject.transform.GetComponent<XRHandPoser>() ??
                args.interactableObject.transform.GetComponentInChildren<XRHandPoser>();
        }

        public void ResetAttachTransform()
        {
            attachTransform.localPosition = originalLocalPos;
            attachTransform.localRotation = originalLocalRot;
        }

        private static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            var direction = point - pivot;
            direction = Quaternion.Euler(angles) * direction;
            return direction + pivot;
        }
    }
}
