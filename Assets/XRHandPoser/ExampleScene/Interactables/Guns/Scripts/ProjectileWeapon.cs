// Author MikeNspired. 

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using static Unity.Mathematics.math;

namespace MikeNspired.UnityXRHandPoser
{
    public class ProjectileWeapon : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private Rigidbody projectilePrefab;
        [SerializeField] private ParticleSystem cartridgeEjection;
        [SerializeField] private AudioSource fireAudio;
        [SerializeField] private AudioSource outOfAmmoAudio;
        [SerializeField] private MatchTransform bulletFlash;
        [SerializeField] private GunCocking gunCocking;

        //All public for in game changes
        public MagazineAttachPoint magazineAttach = null;
        public float recoilAmount = -0.03f;
        public float recoilRotation = 1;
        public float recoilTime = .06f;
        public int bulletsPerShot = 1;
        public float bulletSpreadAngle = 1;
        public float bulletSpeed = 150;
        public bool infiniteAmmo = false;
        public float hapticDuration = .1f;
        public float hapticStrength = .5f;

        //AutoFire
        public float fireSpeed = .25f;
        public bool automaticFiring = false;

        private XRGrabInteractable interactable;
        private XRBaseInteractor controller;
        private Collider[] gunColliders;
        private bool gunCocked, isFiring;
        private float fireTimer;

        public UnityEvent BulletFiredEvent, OutOfAmmoEvent, FiredLastBulletEvent;

        private void Awake()
        {
            OnValidate();
            interactable.activated.AddListener(x => TryFire(true));
            interactable.deactivated.AddListener(x => TryFire(false));
            interactable.onSelectEntered.AddListener(SetupRecoilVariables);
            interactable.onSelectExited.AddListener(DestroyRecoilTracker);

            if (gunCocking)
                gunCocking.GunCockedEvent.AddListener(() => gunCocked = true);
        }

