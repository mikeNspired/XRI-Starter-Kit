using UnityEngine.InputSystem;

namespace MikeNspired.XRIStarterKit
{
    public static class InputActionReferenceExtensions
    {
        public static void EnableAction(this InputActionReference actionReference)
        {
            var action = GetInputAction(actionReference);
            if (action != null && !action.enabled)
                action.Enable();
        }

        public static void DisableAction(this InputActionReference actionReference)
        {
            var action = GetInputAction(actionReference);
            if (action != null && action.enabled)
                action.Disable();
        }

        public static InputAction GetInputAction(this InputActionReference actionReference)
        {
#pragma warning disable IDE0031 // Use null propagation -- Do not use for UnityEngine.Object types
            return actionReference != null ? actionReference.action : null;
#pragma warning restore IDE0031
        }
    }
}