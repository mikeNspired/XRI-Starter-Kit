using System;
using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace MyNamespace
{
    public class InventoryGrabInteract : MonoBehaviour
    {
        [SerializeField] private InteractButton interactButton = InteractButton.grip;

        private bool leftIsGripped, rightIsGripped;
        private List<XRController> controllers = new List<XRController>();
        private InventorySlot inventorySlot;


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
            if (!inventorySlot)
                inventorySlot = GetComponent<InventorySlot>();
        }

        private InputDevice inputDevice;


        private void Update()
        {
            if (controllers.Count == 0) return;

            foreach (var controller in controllers)
            {
                CheckController(controller);
            }
        }

        private void CheckController(XRController controller)
        {
            if (interactButton == InteractButton.trigger)
                CheckControllerTrigger(controller);
            else
            {
                if (controller.controllerNode == XRNode.LeftHand)
                    CheckControllerGrip(controller, ref leftIsGripped);
                else
                    CheckControllerGrip(controller, ref rightIsGripped);
            }
        }

        private bool isGrippedCheckerBitches = false;
        private void CheckControllerGrip(XRController controller, ref bool isGripped)
        {
            inputDevice = controller.inputDevice;
            if (!inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool gripValue)) return;

           // Debug.Log("isGripped: " + isGripped + " " + " gripValue: " + gripValue + " isGrippedCheckerBitches: " + isGrippedCheckerBitches);
            if (!isGripped && gripValue)// && !isGrippedCheckerBitches)
            {
                Debug.Log("grabbing item");
                isGripped = true;
                if (!IsControllerHoldingObject(controller))
                    inventorySlot.TryInteractWithSlot(controller.GetComponent<XRBaseInteractor>());
            }
            else if (isGripped && !gripValue)
            {
                Debug.Log("releasing item");

                isGripped = false;
                if (IsControllerHoldingObject(controller))
                    inventorySlot.TryInteractWithSlot(controller.GetComponent<XRBaseInteractor>());
            }

            // if(!gripValue)
            // {
            //     isGrippedCheckerBitches = false;
            // }
        }

        private bool IsControllerHoldingObject(XRController controller)
        {
            return controller.GetComponent<XRDirectInteractor>().selectTarget;
        }

        private void CheckControllerTrigger(XRController controller)
        {
            inputDevice = controller.inputDevice;
            if (!inputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool gripValue)) return;

            if (gripValue)
            {
                if (!controller.GetComponent<XRDirectInteractor>().selectTarget)
                    inventorySlot.TryInteractWithSlot(controller.GetComponent<XRBaseInteractor>());
            }
        }
    }
}