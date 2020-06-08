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
    public class XRControllerTriggerValueEvent : MonoBehaviour
    {
        public InputDevice inputDevice;
        public XRController xrController;

        public UnityEventFloat OnTriggerValue;
        public UnityEvent OnGripPressed;
        public UnityEvent OnGripRelease;

        public bool gripValue;
        public float triggerValue;

        private bool IsGripped;

        void Start()
        {
            xrController = GetComponentInParent<XRController>();

            if (xrController)
                inputDevice = xrController.inputDevice;
            else
                enabled = false;

        }

        void Update()
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

        public void Grab()
        {
            OnGripPressed.Invoke();

        }

        public void Release()
        {
            OnGripRelease.Invoke();

        }
    }



    [System.Serializable]
    public class UnityEventFloat : UnityEvent<float>
    {
    }
}