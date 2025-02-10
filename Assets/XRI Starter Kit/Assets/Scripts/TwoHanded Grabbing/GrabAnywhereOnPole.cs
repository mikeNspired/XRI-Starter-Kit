using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.XRIStarterKit
{
    public class GrabAnywhereOnPole : MonoBehaviour
    {
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable Interactable;
        [SerializeField] private Transform GrabbableHolder,LeftHandGrip, RightHandGrip;
        [SerializeField] private Vector2 length;

        private Transform LeftHand, RightHand;
        private HandReference currentHandGrabbing;
        private bool leftFollow = true, rightFollow = true;


        private void Start()
        {
            OnValidate();
            Interactable.selectEntered.AddListener(controller => EnableFollowOnHand(controller.interactorObject, false));
            Interactable.selectExited.AddListener(controller => EnableFollowOnHand(controller.interactorObject, true));
            Interactable.selectExited.AddListener(ReleaseHand);
            Interactable.attachTransform = GrabbableHolder;
            LeftHandGrip.SetParent(transform);
            RightHandGrip.SetParent(transform);
        }

        private void OnValidate()
        {
            if (!Interactable)
                Interactable = GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        private void Update()
        {
            if (LeftHand && leftFollow) MoveGripPosition(LeftHandGrip, LeftHand);
            if (RightHand && rightFollow) MoveGripPosition(RightHandGrip, RightHand);
        }

        private void MoveGripPosition(Transform grip, Transform hand)
        {
            Vector3 newPosition = Vector3.Project((hand.position - GrabbableHolder.position), transform.up);

            newPosition += GrabbableHolder.position;

            grip.position = newPosition;
            grip.localPosition = new Vector3(grip.localPosition.x,
                Mathf.Clamp(grip.localPosition.y, -length.x, length.y), grip.localPosition.z);
        }

        private void EnableFollowOnHand(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor hand, bool state)
        {
            currentHandGrabbing = hand.transform.GetComponentInParent<HandReference>();

            if (currentHandGrabbing.LeftRight == LeftRight.Left)
            {
                leftFollow = state;
            }
            else
            {
                rightFollow = state;
            }
        }

        private void ReleaseHand(SelectExitEventArgs x)
        {
            var controllerHand = x.interactorObject.transform.GetComponentInParent<HandReference>();
            if (currentHandGrabbing != controllerHand) return;
            if (controllerHand.Hand.handType == LeftRight.Left)
                leftFollow = true;
            else
                rightFollow = true;

            currentHandGrabbing = null;
        }

        private void ReleaseHand(HandReference hand)
        {
            if (hand.Hand.handType == LeftRight.Left)
            {
                LeftHand = null;
                leftFollow = true;
            }
            else
            {
                RightHand = null;
                rightFollow = true;
            }
            currentHandGrabbing = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            HandReference hand = other.GetComponentInParent<HandReference>();

            if (!hand) return;

            if (hand.Hand.handType == LeftRight.Left)
                LeftHand = other.transform;
            else
                RightHand = other.transform;
        }

        private void OnTriggerExit(Collider other)
        {
            HandReference hand = other.GetComponentInParent<HandReference>();
            if (!hand) return;
            if (hand == currentHandGrabbing) return;
            ReleaseHand(hand);
        }

        private void OnDrawGizmosSelected()
        {
            var localPosition = GrabbableHolder.localPosition;

            Gizmos.matrix = GrabbableHolder.parent.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(localPosition - Vector3.up * length.x, localPosition + Vector3.up * length.y);
            Gizmos.DrawWireSphere(localPosition - Vector3.up * length.x, .025f);
            Gizmos.DrawWireSphere(localPosition + Vector3.up * length.y, .025f);
        }
    }
}