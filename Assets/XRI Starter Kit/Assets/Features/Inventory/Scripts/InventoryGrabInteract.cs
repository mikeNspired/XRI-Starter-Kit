using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class InventoryGrabInteract : MonoBehaviour
    {
        [SerializeField] private InteractButton interactButton = InteractButton.grip;
        [SerializeField] private InventoryManager inventoryManager;

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
                leftHand.selectAction.reference.GetInputAction().performed += x => SetControllerGrip(leftHand, true);
                rightHand.selectAction.reference.GetInputAction().performed += x => SetControllerGrip(rightHand, true);
                leftHand.selectAction.reference.GetInputAction().canceled += x => SetControllerGrip(leftHand, false);
                rightHand.selectAction.reference.GetInputAction().canceled += x => SetControllerGrip(rightHand, false);
            }

            else
            {
                leftHand.activateAction.reference.GetInputAction().performed += x => SetControllerGrip(leftHand, true);
                rightHand.activateAction.reference.GetInputAction().performed += x => SetControllerGrip(rightHand, true);
                leftHand.activateAction.reference.GetInputAction().canceled += x => SetControllerGrip(leftHand, false);
                rightHand.activateAction.reference.GetInputAction().canceled += x => SetControllerGrip(rightHand, false);
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

        private void SetControllerGrip(ActionBasedController controller, bool state)
        {
            if (!controllers.Contains(controller)) return;
            if (!inventorySlot.gameObject.activeInHierarchy) return;
            inventorySlot.TryInteractWithSlot(controller.GetComponentInChildren<XRDirectInteractor>());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out XRBaseInteractor interactor)) return;
            var controller = interactor.GetComponentInParent<ActionBasedController>();

            if (controller && !controllers.Contains(controller)) controllers.Add(controller);
        }

        private void OnTriggerExit(Collider other)
        {
            var controller = other.GetComponentInParent<ActionBasedController>();
            if (controller) controllers.Remove(controller);
        }
    }
}