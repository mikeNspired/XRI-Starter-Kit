// Author MikeNspired. 

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

        public UnityEvent OnTriggerPressed;
        public UnityEvent OnTriggerRelease;
        public UnityEventFloat OnTriggerValue;
        public UnityEvent OnGripPressed;
        public UnityEvent OnGripRelease;
        public UnityEventFloat OnGripValue;

        public float gripValue;
        public float triggerValue;

        private InputDevice inputDevice;
        public bool IsGripped, IsTriggered;

        private void Start()
        {
            OnValidate();
            if (!actionBasedController)
                enabled = false;

            actionBasedController.selectAction.reference.GetInputAction().performed += x => Gripped(true);
            actionBasedController.selectAction.reference.GetInputAction().canceled += x => Gripped(false);
            actionBasedController.activateAction.reference.GetInputAction().performed += x => Triggered(true);
            actionBasedController.activateAction.reference.GetInputAction().canceled += x => Triggered(false);
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

        private void Triggered(bool state)
        {
            IsTriggered = state;
            if (state) OnTriggerPressed.Invoke();
            else OnTriggerRelease.Invoke();
        }

        private void Update()
        {
            triggerValue = actionBasedController.activateActionValue.action.ReadValue<float>();
            gripValue = actionBasedController.selectActionValue.action.ReadValue<float>();

            OnTriggerValue.Invoke(triggerValue);
            OnGripValue.Invoke(gripValue);
        }
    }
}
