using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ClimbGrabPoint : MonoBehaviour
{
    private XRGrabInteractable xrGrabInteractable;
    private PlayerClimbing playerClimbing;
    private XRController xRController;
    private XRDirectInteractor xRDirectInteractor;
    private new Camera camera;
    public float hapticDuration = .1f;
    public float hapticStrength = .5f;
    [SerializeField]
    private VelocityTracker velocityTracker;
    private void Start()
    {
        playerClimbing = FindObjectOfType<PlayerClimbing>();
        xrGrabInteractable = GetComponent<XRGrabInteractable>();
        xrGrabInteractable.onSelectEnter.AddListener(OnSelect);
        xrGrabInteractable.onSelectExit.AddListener(OnSelectExit);
        camera = Camera.main;
    }

    private void OnValidate()
    {
        if (velocityTracker == null)
            velocityTracker = GetComponent<VelocityTracker>();
    }
    
    private void OnSelect(XRBaseInteractor interactor)
    {
        xRController = interactor.GetComponent<XRController>();
        xRDirectInteractor = interactor.GetComponent<XRDirectInteractor>();

        if (xRDirectInteractor)
            SetClimberHand(xRController);

        if (velocityTracker)
        {
            //controllerVelocity.SetController(interactor.transform);
            velocityTracker.SetController(interactor.GetComponentInParent<XRRig>().transform);
            velocityTracker.StartTracking();
        }
        
        interactor.GetComponent<XRController>().SendHapticImpulse(hapticStrength, hapticDuration);
    }

    private void SetClimberHand(XRController controller)
    {
        playerClimbing.SetClimbHand(controller);
    }

    private void OnSelectExit(XRBaseInteractor interactor)
    {
        xRController = interactor.GetComponent<XRController>();
        xRDirectInteractor = interactor.GetComponent<XRDirectInteractor>();
        
        if (xRDirectInteractor)
        {
            playerClimbing.RemoveClimbHand(xRController);
            if (velocityTracker)
            {
                playerClimbing.SetReleasedVelocity(velocityTracker.CurrentSmoothedVelocity);
                velocityTracker.StopTracking();
            }
        }
    }
}