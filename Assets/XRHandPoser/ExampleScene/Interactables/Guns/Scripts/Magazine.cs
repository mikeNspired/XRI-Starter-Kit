﻿// Copyright (c) MikeNspired. All Rights Reserved.

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class Magazine : MonoBehaviour
    {
        public int AmmoCount = 10;
        public GunType gunType = null;
        private bool isBeingGrabbed = false;

        [SerializeField] private GameObject ammoModels = null;
        [SerializeField] private Collider collider = null;
        [SerializeField] private new Rigidbody rigidBody = null;
        public bool IsBeingGrabbed() => isBeingGrabbed;


        private void Start()
        {
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

        public void SetupForGunAttachment()
        {
            collider.isTrigger = true;
            rigidBody.isKinematic = true;
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
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
            if (AmmoCount <= 0)
            {
                ammoModels.SetActive(false);
                return false;
            }

            AmmoCount--;
            return true;
        }
    }
}