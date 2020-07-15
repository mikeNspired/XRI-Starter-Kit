// Copyright (c) MikeNspired. All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class ColliderSwitchOnGrab : MonoBehaviour
    {
        [SerializeField] private XRGrabInteractable grabInteractable;

        [Tooltip("The default colliders to grab the weapon when the item is not being held")] [SerializeField]
        private Transform defaultColliders = null;

        [Tooltip("Used to allow collisions but disable accidentally grabbing item when trying to grab other grabbable parts of the item." +
                 "NOTE: Must be disabled before starting game or it will be registered with XRManager")]
        [SerializeField]
        private Transform onGrabColliders = null;

        [Tooltip("Optional: Expands the grab point near hand to a larger collider to. NOTE: Must be enabled at start")] [SerializeField]
        private Transform handGrabCollider = null;

        private bool PreventDisableOfCollidersForObjectDisable;

        private void Awake()
        {
            OnValidate();

            handGrabCollider.gameObject.SetActive(false);

            grabInteractable.onSelectEnter.AddListener(x => OnGrab());
            grabInteractable.onSelectExit.AddListener(x => OnRelease());
        }

        private void OnValidate()
        {
            if (!grabInteractable)
                grabInteractable = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            handGrabCollider.gameObject.SetActive(false);
            defaultColliders.gameObject.SetActive(true);
            onGrabColliders.gameObject.SetActive(false);
        }

        private void OnRelease()
        {
            if (PreventDisableOfCollidersForObjectDisable)
            {
                handGrabCollider.gameObject.SetActive(true);
                defaultColliders.gameObject.SetActive(true);
                onGrabColliders.gameObject.SetActive(true);
            }
            else
            {
                handGrabCollider.gameObject.SetActive(false);
                defaultColliders.gameObject.SetActive(true);
                onGrabColliders.gameObject.SetActive(false);
            }

            PreventDisableOfCollidersForObjectDisable = false;
        }

        public void TurnOnAllCollidersToRemoveXRFromManager()
        {
            handGrabCollider.gameObject.SetActive(true);
            defaultColliders.gameObject.SetActive(true);
            onGrabColliders.gameObject.SetActive(true);
            PreventDisableOfCollidersForObjectDisable = true;
        }

        private void OnGrab()
        {
            handGrabCollider.gameObject.SetActive(true);
            defaultColliders.gameObject.SetActive(false);
            onGrabColliders.gameObject.SetActive(true);
        }
    }
}