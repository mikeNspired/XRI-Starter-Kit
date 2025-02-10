using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    public class PullInteraction : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private AudioRandomize pullBackAudio, arrowNotchedAudio, launchClip;
        [SerializeField] private Transform start, end, stringPosition;

        [SerializeField] private AutoSpawnObjectInHandOnGrab autoSpawnObjectInHandOnGrab;

        private XRBaseInteractable xrBaseInteractable; // For bow grabbing events
        private XRBaseInteractor currentInteractor; // The hand grabbing the bow
        private HapticImpulsePlayer currentHapticImpulsePlayer; // Haptic feedback
        private float pullAmount;
        private bool isSelected = false, canPlayPullBackSound = true;
        private Arrow notchedArrow; // The arrow currently notched on the bow
        private Collider[] colliders; // Used when releasing the arrow
        private float lastFramePullBack;

        public Arrow NotchedArrow => notchedArrow;

        private void OnValidate()
        {
            // Ensure the bow has an XRBaseInteractable
            if (!xrBaseInteractable)
                xrBaseInteractable = GetComponent<XRBaseInteractable>();
        }

        private void Start()
        {
            OnValidate();

            // Cache all bow-related colliders so the arrow won't collide with the bow on release
            colliders = transform.parent.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
                Debug.LogWarning("No colliders found for the bow. Ensure your setup is correct.");

            // Listen for when the bow is grabbed/released
            xrBaseInteractable.selectEntered.AddListener(OnSelectEntered);
            xrBaseInteractable.selectExited.AddListener(OnSelectExited);
        }

        /// <summary>
        /// Detects if an arrow that is currently held by an interactor enters the bow's trigger.
        /// If so, we 'notch' it: remove it from the player's hand, attach it to the bow.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // If there's already an arrow notched, do nothing
            if (notchedArrow) return;

            // Check if the collider has an attached Rigidbody
            var attachedRigidbody = other.attachedRigidbody;
            if (attachedRigidbody == null) return;

            // Check if the Rigidbody has an XRGrabInteractable component
            if (!attachedRigidbody.TryGetComponent<XRGrabInteractable>(out var arrowInteractable)) return;

            // Ensure the XRGrabInteractable is currently selected (held by an interactor)
            if (!arrowInteractable.isSelected) return;

            // Check if the firstInteractorSelecting is an XRBaseInteractor
            if (!(arrowInteractable.firstInteractorSelecting is XRBaseInteractor arrowHolder)) return;

            // Finally, check if the interactable has an Arrow component
            if (!attachedRigidbody.TryGetComponent(out Arrow arrow)) return;

            // If all conditions are met, handle the arrow interaction
            HandleArrowInteraction(arrowHolder, arrow, arrowInteractable);
        }

        private void HandleArrowInteraction(XRBaseInteractor arrowHolder, Arrow arrow,
            XRGrabInteractable arrowInteractable)
        {
            // Remove arrow from the hand
            arrowHolder.interactionManager.SelectExit((IXRSelectInteractor)arrowHolder, arrowInteractable);

            // Set up the arrow so it's 'notched' onto the bow
            notchedArrow = arrow;
            notchedArrow.transform.SetParent(transform);
            notchedArrow.transform.SetLocalPositionAndRotation(start.localPosition, start.localRotation);

            // Disable arrow's physics and grabbing
            if (notchedArrow.TryGetComponent<Rigidbody>(out var arrowRigidbody))
                arrowRigidbody.isKinematic = true;
            arrowInteractable.enabled = false;

            arrowNotchedAudio.Play();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // The interactor that grabbed the bow
            currentInteractor = args.interactorObject as XRBaseInteractor;
            if (currentInteractor == null) return;

            isSelected = true;

            // Haptic feedback
            currentHapticImpulsePlayer = currentInteractor.GetComponentInParent<HapticImpulsePlayer>();
            currentHapticImpulsePlayer?.SendHapticImpulse(0.7f, 0.05f);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            var interactor = args.interactorObject as XRBaseInteractor;
            if (interactor == null) return;

            isSelected = false;
            currentInteractor = null;
            canPlayPullBackSound = true;

            // Play the bow-release sound
            launchClip.Play(pullAmount);

            // If the bow was pulled back, fire the arrow
            if (pullAmount > 0 && notchedArrow)
            {
                notchedArrow.transform.SetParent(null);
                notchedArrow.Release(pullAmount, colliders);
                notchedArrow = null;

                autoSpawnObjectInHandOnGrab.TrySpawn();
            }

            // Reset pull
            pullAmount = 0f;
            skinnedMeshRenderer.SetBlendShapeWeight(0, 0);
        }

        private void Update()
        {
            if (!isSelected || currentInteractor == null)
                return;

            // Calculate how far the bow string is pulled based on the interactor's position
            Vector3 pullPosition = currentInteractor.transform.position;
            pullAmount = CalculatePull(pullPosition);

            // Update blend shape
            skinnedMeshRenderer.SetBlendShapeWeight(0, pullAmount * 100);

            // Move the bowstring in 3D space
            stringPosition.position = Vector3.Lerp(start.position, end.position, pullAmount);
            stringPosition.rotation = Quaternion.Lerp(start.rotation, end.rotation, pullAmount);

            // If an arrow is notched, move it with the bowstring
            if (notchedArrow != null)
                notchedArrow.transform.SetPositionAndRotation(stringPosition.position, stringPosition.rotation);

            // Provide haptic feedback and play the pull audio at a certain threshold
            if (pullAmount > 0.3f)
            {
                // Slight haptic bump if the pull changed significantly
                if (Mathf.Abs(lastFramePullBack - pullAmount) > 0.01f)
                    currentHapticImpulsePlayer?.SendHapticImpulse(pullAmount / 5f, 0.05f);

                // Play the pull audio once
                if (canPlayPullBackSound)
                {
                    canPlayPullBackSound = false;
                    pullBackAudio.Play();
                }
            }
            else
            {
                canPlayPullBackSound = true;
            }

            lastFramePullBack = pullAmount;
        }

        /// <summary>
        /// Calculates a 0..1 pull amount by projecting the interactor's position onto
        /// the line between 'start' and 'end'. 
        /// </summary>
        private float CalculatePull(Vector3 pullPosition)
        {
            Vector3 pullDirection = pullPosition - start.position;
            Vector3 targetDirection = end.position - start.position;
            float maxLength = targetDirection.magnitude;

            targetDirection.Normalize();
            float pullValue = Vector3.Dot(pullDirection, targetDirection) / maxLength;
            return Mathf.Clamp(pullValue, 0, 1);
        }
    }
}
