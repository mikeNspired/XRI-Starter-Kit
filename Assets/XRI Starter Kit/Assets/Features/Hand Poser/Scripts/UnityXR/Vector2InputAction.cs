using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static Unity.Mathematics.math;

namespace MikeNspired.XRIStarterKit
{
    public class Vector2InputAction : MonoBehaviour
    {
        [SerializeField] private InputActionReference inputAction;
        [SerializeField] private bool inverseX, inverseY;
        public Vector2 value;

        public UnityEvent OnActivate, OnCancel;
        public UnityEventFloat XAxis; // expects a float parameter
        public UnityEventFloat YAxis; // expects a float parameter
        public UnityEventVector2 Axis; // expects a Vector2 parameter

        private bool isActive;

        private void OnEnable()
        {
            if (inputAction == null || inputAction.action == null)
            {
                Debug.LogWarning("Missing InputActionReference on gameObject: " + gameObject);
                enabled = false;
                return;
            }

            // Enable the action and subscribe to its events.
            inputAction.action.Enable();
            inputAction.action.performed += OnActionPerformed;
            inputAction.action.canceled += OnActionCanceled;
        }

        private void OnDisable()
        {
            if (inputAction != null && inputAction.action != null)
            {
                inputAction.action.performed -= OnActionPerformed;
                inputAction.action.canceled -= OnActionCanceled;
                inputAction.action.Disable();
            }
        }

        // Called when the input action is performed.
        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            // Depending on your desired behavior, you might choose to set isActive here.
            // For continuous input, you may not need to gate reading the value.
            isActive = true;
            OnActivate.Invoke();
        }

        // Called when the input action is canceled.
        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            isActive = false;
            OnCancel.Invoke();
            value = Vector2.zero;
            InvokeValueEvents();
        }

        private void Update()
        {
            if (!isActive)
            {
                value = Vector2.zero;
                return;
            }

            // Read the current value from the input action.
            value = inputAction.action.ReadValue<Vector2>();

            if (inverseX)
                value.x = remap(-1, 1, 1, -1, value.x);
            if (inverseY)
                value.y = remap(-1, 1, 1, -1, value.y);

            InvokeValueEvents();
        }

        private void InvokeValueEvents()
        {
            XAxis.Invoke(value.x);
            YAxis.Invoke(value.y);
            Axis.Invoke(value);
        }
    }
}
