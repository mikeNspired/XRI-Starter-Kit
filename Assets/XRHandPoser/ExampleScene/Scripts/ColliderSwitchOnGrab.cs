// Copyright (c) MikeNspired. All Rights Reserved.


using System.Collections;
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
        [SerializeField] private bool moveAndDisableAfterFrameOnGrabColliders;

        // [Tooltip("Optional: Expands the grab point near hand to a larger collider to. NOTE: Must be enabled at start")] [SerializeField]
        // private Transform handGrabCollider = null;

        private bool PreventDisableOfCollidersForObjectDisable;
        
        private void Start()
        {
            OnValidate();
            
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
            defaultColliders.gameObject.SetActive(true);
            onGrabColliders.gameObject.SetActive(false);
        }

        private void OnRelease()
        {
            if (PreventDisableOfCollidersForObjectDisable)
            {
                defaultColliders.gameObject.SetActive(true);
                onGrabColliders.gameObject.SetActive(true);
            }
            else
            {
                defaultColliders.gameObject.SetActive(true);
                // onGrabColliders.gameObject.SetActive(false);
                StartCoroutine(DisableItem(onGrabColliders.transform));
            }

            PreventDisableOfCollidersForObjectDisable = false;
        }

        public void TurnOnAllCollidersToRemoveXRFromManager()
        {
            defaultColliders.gameObject.SetActive(true);
            onGrabColliders.gameObject.SetActive(true);
            PreventDisableOfCollidersForObjectDisable = true;
        }

        private void OnGrab()
        {
            defaultColliders.gameObject.SetActive(false);
            onGrabColliders.gameObject.SetActive(true);
        }

        private Vector3 onGrabDefaultPosition;
        private IEnumerator DisableItem(Transform item)
        {
            onGrabDefaultPosition = item.transform.localPosition;
            item.position += Vector3.one * 9999;
            //Lets physics respond to collider disappearing before disabling object phyics update needs to run twice
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);
            item.gameObject.SetActive(false);
            item.localPosition = onGrabDefaultPosition;
        }
    }
}