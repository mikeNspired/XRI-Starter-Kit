using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Updated class to get trigger and grip input using the new ControllerInputActionManager.
    /// </summary>
    public class XRControllerButtons : MonoBehaviour
    {
        [SerializeField] private InputActionReference triggerAction;
        [SerializeField] private InputActionReference gripAction;

        public UnityEvent OnTriggerPressed;
        public UnityEvent OnTriggerReleased;
        public UnityEvent<float> OnTriggerValue;
        public UnityEvent OnGripPressed;
        public UnityEvent OnGripReleased;
        public UnityEvent<float> OnGripValue;

        public float gripValue { get; private set; }
        public float triggerValue { get; private set; }

        public bool IsGripped { get; private set; }
        public bool IsTriggered { get; private set; }

        // Stored so OnDestroy can unsubscribe the SAME delegate instances that Awake subscribed.
        // Inline lambdas in -= create new instances and silently remove nothing, leaving the
        // shared InputAction invoking handlers on a destroyed component after scene changes.
        private System.Action<InputAction.CallbackContext> triggerPerformed, triggerCanceled;
        private System.Action<InputAction.CallbackContext> gripPerformed, gripCanceled;

        private void Awake()
        {
            ValidateInputs();

            triggerPerformed = ctx => Triggered(true);
            triggerCanceled  = ctx => Triggered(false);
            gripPerformed    = ctx => Gripped(true);
            gripCanceled     = ctx => Gripped(false);

            // Subscribe to trigger actions
            if (triggerAction?.action != null)
            {
                triggerAction.action.performed += triggerPerformed;
                triggerAction.action.canceled += triggerCanceled;
            }

            // Subscribe to grip actions
            if (gripAction?.action != null)
            {
                gripAction.action.performed += gripPerformed;
                gripAction.action.canceled += gripCanceled;
            }
        }

        private void OnEnable()
        {
            // Enable input actions
            triggerAction?.action?.Enable();
            gripAction?.action?.Enable();
        }

        private void OnDisable()
        {
            // Disable input actions
            triggerAction?.action?.Disable();
            gripAction?.action?.Disable();
        }

        private void OnDestroy()
        {
            // Unsubscribe from trigger actions
            if (triggerAction?.action != null)
            {
                triggerAction.action.performed -= triggerPerformed;
                triggerAction.action.canceled -= triggerCanceled;
            }

            // Unsubscribe from grip actions
            if (gripAction?.action != null)
            {
                gripAction.action.performed -= gripPerformed;
                gripAction.action.canceled -= gripCanceled;
            }
        }

        private void Update()
        {
            if (triggerAction?.action != null)
            {
                triggerValue = triggerAction.action.ReadValue<float>();
                OnTriggerValue.Invoke(triggerValue);
            }

            if (gripAction?.action != null)
            {
                gripValue = gripAction.action.ReadValue<float>();
                OnGripValue.Invoke(gripValue);
            }
        }

        private void Triggered(bool state)
        {
            IsTriggered = state;
            if (state)
                OnTriggerPressed.Invoke();
            else
                OnTriggerReleased.Invoke();
        }

        private void Gripped(bool state)
        {
            IsGripped = state;
            if (state)
                OnGripPressed.Invoke();
            else
                OnGripReleased.Invoke();
        }

        private void ValidateInputs()
        {
            if (triggerAction == null)
                Debug.LogError("Trigger Action is not assigned.", this);

            if (gripAction == null)
                Debug.LogError("Grip Action is not assigned.", this);
        }
    }
}
