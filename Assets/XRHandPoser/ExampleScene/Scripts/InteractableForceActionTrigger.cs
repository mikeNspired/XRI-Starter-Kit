using System;
using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class InteractableForceActionTrigger : MonoBehaviour
{
    private XRGrabInteractable interactable;

    public XRBaseControllerInteractor.SelectActionTriggerType ActionTriggerType;
    private XRBaseControllerInteractor.SelectActionTriggerType originalActionTriggerType;
    // Start is called before the first frame update
    void Start()
    {
        OnValidate();
        interactable.onSelectEnter.AddListener(x => SetAttachForInstantaneous(x.GetComponent<XRBaseControllerInteractor>()));
        interactable.onSelectExit.AddListener(x => ReturnAttachForInstantaneous(x.GetComponent<XRBaseControllerInteractor>()));
    }

    private void SetAttachForInstantaneous(XRBaseControllerInteractor controller)
    {
        Debug.Log(controller.gameObject.name + "Set Attach to : " + ActionTriggerType);
        originalActionTriggerType = controller.selectActionTrigger;
        controller.selectActionTrigger = ActionTriggerType;
        
    }
    private void ReturnAttachForInstantaneous(XRBaseControllerInteractor controller)
    {
        Debug.Log(controller.gameObject.name + "Return Attach to : " + originalActionTriggerType);
        controller.selectActionTrigger = originalActionTriggerType;
    }
    

    private void OnValidate()
    {
        interactable = GetComponent<XRGrabInteractable>();
    }
}
