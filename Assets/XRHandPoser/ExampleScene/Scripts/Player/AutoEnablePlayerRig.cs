using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class AutoEnablePlayerRig : MonoBehaviour
    {
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private GameObject viveRig = null;
        [SerializeField] private GameObject oculusRig = null;
        [SerializeField] private GameObject windowsRig = null;
        [SerializeField] private InventoryManager inventoryManager = null;
        [SerializeField] private AmmoBackPack ammoBackPack = null;

        private void Awake()
        {
            if (isEnabled)
                StartCoroutine(EnableCorrectRig());
        }

        private IEnumerator EnableCorrectRig()
        {
            var hmdList = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdList);

            while (hmdList.Count == 0)
            {
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdList);
                yield return new WaitForEndOfFrame();
            }

            var headSetName = hmdList[0].name.ToLower();

            if (headSetName.Contains("windows") || headSetName.Contains("wmr"))
                SetRigActive(windowsRig);
            else if (headSetName.Contains("vive") || headSetName.Contains("openvr"))
                SetRigActive(viveRig);
            else
                SetRigActive(oculusRig);
        }

        private void SetRigActive(GameObject rig)
        {
            viveRig.SetActive(false);
            oculusRig.SetActive(false);
            windowsRig.SetActive(false);

            rig.SetActive(true);

            SetVariables(rig);
        }

        private void SetVariables(GameObject rig)
        {
            var directInteractors = GetComponentsInChildren<XRDirectInteractor>();
            foreach (var interactor in directInteractors)
            {
                var controller = interactor.GetComponent<XRController>();
                if (controller.controllerNode == XRNode.LeftHand)
                {
                    if (inventoryManager)
                        inventoryManager.leftController = controller;
                    if (ammoBackPack)
                        ammoBackPack.leftHand = interactor;
                }
                else
                {
                    if (inventoryManager)
                        inventoryManager.rightController = controller;
                    if (ammoBackPack)
                        ammoBackPack.rightHand = interactor;
                }
            }

            ammoBackPack.ClearControllers();
        }
    }
}