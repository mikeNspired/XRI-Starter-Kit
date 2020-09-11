// Copyright (c) MikeNspired. All Rights Reserved.

using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class MagazineAttachPoint : MonoBehaviour
    {
        [SerializeField] private Transform start = null, end = null;
        [SerializeField] private float alignAnimationLength = .05f;
        [SerializeField] private float insertAnimationLength = .1f;
        [SerializeField] private AudioSource loadAudio = null;
        [SerializeField] private AudioSource unloadAudio = null;
        [SerializeField] private GunType gunType = null;
        [SerializeField] private Magazine startingMagazine = null;
        [SerializeField] private new Collider collider = null;
        [SerializeField] private bool removeByGrabbing = true;

        private XRGrabInteractable xrGrabInteractable;
        private XRInteractionManager interactionManager;
        private Magazine magazine;
        public Magazine Magazine => magazine;
        public GunType GunType => gunType;

        private bool ammoIsAttached; //Used to stop from quickly attaching again when removed
        private bool isBeingGrabbed;

        private void Awake()
        {
            OnValidate();

            //Invoked after a frame to check if the user switched hands
            xrGrabInteractable.onSelectEnter.AddListener(x => SetMagazineGrabbableState());
            xrGrabInteractable.onSelectExit.AddListener(x => SetMagazineGrabbableState());

            collider.gameObject.SetActive(true);
            if (startingMagazine) CreateStartingMagazine();
        }

        private void SetMagazineGrabbableState()
        {
            CancelInvoke();
            Invoke(nameof(MakeMagazineGrabbable), Time.deltaTime);
            Invoke(nameof(EnableOrDisableAttachCollider), Time.deltaTime);
        }

        private void EnableOrDisableAttachCollider()
        {
            collider.gameObject.SetActive(xrGrabInteractable.isSelected);
        }

        private void MakeMagazineGrabbable()
        {
            if (!magazine) return;

            if (xrGrabInteractable.isSelected && removeByGrabbing)
            {
                magazine.EnableCollider();
                isBeingGrabbed = true;
            }
            else if(xrGrabInteractable.isSelected)
                isBeingGrabbed = true;
            else
            {
                magazine.DisableCollider();
                isBeingGrabbed = false;
            }
        }

        private void OnValidate()
        {
            if (!xrGrabInteractable)
                xrGrabInteractable = GetComponentInParent<XRGrabInteractable>();
            if (!interactionManager)
                interactionManager = FindObjectOfType<XRInteractionManager>();
        }

        private void CreateStartingMagazine()
        {
            if (magazine) return;
            magazine = Instantiate(startingMagazine, transform.position, end.rotation, transform);
            SetupNewMagazine(magazine);
            ammoIsAttached = true;
            MakeMagazineGrabbable();
        }


        public void OnTriggerEnter(Collider other)
        {
            if (ammoIsAttached) return;

            Magazine collidedMagazine = other.attachedRigidbody?.GetComponent<Magazine>();

            if (collidedMagazine && collidedMagazine.GunType == gunType && CheckIfBothGrabbed(collidedMagazine))
            {
                if (collidedMagazine.CurrentAmmo <= 0) return;
                ammoIsAttached = true;
                magazine = collidedMagazine;
                ReleaseItemFromHand();
                SetupNewMagazine(collidedMagazine);
                StartCoroutine(StartAnimation(other.attachedRigidbody.transform));
            }
        }

        private void SetupNewMagazine(Magazine mag)
        {
            magazine = mag;
            magazine.GetComponent<XRGrabInteractable>().onSelectEnter.AddListener(AmmoRemoved);
            magazine.SetupForGunAttachment();
            magazine.transform.parent = transform;
        }


        private bool CheckIfBothGrabbed(Magazine magazine)
        {
            return isBeingGrabbed && magazine.IsBeingGrabbed();
        }

        private void ReleaseItemFromHand()
        {
            XRBaseInteractor interactor = magazine.GetComponent<XRGrabInteractable>().selectingInteractor;
            XRBaseInteractable interactable = magazine.GetComponent<XRBaseInteractable>();
            interactionManager.SelectExit_public(interactor, interactable);
        }

        private void AmmoRemoved(XRBaseInteractor arg0)
        {
            StopAllCoroutines();
            magazine.GetComponent<XRGrabInteractable>().onSelectEnter.RemoveListener(AmmoRemoved);
            magazine.transform.parent = null;
            magazine = null;
            unloadAudio.Play();

            Invoke(nameof(PreventAutoAttach), .15f);
        }

        private void PreventAutoAttach()
        {
            ammoIsAttached = false;
        }

        private IEnumerator StartAnimation(Transform ammo)
        {
            float timer = 0;
            Vector3 startPos = ammo.localPosition;
            Quaternion startRot = ammo.localRotation;

            //Align magazine with start point
            while (timer < alignAnimationLength + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(startPos, start.localPosition, timer / alignAnimationLength);
                var newRotation = Quaternion.Lerp(startRot, start.localRotation, timer / alignAnimationLength);

                ammo.localPosition = newPosition;
                ammo.localRotation = newRotation;

                yield return new WaitForSeconds(Time.deltaTime);
                timer += Time.deltaTime;
            }

            loadAudio.Play();

            timer = 0;

            //Slide magazine into gun
            while (timer < insertAnimationLength + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(start.localPosition, end.localPosition, timer / insertAnimationLength);
                var newRotation = Quaternion.Lerp(start.localRotation, end.localRotation, timer / insertAnimationLength);

                ammo.localPosition = newPosition;
                ammo.localRotation = newRotation;

                yield return new WaitForSeconds(Time.deltaTime);
                timer += Time.deltaTime;
            }
        }

        public void EjectMagazine()
        {
            StopAllCoroutines();
            StartCoroutine(EjectMagazineAnimation(magazine.transform));
        }

        private IEnumerator EjectMagazineAnimation(Transform ammo)
        {
            float timer = 0;
            unloadAudio.Play();

            //Slide magazine out
            while (timer < insertAnimationLength + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(end.localPosition, start.localPosition, timer / insertAnimationLength);
                var newRotation = Quaternion.Lerp(end.localRotation, start.localRotation, timer / insertAnimationLength);

                ammo.localPosition = newPosition;
                ammo.localRotation = newRotation;

                yield return new WaitForSeconds(Time.deltaTime);
                timer += Time.deltaTime;
            }

            magazine.GetComponent<XRGrabInteractable>().onSelectEnter.RemoveListener(AmmoRemoved);
            magazine.ResetToGrabbableObject();
            magazine = null;
            ammoIsAttached = false;
            collider.gameObject.SetActive(true);
        }
    }
}