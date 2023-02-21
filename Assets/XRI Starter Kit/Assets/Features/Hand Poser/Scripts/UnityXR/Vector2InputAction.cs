using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static Unity.Mathematics.math;

namespace MikeNspired.UnityXRHandPoser
{
    public class Vector2InputAction : MonoBehaviour
    {
        [SerializeField] private InputActionReference inputAction;
        [SerializeField] private bool inverseX, inverseY;
        public Vector2 value;

        public UnityEvent OnActivate, OnCancel;
        public UnityEventFloat XAxis, YAxis;
        public UnityEventVector2 Axis;

        private bool isActive;

        private void Start()
        {
            if (!inputAction || inputAction.action == null)
            {
                Debug.LogWarning("Missing InputActionReference on gameObject: " + gameObject);
                enabled = false;
                return;
            }

            inputAction.action.Enable();
            inputAction.action.performed += x => Activate();
            inputAction.action.canceled += x => Cancel();
        }

        private void Activate()
        {
            isActive = true;
            OnActivate.Invoke();
        }

        private void Cancel()
        {
            isActive = false;
            OnCancel.Invoke();

            value = Vector2.zero;

            InvokeValueEvents();
        }

        private void Update()
        {
            if (!isActive) return;

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