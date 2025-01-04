using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace MikeNspired.UnityXRHandPoser
{
    public class InventoryGrabInteract : MonoBehaviour
    {
        [SerializeField] private InputActionReference leftControllerInput, rightControllerInput;
        [SerializeField] private InventoryManager inventoryManager; 

        private List<ControllerInputActionManager> hoveringControllers = new List<ControllerInputActionManager>();
        private InventorySlot inventorySlot;

        private void Start()
        {
            OnValidate();

            if (leftControllerInput?.action != null)
            {
                leftControllerInput.action.performed += _ => CheckControllerInteraction(inventoryManager.leftController);
                leftControllerInput.action.canceled += _ => CheckControllerInteraction(inventoryManager.leftController);
            }

            if (rightControllerInput?.action != null)
            {
                rightControllerInput.action.performed += _ => CheckControllerInteraction(inventoryManager.rightController);
                rightControllerInput.action.canceled += _ => CheckControllerInteraction(inventoryManager.rightController);
            }
        }

        private void OnValidate()
        {
            if (!inventoryManager)
                inventoryManager = GetComponentInParent<InventoryManager>();

            if (!inventorySlot)
                inventorySlot = GetComponent<InventorySlot>();
        }

        private void CheckControllerInteraction(ControllerInputActionManager controller)
        {
            if (!hoveringControllers.Contains(controller) || !inventorySlot.gameObject.activeInHierarchy) return;
            
            var interactor = controller.GetComponentInChildren<XRDirectInteractor>();
            if (interactor != null)
                inventorySlot.TryInteractWithSlot(interactor);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out XRBaseInteractor interactor)) return;

            var controller = interactor.GetComponentInParent<ControllerInputActionManager>();
            if (controller != null && !hoveringControllers.Contains(controller))
                hoveringControllers.Add(controller);
        }

        private void OnTriggerExit(Collider other)
        {
            var controller = other.GetComponentInParent<ControllerInputActionManager>();
            if (controller != null)
                hoveringControllers.Remove(controller);
        }
    }
}
