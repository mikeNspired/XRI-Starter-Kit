//Author Mikenspired

using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class InteractableItemData : MonoBehaviour
    {
        public bool canInventory = true;

        public bool canDistanceGrab = true;

        // public int maxInventoryStackSize = 0;

        #region TemporaryFixForSmoothMovementJerkyness

        [Tooltip("Only needed for velocity and isKinematic movement")]
        public bool parentToPlayerOnGrabForSmoothMovement = true;

        private XRGrabInteractable interactable;

        private void Start()
        {
            if (!parentToPlayerOnGrabForSmoothMovement) return;
            interactable = GetComponent<XRGrabInteractable>();
            if (!interactable || interactable.movementType == XRBaseInteractable.MovementType.Instantaneous) return;
            interactable.onSelectEntered.AddListener(x => Invoke(nameof(SetParentToPlayerForSmoothMovement), interactable.attachEaseInTime + Time.deltaTime));
        }

        private void SetParentToPlayerForSmoothMovement()
        {
            if (interactable.selectingInteractor)
                transform.parent = interactable.selectingInteractor.transform.parent;
        }

        #endregion
    }
}