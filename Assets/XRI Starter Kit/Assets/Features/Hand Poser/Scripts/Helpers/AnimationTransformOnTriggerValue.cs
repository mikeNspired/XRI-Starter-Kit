// Author MikeNspired.
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Matches a Transform to another transform by the trigger value from 0 to 1.
    /// If the value is 0 the 'MovingObject' will be at the original starting position/rotation. 
    /// If the value is 1 the 'MovingObject' will be at the endPosition position/rotation.
    /// This class moves objects based on localPosition so make sure they have the same Parent.
    /// </summary>
    public class AnimationTransformOnTriggerValue : MonoBehaviour
    {
        [Tooltip("The Transform that you want to be animated based on trigger value")]
        [SerializeField] private Transform MovingObject;

        [Tooltip("The Transform ('Typically an empty gameObject') that you want the 'MovingObject' to be animated to")]
        [SerializeField] private Transform endPosition;

        [SerializeField] private XRGrabInteractable interactable;

        private TransformStruct startingPosition;
        private XRControllerButtons buttons;

        private void Start()
        {
            startingPosition.position = MovingObject.localPosition;
            startingPosition.rotation = MovingObject.localRotation;

            if (!interactable) interactable = GetComponent<XRGrabInteractable>();

            interactable.selectEntered.AddListener(args => SetController(args));
            interactable.selectExited.AddListener(args => buttons = null);
        }

        private void OnValidate()
        {
            if (!interactable) interactable = GetComponent<XRGrabInteractable>();
        }

        private void SetController(SelectEnterEventArgs args)
        {
            if (args.interactorObject is { } interactor)
                buttons = interactor.transform.GetComponentInParent<HandReference>()?.Hand.GetComponent<XRControllerButtons>();
        }

        private void Update()
        {
            if (buttons == null) return;

            float value = buttons.triggerValue;
            MovingObject.localPosition = Vector3.Lerp(startingPosition.position, endPosition.localPosition, value);
            MovingObject.localRotation = Quaternion.Lerp(startingPosition.rotation, endPosition.localRotation, value);
        }
    }
}