        private void OnValidate()
        {
            if (!gunCocking)
                gunCocking = GetComponentInChildren<GunCocking>();
            if (!interactable)
                interactable = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable() => Application.onBeforeRender += RecoilUpdate;

        private void OnDisable() => Application.onBeforeRender -= RecoilUpdate;

        private void TryFire(bool state)
        {
            isFiring = state;
            if (state && !automaticFiring)
                FireGun();
        }

        private void Update()
        {
            if (!automaticFiring) return;

            if (isFiring && fireTimer >= fireSpeed)
            {
                FireGun();
                fireTimer = 0;
            }

            fireTimer += Time.deltaTime;
        }

        public void FireGun()
        {
            if (bulletsPerShot < 1) return;

            if (magazineAttach && !infiniteAmmo && (CheckIfGunCocked() || !magazineAttach.Magazine || !magazineAttach.Magazine.UseAmmo()))
            {
                OutOfAmmoEvent.Invoke();
                outOfAmmoAudio.PlayOneShot(outOfAmmoAudio.clip);
                gunCocked = false;
                return;
            }

            if (gunCocking && !gunCocked)
            {
                OutOfAmmoEvent.Invoke();
                outOfAmmoAudio.PlayOneShot(outOfAmmoAudio.clip);
                return;
            }

            for (int i = 0; i < bulletsPerShot; i++)
            {
                Vector3 shotDirection = Vector3.Slerp(firePoint.forward, UnityEngine.Random.insideUnitSphere, bulletSpreadAngle / 180f);
                var bullet = Instantiate(projectilePrefab);
                IgnoreColliders(bullet);

                bullet.transform.SetPositionAndRotation(firePoint.position, Quaternion.LookRotation(shotDirection));
                bullet.AddForce((bullet.transform.forward * bulletSpeed), ForceMode.VelocityChange);

                controller.GetComponentInParent<ActionBasedController>().SendHapticImpulse(hapticStrength, hapticDuration);

                BulletFiredEvent.Invoke();
                StopAllCoroutines();
                StartRecoil();
            }

            if (magazineAttach && magazineAttach.Magazine && magazineAttach.Magazine.CurrentAmmo == 0)
                FiredLastBulletEvent.Invoke();

            if (bulletFlash)
            {
                var flash = Instantiate(bulletFlash);
                flash.transform.position = firePoint.position;
                flash.positionToMatch = firePoint; //Follow gun barrel on update  
            }

            if (fireAudio)
                fireAudio.PlayOneShot(fireAudio.clip);

            if (cartridgeEjection)
                cartridgeEjection.Play();
        }

        private void IgnoreColliders(Component bullet)
        {
            gunColliders = GetComponentsInChildren<Collider>(true);
            var bulletCollider = bullet.GetComponentInChildren<Collider>();
            foreach (var c in gunColliders) Physics.IgnoreCollision(c, bulletCollider);
        }

        private bool CheckIfGunCocked()
        {
            return gunCocking && !gunCocked;
        }

        #region Recoil

        private Transform recoilTracker;
        private Quaternion startingRotation;
        private Vector3 endOfRecoilPosition;
        private Quaternion endOfRecoilRotation;
        private float timer = 0;
        private bool isRecoiling;
        private Vector3 controllerToAttachDelta;

        private void SetupRecoilVariables(XRBaseInteractor interactor)
        {
            controller = interactor;
            StartCoroutine(SetupRecoil(interactable.attachEaseInTime));
        }

        private void DestroyRecoilTracker(XRBaseInteractor interactor)
        {
            StopAllCoroutines();
            if (recoilTracker)
                Destroy(recoilTracker.gameObject);
            isRecoiling = false;
        }

        private IEnumerator SetupRecoil(float interactableAttachEaseInTime)
        {
            var handReference = controller.GetComponentInParent<HandReference>();
            if (!handReference) yield break;

            recoilTracker = new GameObject().transform;
            recoilTracker.parent = controller.attachTransform;
            recoilTracker.name = gameObject.name + " Recoil Tracker";

            yield return null;
        }

        private void StartRecoil()
        {
            if (!recoilTracker) StartCoroutine(SetupRecoil(1));
            recoilTracker.localRotation = startingRotation;
            recoilTracker.localPosition = Vector3.zero;
            startingRotation = transform.localRotation;

            timer = 0;
            controllerToAttachDelta = transform.position - recoilTracker.transform.position;
            isRecoiling = true;
        }

        [BeforeRenderOrder(101)]
        private void RecoilUpdate()
        {
            if (!isRecoiling) return;

            if (timer < recoilTime / 2)
            {
                if (Math.Abs(recoilAmount) > .001f)
                {
                    recoilTracker.position += transform.forward * recoilAmount * Time.deltaTime;
                    transform.position = recoilTracker.position + controllerToAttachDelta;
                }

                if (Math.Abs(recoilRotation) > .001f)
                {
                    transform.Rotate(Vector3.right, -recoilRotation * Time.deltaTime, Space.Self);
                    // transform.rotation = recoilTracker.rotation;
                }

                endOfRecoilPosition = recoilTracker.localPosition;
                endOfRecoilRotation = transform.localRotation;
            }
            else
            {
                var timerRemappedPercentage = remap(recoilTime / 2, recoilTime, 0, 1, timer);
                var newPosition = Vector3.Lerp(endOfRecoilPosition, Vector3.zero, timerRemappedPercentage);
                var newRotation = Quaternion.Lerp(endOfRecoilRotation, startingRotation, timerRemappedPercentage);
                recoilTracker.localPosition = newPosition;
                //recoilTracker.localRotation = newRotation;

                transform.position = recoilTracker.position + controllerToAttachDelta;
                transform.localRotation = newRotation;
                //Debug.Break();
            }

            timer += Time.deltaTime;
            if (timer > recoilTime)
                isRecoiling = false;
        }

        #endregion
    }
}