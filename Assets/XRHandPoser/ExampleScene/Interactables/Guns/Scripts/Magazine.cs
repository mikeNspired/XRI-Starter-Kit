// Copyright (c) MikeNspired. All Rights Reserved.

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class Magazine : MonoBehaviour
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
            if (CurrentAmmo <= 0)
            {
                ammoModels.SetActive(false);
                return false;
            }

            CurrentAmmo--;
            return true;
        }
    }
}