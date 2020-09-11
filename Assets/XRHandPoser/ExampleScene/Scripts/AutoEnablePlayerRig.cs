using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace MikeNspired.UnityXRHandPoser
{
    public class AutoEnablePlayerRig : MonoBehaviour
    {
        [SerializeField] private GameObject viveRig;
        [SerializeField] private GameObject oculusRig;
        [SerializeField] private GameObject windowsRig;

        private void Awake()
        {
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

            yield return new WaitForEndOfFrame();

            SetAllCanvasesToRig();
        }

        private void SetRigActive(GameObject rig)
        {
            viveRig.SetActive(false);
            oculusRig.SetActive(false);
            windowsRig.SetActive(false);

            rig.SetActive(true);
        }

        private void SetAllCanvasesToRig()
        {
            var canvases = FindObjectsOfType<Canvas>();
            // foreach (var canvas in canvases)
            // {
            //     canvas.worldCamera = Camera.main;
            // }
        }
    }
}