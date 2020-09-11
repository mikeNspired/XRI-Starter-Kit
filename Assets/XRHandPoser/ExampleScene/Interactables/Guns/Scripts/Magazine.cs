// Copyright (c) MikeNspired. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        [SerializeField] private new Collider collider = null;
        [SerializeField] private Rigidbody rigidBody = null;
        public bool IsBeingGrabbed() => isBeingGrabbed;
        public GunType GunType => gunType;
        private Vector3 startingColliderPosition;

        private void Start()
        {
            startingColliderPosition = collider.transform.localPosition;
            
            OnValidate();
            GetComponent<XRGrabInteractable>().onSelectEnter.AddListener(x => OnGrab());
            GetComponent<XRGrabInteractable>().onSelectExit.AddListener(x => isBeingGrabbed = false);
        }

        private void OnGrab()
        {
            isBeingGrabbed = true;
            collider.isTrigger = false;
            rigidBody.isKinematic = false;
        }

        private void OnEnable()
        {
            collider.transform.localPosition = startingColliderPosition;
        }

        public void DisableCollider()
        {
            StartCoroutine(MoveAndDisableCollider());
        }

        public void EnableCollider()
        {
            ReturnMovedColliders();
            collider.enabled = true;
            EnableDistanceGrabbing(true);
        }

        public void ResetToGrabbableObject()
        {
            EnableCollider();
            isBeingGrabbed = false;
            collider.isTrigger = false;
            rigidBody.isKinematic = false;
            transform.parent = null;
        }
        public void SetupForGunAttachment()
        {
            collider.isTrigger = true;
            rigidBody.isKinematic = true;
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            
            EnableDistanceGrabbing(false);
        }
        private void EnableDistanceGrabbing(bool state)
        {
            if (state)
            {
                GetComponent<Highlight>()?.EnableHighlighting();
                GetComponent<InteractableItemData>().canDistanceGrab = true;
            }
            else
            {
                GetComponent<Highlight>()?.DisableHighlighting();
                GetComponent<InteractableItemData>().canDistanceGrab = false;
            }
        }
        

        private void OnValidate()
        {
            if (!collider)
                collider = GetComponentInChildren<Collider>();
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

            collider.transform.position += Vector3.one * 9999;
            //Lets physics respond to collider disappearing before disabling object physics update needs to run twice
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);
            collider.enabled = false;
            collider.transform.localPosition = startingColliderPosition;
        }

        public void ReturnMovedColliders()
        {
            StopAllCoroutines();
            collider.transform.localPosition = startingColliderPosition;
        }
    }
}