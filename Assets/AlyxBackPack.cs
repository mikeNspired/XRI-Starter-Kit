using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class AlyxBackPack : MonoBehaviour
{
    [SerializeField] private XRDirectInteractor leftHand, rightHand;

    [SerializeField] private InteractButton interactButton = InteractButton.grip;
    [SerializeField] private XRGrabInteractable magazine;
    [SerializeField] private XRGrabInteractable magazine2;
    [SerializeField] private GunType gunType1;
    [SerializeField] private GunType gunType2;
    [SerializeField] private float itemGrabTimeout = .5f; 
    private float itemGrabTimeoutTimer;

    private bool leftIsGripped, rightIsGripped;
    private List<XRController> controllers = new List<XRController>();
    private XRInteractionManager interactionManager;


    private enum InteractButton
    {
        trigger,
        grip
    };

    private void OnTriggerEnter(Collider other)
    {
        var controller = other.GetComponent<XRController>();
        if (controller && !controllers.Contains(controller))
        {
            controllers.Add(controller);
            if (controller.controllerNode == XRNode.LeftHand)
                leftIsGripped = true;
            else
                rightIsGripped = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var controller = other.GetComponent<XRController>();
        if (controller)
            controllers.Remove(controller);
    }

    private void Start()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (!interactionManager)
            interactionManager = FindObjectOfType<XRInteractionManager>();
    }

    private InputDevice inputDevice;


    private void Update()
    {
        itemGrabTimeoutTimer += Time.deltaTime;
        
        if (controllers.Count == 0) return;

        if (itemGrabTimeoutTimer <= itemGrabTimeout) return;
        foreach (var controller in controllers)
        {
            CheckController(controller);
        }
    }

    private void CheckController(XRController controller)
    {
        if (interactButton == InteractButton.trigger)
        {
            
        }
        else
        {
            if (controller.controllerNode == XRNode.LeftHand)
                CheckControllerGrip(controller, ref leftIsGripped);
            else
                CheckControllerGrip(controller, ref rightIsGripped);
        }
    }

    private void CheckControllerGrip(XRController controller, ref bool isGripped)
    {
        inputDevice = controller.inputDevice;
        if (!inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool gripValue)) return;

        if (!isGripped && gripValue) // && !isGrippedCheckerBitches)
        {
            isGripped = true;
            if (!IsControllerHoldingObject(controller))
                TryGrabAmmo(controller.GetComponent<XRBaseInteractor>());
        }
        else if (isGripped && !gripValue)
        {
            isGripped = false;
        }
    }

    private bool IsControllerHoldingObject(XRController controller)
    {
        return controller.GetComponent<XRDirectInteractor>().selectTarget;
    }

    private void CheckControllerTrigger(XRController controller)
    {
    }

    private void TryGrabAmmo(XRBaseInteractor interactor)
    {
        XRBaseInteractor currentInteractor;
        XRBaseInteractor handHoldingWeapon;
        if (interactor == leftHand)
        {
            handHoldingWeapon = rightHand;
            currentInteractor = interactor;
        }
        else
        {
            handHoldingWeapon = leftHand;
            currentInteractor = interactor;
        }

        //Check if hand not interacting with pack is holding weapon
        if (!handHoldingWeapon.selectTarget) return;
        if (currentInteractor.selectTarget) return;


        var gunType = handHoldingWeapon.selectTarget.GetComponentInChildren<MagazineAttachPoint>()?.GunType;
        if (!gunType) return;

        XRGrabInteractable newMagazine;
        if (gunType1 == gunType)
            newMagazine = Instantiate(magazine);
        else if (gunType2 == gunType)
            newMagazine = Instantiate(magazine2);
        else newMagazine = Instantiate(magazine2);

        newMagazine.transform.position = currentInteractor.transform.position;
        newMagazine.transform.forward = currentInteractor.transform.forward;

        StartCoroutine(GrabItem(currentInteractor, newMagazine));
    }

    IEnumerator GrabItem(XRBaseInteractor currentInteractor, XRGrabInteractable newMagazine)
    {
        yield return new WaitForFixedUpdate();
        if (currentInteractor.selectTarget) yield break;
        interactionManager.SelectEnter_public(currentInteractor, newMagazine);
        itemGrabTimeoutTimer = 0;
    }
}