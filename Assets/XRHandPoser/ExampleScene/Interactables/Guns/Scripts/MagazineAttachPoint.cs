using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class MagazineAttachPoint : MonoBehaviour
    {
        [SerializeField] private Transform start, end;
        [SerializeField] private float alignAnimationLength = .05f;
        [SerializeField] private float insertAnimationLength = .1f;
        [SerializeField] private AudioSource AudioSource;
        [SerializeField] private GunType gunType;
        public bool isBeingGrabbed;
        public Magazine magazine;
        private XRGrabInteractable xrGrabInteractable;
        private XRInteractionManager interactionManager;

        private bool ammoIsAttached;

        private void Start()
        {
            OnValidate();
            xrGrabInteractable.onSelectEnter.AddListener(x => isBeingGrabbed = true);
            xrGrabInteractable.onSelectExit.AddListener(x => isBeingGrabbed = false);
        }

        private void OnValidate()
        {
            if (!AudioSource)
                AudioSource = GetComponent<AudioSource>();
            if (!xrGrabInteractable)
                xrGrabInteractable = GetComponentInParent<XRGrabInteractable>();
            if (!interactionManager)
                interactionManager = FindObjectOfType<XRInteractionManager>();
        }


        public void OnTriggerEnter(Collider other)
        {
            if (ammoIsAttached) return;

            Magazine collidedMagazine = other.attachedRigidbody?.GetComponent<Magazine>();
            if (collidedMagazine && collidedMagazine.gunType == gunType && CheckIfBothGrabbed(collidedMagazine))
            {
                SetMagazine(collidedMagazine);
                other.isTrigger = true;
                ReleaseItemFromHand();
                
                magazine.GetComponent<Rigidbody>().isKinematic = true;
                magazine.GetComponent<Rigidbody>().velocity = Vector3.zero;
                magazine.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

                magazine.transform.parent = transform;

                ammoIsAttached = true;
                StartCoroutine(StartAnimation(other.attachedRigidbody.transform));
            }
        }

        private void SetMagazine(Magazine collidedMagazine)
        {
            magazine = collidedMagazine;
            magazine.GetComponent<XRGrabInteractable>().onSelectEnter.AddListener(AmmoRemoved); 
        }

        private bool CheckIfBothGrabbed(Magazine magazine)
        {
            return isBeingGrabbed && magazine.isBeingGrabbed;
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
            magazine.GetComponent<Rigidbody>().isKinematic = false;
            magazine.transform.parent = null;
            magazine = null;
            Invoke(nameof(Test),1);
        }

        private void Test()
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

            PlaySound();

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

        private void PlaySound()
        {
            AudioSource.Play();
        }
    }
}