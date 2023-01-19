using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class InventoryGrabInteractBackup : MonoBehaviour
    {
        [SerializeField] private InteractButton interactButton = InteractButton.grip;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private bool addToSlotOnRelease = true;

        private bool isLeftGripped, isRightGripped;
        private List<ActionBasedController> controllers = new List<ActionBasedController>();
        private InventorySlot inventorySlot;
        private ActionBasedController leftHand, rightHand;

        private enum InteractButton
        {
            trigger,
            grip
        };

        private void Start()
        {
            OnValidate();

            if (interactButton == InteractButton.grip)
            {
                leftHand.selectActionValue.reference.GetInputAction().performed += x => SetControllerGrip(leftHand, true);
                rightHand.selectActionValue.reference.GetInputAction().performed += x => SetControllerGrip(rightHand, true);
                leftHand.selectActionValue.reference.GetInputAction().canceled += x => SetControllerGrip(leftHand, false);
                rightHand.selectActionValue.reference.GetInputAction().canceled += x => SetControllerGrip(rightHand, false);
            }

            else
            {
                leftHand.activateActionValue.reference.GetInputAction().performed += x => SetControllerGrip(leftHand, true);
                rightHand.activateActionValue.reference.GetInputAction().performed += x => SetControllerGrip(rightHand, true);
                leftHand.activateActionValue.reference.GetInputAction().canceled += x => SetControllerGrip(leftHand, false);
                rightHand.activateActionValue.reference.GetInputAction().canceled += x => SetControllerGrip(rightHand, false);
            }
        }

        private void OnValidate()
        {
            if (!inventoryManager)
                inventoryManager = GetComponentInParent<InventoryManager>();
            if (!inventorySlot)
                inventorySlot = GetComponent<InventorySlot>();
            if (!leftHand && inventoryManager)
                leftHand = inventoryManager.leftController;
            if (!rightHand && inventoryManager)
                rightHand = inventoryManager.rightController;
        }

        private void Update()
        {
            if (controllers.Count == 0) return;
            for (var index = 0; index < controllers.Count; index++) TryInteractIfGripping(controllers[index]);
        }

        private void TryInteractIfGripping(ActionBasedController controller)
        {
            if (isLeftGripped && controller == leftHand)
            {
                inventorySlot.TryInteractWithSlot(controller.GetComponentInChildren<XRDirectInteractor>());
                isLeftGripped = false;
            }
            else if (isRightGripped && controller == rightHand)
            {
                inventorySlot.TryInteractWithSlot(controller.GetComponentInChildren<XRDirectInteractor>());
                isRightGripped = false;
            }
        }

        private void SetControllerGrip(ActionBasedController controller, bool state)
        {
            if (controller == leftHand)
                isLeftGripped = state;
            else
                isRightGripped = state;

            TryInteractOnRelease(controller);
        }

        private void TryInteractOnRelease(ActionBasedController controller)
        {
            if (!addToSlotOnRelease) return;
            var hasSelection = controller.GetComponentInChildren<XRDirectInteractor>().hasSelection;
            if (hasSelection && controllers.Contains(controller))
                inventorySlot.TryInteractWithSlot(controller.GetComponentInChildren<XRDirectInteractor>());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out XRBaseInteractor interactor)) return;
            var controller = interactor.GetComponentInParent<ActionBasedController>();

            if (controller && !controllers.Contains(controller))
            {
                controllers.Add(controller);

                // isGripped is marked as false when a slot is interacted with to prevent repeated interaction.
                // In that event, this checks that again if the player moved the hand in and out of the slot
                var gripState = GetTriggerOrGripValue();
                
                if (controller == leftHand)
                    isLeftGripped = gripState;
                else
                    isRightGripped = gripState;
            }
            
            bool GetTriggerOrGripValue()
            {
                var gripState = false;

                if (interactButton == InteractButton.grip)
                    gripState = (int)controller.selectActionValue.action.ReadValue<float>() == 1;
                else
                    gripState = (int)controller.activateActionValue.action.ReadValue<float>() == 1;
                return gripState;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var controller = other.GetComponentInParent<ActionBasedController>();
            if (controller) controllers.Remove(controller);
        }
    }
}