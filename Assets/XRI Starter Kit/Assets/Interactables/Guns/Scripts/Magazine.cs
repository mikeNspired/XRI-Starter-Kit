// Author MikeNspired. 

using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public class Magazine : MonoBehaviour, IReturnMovedColliders
    {
        public int MaxAmmo = 10;
        public int CurrentAmmo = 10;
        private bool isBeingGrabbed = false;

        [SerializeField] private GunType gunType = null;
        [SerializeField] private GameObject ammoModels = null;
        [FormerlySerializedAs("collider")] [SerializeField] private Collider magazineCollider = null;
        [SerializeField] private Rigidbody rigidBody = null;

        private XRGrabInteractable grabInteractable;
        private Vector3 startingColliderPosition;
        private Rigidbody rb;
        
        public bool IsBeingGrabbed() => isBeingGrabbed;
        public GunType GunType => gunType;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            magazineCollider = magazineCollider ? magazineCollider : GetComponentInChildren<Collider>();
            rigidBody = rigidBody ? rigidBody : GetComponentInChildren<Rigidbody>();

            startingColliderPosition = magazineCollider.transform.localPosition;

            RegisterEvents();
        }
        
        private void RegisterEvents()
        {
            grabInteractable.selectEntered.AddListener(_ => OnGrab());
            grabInteractable.selectExited.AddListener(_ => OnRelease());
        }

        private void OnGrab()
        {
            isBeingGrabbed = true;
           // magazineCollider.isTrigger = false;
            rigidBody.isKinematic = true;
        }

        private void OnRelease()
        {
            isBeingGrabbed = false;
            ResetToGrabbableObject();
        }

        private void OnEnable()
        {
            magazineCollider.transform.localPosition = startingColliderPosition;
        }

        public void DisableCollider()
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(PhysicsHelper.MoveAndDisableCollider(magazineCollider,startingColliderPosition));
        }

        public void EnableCollider()
        {
            ReturnMovedColliders();
            magazineCollider.enabled = true;
            EnableDistanceGrabbing(true);
        }

        public void ResetToGrabbableObject()
        {
            EnableCollider();
            //magazineCollider.isTrigger = false;
            rigidBody.isKinematic = false;
            transform.parent = null;
        }

        public void SetupForGunAttachment()
        {
          //  magazineCollider.isTrigger = true;
            rigidBody.isKinematic = true;
            rigidBody.useGravity = true;
            EnableDistanceGrabbing(false);
        }

        private void EnableDistanceGrabbing(bool state)
        {
            if (TryGetComponent(out InteractableItemData itemData))
                itemData.canDistanceGrab = state;
        }

        public bool UseAmmo()
        {
            if (CurrentAmmo <= 0) 
                return false;

            CurrentAmmo--;

            if (CurrentAmmo <= 0 && ammoModels != null) 
                ammoModels.SetActive(false);

            return true;
        }
        
        public void ReturnMovedColliders()
        {
            StopAllCoroutines();
            magazineCollider.transform.localPosition = startingColliderPosition;
        }
    }
}
