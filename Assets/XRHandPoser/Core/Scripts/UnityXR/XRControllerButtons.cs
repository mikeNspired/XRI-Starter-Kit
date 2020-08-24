// Copyright (c) MikeNspired. All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    /// <summary>
    /// Temporary class to get the trigger and grip input.
    /// A more robust solution will be released when unity integrates their new input system into UnityXR.
    /// </summary>
    public class XRControllerButtons : MonoBehaviour
    {
        [SerializeField] private XRController xrController;

        public UnityEventFloat OnTriggerValue;
        public UnityEvent OnGripPressed;
        public UnityEvent OnGripRelease;

        public bool gripValue;
        public float triggerValue;

        private InputDevice inputDevice;
        private bool IsGripped;

        private void Start()
        {
            OnValidate();
            if (xrController)
                inputDevice = xrController.inputDevice;
            else
                enabled = false;
        }

        private void OnValidate()
        {
            if (!xrController) xrController = GetComponentInParent<XRController>();
        }

        private void Update()
        {
            inputDevice = xrController.inputDevice;
            inputDevice.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);

            OnTriggerValue.Invoke(triggerValue);

            if (inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripValue))
            {
                if (!IsGripped && gripValue)
                {
                    IsGripped = true;
                    OnGripPressed.Invoke();
                }
                else if (IsGripped && !gripValue)
                {
                    IsGripped = false;
                    OnGripRelease.Invoke();
                }
            }
        }
    }


    [System.Serializable]
    public class UnityEventFloat : UnityEvent<float>
    {
    }
}