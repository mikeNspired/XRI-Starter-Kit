using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Test : MonoBehaviour
{
    public XRGrabInteractable xrGrabInteractable;
    public Wheel wheel;
    public Test interactableChanger;

    private bool isBeingGrabbed = false;

    private void Start()
    {
        xrGrabInteractable.onSelectEntered.AddListener(OnGrab);
        xrGrabInteractable.onSelectExited.AddListener(OnRelease);
    }

    private void OnRelease(XRBaseInteractor interactor)
    {
        if (interactableChanger.isBeingGrabbed) return;
        isBeingGrabbed = false;

        wheel.enabled = true;
        
    }

    private void OnGrab(XRBaseInteractor interactor)
    {
        if (interactableChanger.isBeingGrabbed) return;

        isBeingGrabbed = true;
        wheel.enabled = false;
    }
}