// Copyright (c) MikeNspired. All Rights Reserved.

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
        [SerializeField] private ActionBasedController actionBasedController;

        public UnityEventFloat OnTriggerValue;
        public UnityEvent OnGripPressed;
        public UnityEvent OnGripRelease;

        public float gripValue;
        public float triggerValue;

        private InputDevice inputDevice;
        public bool IsGripped;

        private void Start()
        {
            OnValidate();
            if (!actionBasedController)
                enabled = false;

            actionBasedController.selectActionValue.reference.GetInputAction().performed += x => Gripped(true);
            actionBasedController.selectActionValue.reference.GetInputAction().canceled += x => Gripped(false);
        }

        private void OnValidate()
        {
            if (!actionBasedController) actionBasedController = GetComponentInParent<ActionBasedController>();
        }

        private void Gripped(bool state)
        {
            IsGripped = state;
            if (state) OnGripPressed.Invoke();
            else OnGripRelease.Invoke();
        }

        private void Update()
        {
            triggerValue = actionBasedController.activateAction.action.ReadValue<float>();
            gripValue = actionBasedController.selectActionValue.action.ReadValue<float>();

            OnTriggerValue.Invoke(triggerValue);
        }
    }


    [System.Serializable]
    public class UnityEventFloat : UnityEvent<float>
    {
    }
}