using UnityEngine;
using UnityEngine.InputSystem;

namespace MikeNspired.XRIStarterKit
{
    public class EnableScriptOnButton : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour behaviour;
        [SerializeField] private InputActionReference inputAction;
        [SerializeField] private bool inverse;

        private System.Action<InputAction.CallbackContext> onPerformed;
        private System.Action<InputAction.CallbackContext> onCanceled;
        private bool isInitialized;

        private void Awake() => InitializeDelegates();

        private void OnEnable()
        {
            InitializeDelegates(); 

            if (inputAction?.action == null) return;
            
            inputAction.action.performed += onPerformed;
            inputAction.action.canceled += onCanceled;
            inputAction.action.Enable();
        }

        private void OnDisable()
        {
            if (inputAction?.action == null) return;
            
            inputAction.action.performed -= onPerformed;
            inputAction.action.canceled -= onCanceled;
            inputAction.action.Disable();
        }

        private void InitializeDelegates()
        {
            if (isInitialized) return;

            onPerformed = ctx => Activate(!inverse);
            onCanceled = ctx => Activate(inverse);
            isInitialized = true;
        }

        private void Activate(bool state)
        {
            if (behaviour)
                behaviour.enabled = state;
        }
    }
}