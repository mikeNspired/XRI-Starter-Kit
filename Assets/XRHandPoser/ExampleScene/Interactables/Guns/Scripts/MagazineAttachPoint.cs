// Copyright (c) MikeNspired. All Rights Reserved.

using System.Collections;
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

        private XRGrabInteractable xrGrabInteractable;
        private XRInteractionManager interactionManager;
        private Magazine magazine;
        public Magazine Magazine => magazine;

        private bool ammoIsAttached; //Used to stop from quickly attaching again when removed
        private bool isBeingGrabbed;

        private void Start()
        {
            OnValidate();
            xrGrabInteractable.onSelectEnter.AddListener(x => isBeingGrabbed = true);
            xrGrabInteractable.onSelectExit.AddListener(x => isBeingGrabbed = false);

            collider.gameObject.SetActive(true);
            if (startingMagazine) CreateStartingMagazine();
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
            SetupMagazine(magazine);
            ammoIsAttached = true;
        }


        public void OnTriggerEnter(Collider other)
        {
            if (ammoIsAttached) return;

            Magazine collidedMagazine = other.attachedRigidbody?.GetComponent<Magazine>();

            if (collidedMagazine && collidedMagazine.gunType == gunType && CheckIfBothGrabbed(collidedMagazine))
            {
                ammoIsAttached = true;
                magazine = collidedMagazine;
                ReleaseItemFromHand();
                SetupMagazine(collidedMagazine);
                StartCoroutine(StartAnimation(other.attachedRigidbody.transform));
            }
        }

        private void SetupMagazine(Magazine mag)
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

            Invoke(nameof(PreventAutoAttach), 1);
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
    }
}