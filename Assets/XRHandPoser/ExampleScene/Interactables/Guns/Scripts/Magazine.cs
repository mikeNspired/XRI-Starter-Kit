// Author MikeNspired. 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class Magazine : MonoBehaviour, IReturnMovedColliders
    {
        public int MaxAmmo = 10;
        public int CurrentAmmo = 10;
        private bool isBeingGrabbed = false;

        [SerializeField] private GunType gunType = null;
        [SerializeField] private GameObject ammoModels = null;
        [FormerlySerializedAs("collider")] [SerializeField] private Collider _ = null;
        [SerializeField] private Rigidbody rigidBody = null;
        public bool IsBeingGrabbed() => isBeingGrabbed;
        public GunType GunType => gunType;
        private Vector3 startingColliderPosition;

        private void Start()
        {
            startingColliderPosition = _.transform.localPosition;
            
            OnValidate();
            GetComponent<XRGrabInteractable>().onSelectEntered.AddListener(x => OnGrab());
            GetComponent<XRGrabInteractable>().onSelectExited.AddListener(x => isBeingGrabbed = false);
            GetComponent<XRGrabInteractable>().onSelectExited.AddListener(x => ResetToGrabbableObject());
        }

        private void OnGrab()
        {
            isBeingGrabbed = true;
            _.isTrigger = false;
            rigidBody.isKinematic = true;
        }

        private void OnEnable()
        {
            _.transform.localPosition = startingColliderPosition;
        }

        public void DisableCollider()
        {
            StartCoroutine(MoveAndDisableCollider());
        }

        public void EnableCollider()
        {
            ReturnMovedColliders();
            _.enabled = true;
            EnableDistanceGrabbing(true);
        }

        public void ResetToGrabbableObject()
        {
            EnableCollider();
            isBeingGrabbed = false;
            _.isTrigger = false;
            rigidBody.isKinematic = false;
            transform.parent = null;
        }
        public void SetupForGunAttachment()
        {
            _.isTrigger = true;
            rigidBody.isKinematic = true;
            rigidBody.useGravity = true;
            
            EnableDistanceGrabbing(false);
        }
        
        
        //TODO remove this method
        private void EnableDistanceGrabbing(bool state)
        {
            GetComponent<InteractableItemData>().canDistanceGrab = state;
        }
        

        private void OnValidate()
        {
            if (!_)
                _ = GetComponentInChildren<Collider>();
            if (!rigidBody)
                rigidBody = GetComponentInChildren<Rigidbody>();
        }

        public bool UseAmmo()
        {
            if (CurrentAmmo <= 0) 
                return false;

            CurrentAmmo--;
            
            if (CurrentAmmo <= 0) 
                ammoModels.SetActive(false);

            return true;
        }

        private IEnumerator MoveAndDisableCollider()
        {
            //objectToMove.GetComponent<CollidersSetToTrigger>()?.SetAllToTrigger();
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);

            _.transform.position += Vector3.one * 9999;
            //Lets physics respond to collider disappearing before disabling object physics update needs to run twice
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);
            _.enabled = false;
            _.transform.localPosition = startingColliderPosition;
        }

        public void ReturnMovedColliders()
        {
            StopAllCoroutines();
            _.transform.localPosition = startingColliderPosition;
        }
    }
}