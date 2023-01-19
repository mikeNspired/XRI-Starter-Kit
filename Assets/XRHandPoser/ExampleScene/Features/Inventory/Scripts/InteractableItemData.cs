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

        public bool parentToPlayerOnGrabForSmoothMovement = true;

        private XRBaseInteractable interactable;

        private void Start()
        {
            if (!parentToPlayerOnGrabForSmoothMovement) return;
            interactable = GetComponent<XRBaseInteractable>();
            if (TryGetComponent(out XRGrabInteractable grabInteractable))
            {
                grabInteractable.onSelectEntered.AddListener(x => Invoke(nameof(SetParentToPlayerForSmoothMovement), grabInteractable.attachEaseInTime + Time.deltaTime));
            }
            else
                interactable.onSelectEntered.AddListener(x => Invoke(nameof(SetParentToPlayerForSmoothMovement), Time.deltaTime));
        }

        private void SetParentToPlayerForSmoothMovement()
        {
            if (interactable.selectingInteractor)
                transform.parent = interactable.selectingInteractor.transform.parent;
        }

        #endregion
    }
}