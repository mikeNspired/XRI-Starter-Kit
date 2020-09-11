using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class InventoryTouchInteract : MonoBehaviour
    {
        private InventorySlot inventorySlot;

        private void Start()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            if (!inventorySlot)
                inventorySlot = GetComponent<InventorySlot>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!enabled) return;

            var controller = other.GetComponent<XRBaseControllerInteractor>();

            if (!controller) return;

            if (!inventorySlot.CurrentSlotItem && controller?.GetComponent<XRDirectInteractor>()?.selectTarget)
            {
                inventorySlot.TryInteractWithSlot(controller);
            }
            else if (inventorySlot.CurrentSlotItem && controller)
            {
                inventorySlot.TryInteractWithSlot(controller);
            }
        }
    }
}