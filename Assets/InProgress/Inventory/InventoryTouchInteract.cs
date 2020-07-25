using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class InventoryTouchInteract : MonoBehaviour
{
    
    
    //[Tooltip("Original Select Action Trigger type on controllers")] [SerializeField]
    private XRBaseControllerInteractor.SelectActionTriggerType OriginalActionTriggerType;

    //Forcing on Grab only works with 'State Change' Unity is working on fixing for their next release
    private XRBaseControllerInteractor.SelectActionTriggerType requiredSelectActionType = XRBaseControllerInteractor.SelectActionTriggerType.StateChange;

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
           // ReturnToOriginalActionTrigger(controller);
            inventorySlot.TryInteractWithSlot(controller);
        }
        else if (inventorySlot.CurrentSlotItem && controller)
        {
            //SetActionTrigger(controller);
            inventorySlot.TryInteractWithSlot(controller);
        }
    }
    private void SetActionTrigger(XRBaseControllerInteractor controller)
    {
        controller.selectActionTrigger = requiredSelectActionType;
    }

    private void ReturnToOriginalActionTrigger(XRBaseControllerInteractor controller)
    {
        controller.selectActionTrigger = OriginalActionTriggerType;
    }
}