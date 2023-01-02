using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ClimbGrabPoint : MonoBehaviour
{
    private XRGrabInteractable xrGrabInteractable;
    private PlayerClimbingXR playerClimbingXr;
    private XRBaseController xRController;
    private XRDirectInteractor xRDirectInteractor;
    private new Camera camera;
    public float hapticDuration = .1f;
    public float hapticStrength = .5f;
    [SerializeField]
    private VelocityTracker velocityTracker;
    private void Start()
    {
        playerClimbingXr = FindObjectOfType<PlayerClimbingXR>();
        xrGrabInteractable = GetComponent<XRGrabInteractable>();
        xrGrabInteractable.onSelectEntered.AddListener(OnSelect);
        xrGrabInteractable.onSelectExited.AddListener(OnSelectExit);
        camera = Camera.main;
    }

    private void OnValidate()
    {
        if (velocityTracker == null)
            velocityTracker = GetComponent<VelocityTracker>();
    }
    
    private void OnSelect(XRBaseInteractor interactor)
    {
        xRController = interactor.GetComponent<XRBaseController>();
        xRDirectInteractor = interactor.GetComponent<XRDirectInteractor>();

        Debug.Log(xRController);
        Debug.Log(xRDirectInteractor);
        if (xRDirectInteractor)
            SetClimberHand(xRController);

        if (velocityTracker)
        {
            //controllerVelocity.SetController(interactor.transform);
            velocityTracker.SetTrackedObject(interactor.GetComponentInParent<XROrigin>().transform);
            velocityTracker.StartTracking();
        }
        
        interactor.GetComponent<XRBaseController>().SendHapticImpulse(hapticStrength, hapticDuration);
    }

    private void SetClimberHand(XRBaseController controller)
    {
        playerClimbingXr.SetClimbHand(controller);
    }

    private void OnSelectExit(XRBaseInteractor interactor)
    {
        xRController = interactor.GetComponent<XRBaseController>();
        xRDirectInteractor = interactor.GetComponent<XRDirectInteractor>();
        
        if (xRDirectInteractor)
        {
            playerClimbingXr.RemoveClimbHand(xRController);
            if (velocityTracker)
            {
                playerClimbingXr.SetReleasedVelocity(velocityTracker.CurrentSmoothedVelocity);
                velocityTracker.StopTracking();
            }
        }
    }
}