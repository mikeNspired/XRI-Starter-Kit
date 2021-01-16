using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class GrabAnywhereOnPole : MonoBehaviour
    {
        [SerializeField] private XRGrabInteractable Interactable = null;

        [SerializeField] private Transform GrabbableHolder = null;
        [SerializeField] private Transform LeftHandGrip = null, RightHandGrip = null;

        [SerializeField] private bool rotateToFollowHand = true;

        [SerializeField] private float maxHeight = 0;
        [SerializeField] private float minHeight = 0;

        private Transform LeftHand, RightHand, currentGrabber;
        private bool leftFollow = true;
        private bool rightFollow = true;


        private void Start()
        {
            OnValidate();
            Interactable.onSelectEntered.AddListener(controller => EnableFollowOnHand(controller, false));
            Interactable.onSelectEntered.AddListener(SetHand);
            Interactable.onSelectExited.AddListener(controller => EnableFollowOnHand(controller, true));
            Interactable.onSelectExited.AddListener(ReleaseHand);
        }


        private void OnValidate()
        {
            if (!Interactable)
                Interactable = GetComponentInParent<XRGrabInteractable>();
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
                Mathf.Clamp(grip.localPosition.y, -minHeight, maxHeight), grip.localPosition.z);


            if (rotateToFollowHand)
            {
                grip.rotation = Quaternion.LookRotation(-((hand.position) - grip.transform.position), transform.up);
            }
        }

        private bool CheckIfPositionInHeightConstraints(Vector3 newPosition)
        {
            return newPosition.y >= (transform.position - transform.up * minHeight).y
                   && newPosition.y <= (transform.position + transform.up * maxHeight).y;
        }

        private void EnableFollowOnHand(XRBaseInteractor hand, bool state)
        {
            currentGrabber = hand.transform;
            if (hand.GetComponent<HandReference>().LeftRight == LeftRight.Left)
                leftFollow = state;
            else
                rightFollow = state;
        }

        private void SetHand(XRBaseInteractor controller)
        {
            currentGrabber = controller.transform;
        }

        private void ReleaseHand(XRBaseInteractor controller)
        {
            if (currentGrabber == controller.transform)
            {
                currentGrabber = null;
                ReleaseHand(controller.GetComponent<HandReference>());
            }
        }

        private void ReleaseHand(HandReference hand)
        {
            if (hand.hand.handType == LeftRight.Left)
            {
                LeftHand = null;
                leftFollow = true;
            }
            else
            {
                RightHand = null;
                rightFollow = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandReference hand = other.GetComponent<HandReference>();

            if (!hand) return;

            if (hand.hand.handType == LeftRight.Left)
                LeftHand = other.transform;
            else
                RightHand = other.transform;
        }


        private void OnTriggerExit(Collider other)
        {
            HandReference hand = other.GetComponent<HandReference>();
            if (!hand) return;
            if (hand.transform == currentGrabber) return;
            ReleaseHand(hand);
        }

        private void OnDrawGizmosSelected()
        {
            var localPosition = GrabbableHolder.localPosition;

            Gizmos.matrix = GrabbableHolder.parent.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(localPosition - Vector3.up * minHeight, localPosition + Vector3.up * maxHeight);
            Gizmos.DrawWireSphere(localPosition - Vector3.up * minHeight, .005f);
            Gizmos.DrawWireSphere(localPosition + Vector3.up * maxHeight, .005f);
        }
    }
}