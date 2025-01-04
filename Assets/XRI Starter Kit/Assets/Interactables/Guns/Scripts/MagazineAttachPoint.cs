// Author MikeNspired.

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.UnityXRHandPoser
{
    public class MagazineAttachPoint : MonoBehaviour
    {
        [SerializeField] private Transform start, end;
        [SerializeField] private float alignAnimationLength = 0.05f;
        [SerializeField] private float insertAnimationLength = 0.1f;
        [SerializeField] private AudioSource loadAudio, unloadAudio;
        [SerializeField] private GunType gunType = null;
        [SerializeField] private Magazine startingMagazine = null;
        [SerializeField] private new Collider collider = null;
        [SerializeField] private bool removeByGrabbing = true;

        private XRGrabInteractable xrGrabInteractable;
        private XRInteractionManager interactionManager;
        private Magazine magazine;
        private bool ammoIsAttached;
        private bool isBeingGrabbed;

        public Magazine Magazine => magazine;
        public GunType GunType => gunType;

        private void Awake()
        {
            OnValidate();

            xrGrabInteractable.selectEntered.AddListener(_ => SetMagazineGrabbableState());
            xrGrabInteractable.selectExited.AddListener(_ => SetMagazineGrabbableState());

            collider.gameObject.SetActive(false);
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

            isBeingGrabbed = xrGrabInteractable.isSelected;

            if (removeByGrabbing)
                magazine.EnableCollider();
            else
                magazine.DisableCollider();
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
            SetupNewMagazine(Instantiate(startingMagazine, end.position, end.rotation, transform));
            magazine.DisableCollider();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ammoIsAttached) return;

            var collidedMagazine = other.attachedRigidbody?.GetComponent<Magazine>();
            if (collidedMagazine && collidedMagazine.GunType == gunType && CheckIfBothGrabbed(collidedMagazine))
            {
                if (collidedMagazine.CurrentAmmo <= 0) return;
                ReleaseItemFromHand(collidedMagazine);
                SetupNewMagazine(collidedMagazine);
                StartCoroutine(StartAnimation(other.attachedRigidbody.transform));
            }
        }

        private void SetupNewMagazine(Magazine mag)
        {
            magazine = mag;
            magazine.GetComponent<XRGrabInteractable>().selectEntered.AddListener(_ => AmmoRemoved());
            magazine.SetupForGunAttachment();
            magazine.transform.parent = transform;
            ammoIsAttached = true;
        }

        private bool CheckIfBothGrabbed(Magazine magazine) => isBeingGrabbed && magazine.IsBeingGrabbed();

        private void ReleaseItemFromHand(Magazine collidedMagazine)
        {
            var interactor = collidedMagazine.GetComponent<XRGrabInteractable>().firstInteractorSelecting;
            interactionManager.SelectExit(interactor, collidedMagazine.GetComponent<XRGrabInteractable>());
        }

        private void AmmoRemoved()
        {
            StopAllCoroutines();

            magazine.GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(_ => AmmoRemoved());

            magazine = null;
            unloadAudio.Play();
            Invoke(nameof(PreventAutoAttach), 0.15f);
        }

        private void PreventAutoAttach()
        {
            ammoIsAttached = false;
        }

        private IEnumerator StartAnimation(Transform ammo)
        {
            yield return AnimateTransform(ammo, start.localPosition, start.localRotation, alignAnimationLength);
            loadAudio.Play();
            yield return AnimateTransform(ammo, end.localPosition, end.localRotation, insertAnimationLength);
        }

        public void EjectMagazine()
        {
            StopAllCoroutines();
            StartCoroutine(EjectMagazineAnimation(magazine.transform));
        }

        private IEnumerator EjectMagazineAnimation(Transform ammo)
        {
            unloadAudio.Play();
            yield return AnimateTransform(ammo, start.localPosition, start.localRotation, insertAnimationLength);

            magazine.GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(_ => AmmoRemoved());
            magazine.ResetToGrabbableObject();
            magazine = null;
            ammoIsAttached = false;
            collider.gameObject.SetActive(true);
        }

        private IEnumerator AnimateTransform(Transform target, Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            float timer = 0;
            var startPosition = target.localPosition;
            var startRotation = target.localRotation;

            while (timer < duration)
            {
                float progress = timer / duration;
                target.localPosition = Vector3.Lerp(startPosition, targetPosition, progress);
                target.localRotation = Quaternion.Lerp(startRotation, targetRotation, progress);
                timer += Time.deltaTime;
                yield return null;
            }

            target.localPosition = targetPosition;
            target.localRotation = targetRotation;
        }
    }
}
