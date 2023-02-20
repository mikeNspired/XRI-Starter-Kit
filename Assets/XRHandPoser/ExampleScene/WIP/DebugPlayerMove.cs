using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
#if (UNITY_EDITOR) 

namespace MikeNspired.UnityXRHandPoser
{
    public class DebugPlayerMove : MonoBehaviour
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private XRDeviceSimulator xrDeviceSimulator;
        [SerializeField] private ActionBasedController leftController, rightController;
        
        private void OnValidate()
        {
            if (!xrOrigin)
                xrOrigin = (XROrigin)FindObjectOfType(typeof(XROrigin));

            if (leftController || rightController) return;

            var controllers = (ActionBasedController[])FindObjectsOfType(typeof(ActionBasedController));
            foreach (var controller in controllers)
            {
                if (!leftController && controller.name.Contains("eft"))
                    leftController = controller;
                else if (!rightController && controller.name.Contains("ight"))
                    rightController = controller;
            }
        }

        public void EnableControllerTracking(bool state)
        {
            xrDeviceSimulator = (XRDeviceSimulator)FindObjectOfType(typeof(XRDeviceSimulator));

            if (!xrDeviceSimulator) return;
            xrDeviceSimulator.leftControllerIsTracked = state;
            xrDeviceSimulator.rightControllerIsTracked = state;

            xrDeviceSimulator.enabled = false;
            xrDeviceSimulator.enabled = true;
        }


        public void DisableControllers()
        {
            leftController.enabled = false;
            rightController.enabled = false;
        }

        public void Move(Transform selection)
        {
            FieldInfo fi = typeof(XRSimulatedControllerState).GetField("m_LeftControllerState", BindingFlags.NonPublic | BindingFlags.Instance);

            if (Application.isPlaying && selection.TryGetComponent(out ActionBasedController controller))
                controller.enableInputTracking = false;
            selection.position = transform.position;
        }

        public static void Select(GameObject selection)
        {
            Selection.activeGameObject = selection;
        }
    }
}

#endif