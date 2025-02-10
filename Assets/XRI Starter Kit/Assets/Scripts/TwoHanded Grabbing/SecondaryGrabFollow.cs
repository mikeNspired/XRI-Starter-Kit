using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    public class SecondaryGrabFollow : MonoBehaviour
    {
        [Header("Interactable References")]
        [SerializeField] 
        private XRGrabInteractable mainInteractable;

        [SerializeField]
        protected XRBaseInteractable interactable;

        [Header("Rotation Options")]
        [SerializeField]
        private bool rotateOnY = false, rotateOnZ = false;

        private XRBaseInteractor currentHand, mainGripHand;
        private XRInteractionManager interactionManager;

        // Cached transforms and rotations
        private Transform interactableStartingParent, newHandAttachTransform, startingParent;
        private Quaternion mainInteractableHandAttachTransformStartingRotation;
        private Vector3 mainHandAttachStartingRotation, mainHandStartingRotation;
        private TransformStruct interactableStartingTransformData;

        void Awake()
        {
            OnValidate();

            // Subscribe to the XR Interaction events
            interactable.selectEntered.AddListener(OnSecondarySelectEntered);
            interactable.selectExited.AddListener(OnSecondarySelectExited);

            mainInteractable.selectEntered.AddListener(OnMainSelectEntered);
            mainInteractable.selectExited.AddListener(OnMainSelectExited);

            // Record original local transform (position, rotation, scale)
            interactableStartingTransformData.SetTransformStruct(
                interactable.transform.localPosition,
                interactable.transform.localRotation,
                interactable.transform.localScale
            );
            interactableStartingParent = interactable.transform.parent;

            // Create a helper transform for rotation
            newHandAttachTransform = new GameObject(mainInteractable.name + " Follow Hand Helper").transform;
            newHandAttachTransform.parent = transform;
        }

        void OnValidate()
        {
            if (!interactable)
                interactable = GetComponent<XRGrabInteractable>();

            if (!interactionManager)
                interactionManager = FindFirstObjectByType<XRInteractionManager>();
        }

        /// <summary>
        /// Called when the secondary hand (this interactable) is grabbed.
        /// </summary>
        private void OnSecondarySelectEntered(SelectEnterEventArgs args)
        {
            currentHand = args.interactorObject as XRBaseInteractor;
            SetStartingFromToRotation(); // position the attach transform
            mainHandStartingRotation = mainGripHand.transform.parent.localEulerAngles;
            mainHandAttachStartingRotation = mainGripHand.attachTransform.localEulerAngles;
        }

        /// <summary>
        /// Called when the secondary hand (this interactable) is released.
        /// </summary>
        private void OnSecondarySelectExited(SelectExitEventArgs args)
        {
            // Restore original local transform
            var t = interactable.transform;
            t.parent = interactableStartingParent;
            t.localPosition = interactableStartingTransformData.position;
            t.localRotation = interactableStartingTransformData.rotation;
            t.localScale = interactableStartingTransformData.scale;

            ResetMainHandAttachTransform();
            currentHand = null;
        }

        /// <summary>
        /// Called when the main hand (mainInteractable) is grabbed.
        /// </summary>
        private void OnMainSelectEntered(SelectEnterEventArgs args)
        {
            mainGripHand = args.interactorObject as XRBaseInteractor;
            if (mainGripHand == null) return;
            startingParent = mainGripHand.attachTransform.parent;
            mainInteractableHandAttachTransformStartingRotation = mainGripHand.attachTransform.localRotation;
        }

        /// <summary>
        /// Called when the main hand (mainInteractable) is released.
        /// </summary>
        private void OnMainSelectExited(SelectExitEventArgs args)
        {
            // If secondary hand is still holding, force release
            if (currentHand)
                interactionManager.SelectExit(currentHand, (IXRSelectInteractable) interactable);

            // Reset main hand attach transform
            mainGripHand.GetComponentInParent<HandReference>()?.ResetAttachTransform();
            mainGripHand = null;
        }

        /// <summary>
        /// Reset the main hand attach transform to its original parent/rotation.
        /// </summary>
        private void ResetMainHandAttachTransform()
        {
            // If there is no current secondary hand, nothing to do
            if (!currentHand) return;
            if (!mainGripHand) return;

            // Restore the attach transform
            mainGripHand.attachTransform.parent = startingParent;
            mainGripHand.attachTransform.localRotation = mainInteractableHandAttachTransformStartingRotation;
            newHandAttachTransform.parent = transform;
        }

        void Update()
        {
            // If both hands are active, run the rotation logic
            if (currentHand && mainGripHand)
                SetRotation();
        }

        /// <summary>
        /// Position the helper transform to face from the main hand attach to the secondary hand.
        /// Then re-parent the main grip's attach transform under that helper.
        /// </summary>
        private void SetStartingFromToRotation()
        {
            newHandAttachTransform.position = mainGripHand.attachTransform.position;
            newHandAttachTransform.LookAt(currentHand.transform);
            newHandAttachTransform.parent = mainGripHand.attachTransform.parent;

            mainGripHand.attachTransform.parent = newHandAttachTransform;
        }

        /// <summary>
        /// Handle rotation constraints (rotate on Y or Z).
        /// </summary>
        private void SetRotation()
        {
            // Orient the helper transform to look at the secondary hand
            newHandAttachTransform.LookAt(currentHand.transform);

            // Start with the original attach transform rotation
            var r = mainHandAttachStartingRotation;

            if (rotateOnZ)
            {
                // Compare difference in localEulerAngles.z
                float rToAdd = mainGripHand.transform.parent.localEulerAngles.z - mainHandStartingRotation.z;
                mainGripHand.attachTransform.localEulerAngles = new Vector3(r.x, r.y, r.z + rToAdd);
            }
            else if (rotateOnY)
            {
                // Compare difference in localEulerAngles.y
                float rToAdd = mainGripHand.transform.localEulerAngles.y - mainHandStartingRotation.y;
                mainGripHand.attachTransform.localEulerAngles = r;
                mainGripHand.attachTransform.Rotate(mainGripHand.attachTransform.up, rToAdd, Space.World);
            }
        }
    }
}
