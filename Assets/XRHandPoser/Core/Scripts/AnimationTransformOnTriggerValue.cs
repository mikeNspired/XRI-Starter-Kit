// Author MikeNspired. 
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    /// <summary>
    /// Matches a Transform to another transform by the trigger value from 0 to 1.
    /// If the value is 0 the 'MovingObject' will be at the original starting position/rotation. 
    /// If the value is 1 the 'MovingObject' will be at the endPosition position/rotation.
    /// This class moves objects based on localPosition so make sure they have the same Parent.
    /// This class will be updated when the new input system is released.
    /// </summary>
    public class AnimationTransformOnTriggerValue : MonoBehaviour
    {
        [Tooltip("The Transform that you want to be animated based on trigger value")] [SerializeField]
        private Transform MovingObject = null;

        [Tooltip("The Transform ('Typically an empty gameObject') that you want the 'MovingObject' to be animated to")] [SerializeField]
        private Transform endPosition = null;

        [SerializeField] private XRGrabInteractable interactable = null;
        private TransformStruct startingPosition;
        private InputDevice inputDevice;
        private XRControllerButtons buttons;


        private void Start()
        {
            startingPosition.position = MovingObject.localPosition;
            startingPosition.rotation = MovingObject.localRotation;

            if (!interactable) interactable = GetComponent<XRGrabInteractable>();
            interactable.onSelectEntered.AddListener(SetController);
            interactable.onSelectExited.AddListener(RemoveController);
        }

        private void OnValidate()
        {
            if (!interactable) interactable = GetComponent<XRGrabInteractable>();
        }

        private void RemoveController(XRBaseInteractor controller) => buttons = null;

        private void SetController(XRBaseInteractor controller)
        {
            buttons = controller.GetComponentInParent<HandReference>().Hand.GetComponent<XRControllerButtons>();
        }


        private void Update()
        {
            //If not controller is being grabbed, will stop this update loop
            if (!buttons) return;
            float value = buttons.triggerValue;
            var newPosition = Vector3.Lerp(startingPosition.position, endPosition.localPosition, value);
            var newRotation = Quaternion.Lerp(startingPosition.rotation, endPosition.localRotation, value);

            MovingObject.localPosition = newPosition;
            MovingObject.localRotation = newRotation;
        }
    }
}