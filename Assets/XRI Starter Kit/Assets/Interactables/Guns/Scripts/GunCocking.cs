using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Custom interactable that can be dragged along an axis. Can either be continuous or snap to integer steps.
    /// </summary>
    public class GunCocking : MonoBehaviour
    {
        [SerializeField] private XRBaseInteractable xrGrabInteractable = null;
        [SerializeField] private XRGrabInteractable mainGrabInteractable = null;
        [SerializeField] private ProjectileWeapon projectileWeapon = null;
        [SerializeField] private Vector3 LocalAxis = -Vector3.forward;
        [SerializeField] private float AxisLength = .1f;
        [SerializeField] private float ReturnSpeed = 1;
        [SerializeField] private AudioRandomize pullBackAudio = null;
        [SerializeField] private AudioRandomize releaseAudio = null;

        private IXRSelectInteractor currentHand, grabbingInteractor;
        private XRInteractionManager interactionManager;
        private Transform originalParent;
        private Vector3 grabbedOffset, endPoint, startPoint;
        private float currentDistance;
        private bool hasReachedEnd, isSelected;
        private Rigidbody rb;
        public UnityEvent GunCockedEvent;
        

        private void Start()
        {
            xrGrabInteractable.selectEntered.AddListener(OnGrabbed);
            xrGrabInteractable.selectExited.AddListener(OnRelease);
            mainGrabInteractable.selectExited.AddListener(ReleaseIfMainHandReleased);

            originalParent = transform.parent;
            LocalAxis.Normalize();

            //Length can't be negative, a negative length means an inverted axis, so fix that
            if (AxisLength < 0)
            {
                LocalAxis *= -1;
                AxisLength *= -1;
            }

            startPoint = transform.localPosition;
            endPoint = transform.localPosition + LocalAxis * AxisLength;
        }

        private void OnEnable()
        {
            OnValidate();

            interactionManager.UnregisterInteractable(xrGrabInteractable as IXRInteractable);
            interactionManager.RegisterInteractable(xrGrabInteractable as IXRInteractable);
        }
 
        private void OnValidate()
        {
            if (!interactionManager)
                interactionManager = FindFirstObjectByType<XRInteractionManager>();
            if (!xrGrabInteractable)
                xrGrabInteractable = GetComponent<XRBaseInteractable>();
            if (!mainGrabInteractable)
                mainGrabInteractable = transform.parent.GetComponentInParent<XRGrabInteractable>();
            if (!projectileWeapon)
                projectileWeapon = GetComponentInParent<ProjectileWeapon>();
        }

        public Vector3 GetEndPoint() => endPoint;
        public Vector3 GetStartPoint() => startPoint;

        public void FixedUpdate()
        {
            if (stopAnimation) return;

            if (isSelected)
                SlideFromHandPosition();
            else
                ReturnToOriginalPosition();
        }

        private void ReleaseIfMainHandReleased(SelectExitEventArgs arg0)
        {
            if (currentHand?.transform && xrGrabInteractable)
                interactionManager.SelectExit(currentHand,
                    xrGrabInteractable.GetComponent<IXRSelectInteractable>());
        }

        private void SlideFromHandPosition()
        {
            Vector3 worldAxis = transform.TransformDirection(LocalAxis);

            Vector3 distance = grabbingInteractor.transform.position - transform.position - grabbedOffset;
            float projected = Vector3.Dot(distance, worldAxis);

            Vector3 targetPoint;
            if (projected > 0)
                targetPoint = Vector3.MoveTowards(transform.localPosition, endPoint, projected);
            else
                targetPoint = Vector3.MoveTowards(transform.localPosition, startPoint, -projected);

            Vector3 move = targetPoint - transform.localPosition;

            transform.localPosition += move;

            if (hasReachedEnd == false && (transform.localPosition - endPoint).magnitude <= .001f)
            {
                hasReachedEnd = true;
                pullBackAudio.Play();
            }
        }

        private void ReturnToOriginalPosition()
        {
            Vector3 targetPoint = Vector3.MoveTowards(transform.localPosition, startPoint, ReturnSpeed * Time.deltaTime);
            Vector3 move = targetPoint - transform.localPosition;

            transform.localPosition += move;

            if (hasReachedEnd && (transform.localPosition - startPoint).magnitude <= .001f)
            {
                hasReachedEnd = false;
                GunCockedEvent.Invoke();
                releaseAudio.Play();
                SetClosed();
            }
        }

        private bool stopAnimation;

        public void SetClosed()
        {
            stopAnimation = false;
        }

        private void OnGrabbed(SelectEnterEventArgs arg0)
        {
            var interactor = arg0.interactorObject;
            stopAnimation = false;
            currentHand = interactor;
            isSelected = true;
            grabbedOffset = interactor.transform.position - transform.position;
            grabbingInteractor = interactor;
            transform.parent = originalParent;
            transform.localPosition = startPoint;
        }

        private void OnRelease(SelectExitEventArgs arg0)
        {
            currentHand = null;
            isSelected = false;
            transform.localPosition = startPoint;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 end = transform.position + transform.TransformDirection(LocalAxis.normalized) * AxisLength;
            Gizmos.DrawLine(transform.position, end);
            Gizmos.DrawSphere(end, 0.01f);
        }

        public void Pause()
        {
            stopAnimation = true;
        }
    }
}