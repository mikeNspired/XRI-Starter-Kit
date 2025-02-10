// Author MikeNspired.

using System;
using System.Collections;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static Unity.Mathematics.math; // For "remap" or other math utility

namespace MikeNspired.XRIStarterKit
{
    public class ProjectileWeapon : MonoBehaviour
    {
        [Header("Required Refs")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private Rigidbody projectilePrefab;
        [SerializeField] private ParticleSystem cartridgeEjection;
        [SerializeField] private AudioSource fireAudio;
        [SerializeField] private AudioSource outOfAmmoAudio;
        [SerializeField] private MatchTransform bulletFlash;
        [SerializeField] private GunCocking gunCocking;

        [Header("Settings")]
        public MagazineAttachPoint magazineAttach = null;
        public float recoilAmount = -0.03f;
        public float recoilRotation = 1;
        public float recoilTime = 0.06f;
        public int bulletsPerShot = 1;
        public float bulletSpreadAngle = 1;
        public float bulletSpeed = 150;
        public bool infiniteAmmo = false;
        public float hapticDuration = 0.1f;
        public float hapticStrength = 0.5f;

        [Header("Auto-Fire")]
        public float fireSpeed = 0.25f;
        public bool automaticFiring = false;

        private XRGrabInteractable interactable;
        private XRBaseInteractor controller;
        private Collider[] gunColliders;
        private bool gunCocked, isFiring;
        private float fireTimer;

        // Events
        public UnityEvent BulletFiredEvent, OutOfAmmoEvent, FiredLastBulletEvent;

        void Awake()
        {
            OnValidate();

            interactable.activated.AddListener(_ => TryFire(true));
            interactable.deactivated.AddListener(_ => TryFire(false));
            interactable.selectEntered.AddListener(SetupRecoilVariables);
            interactable.selectExited.AddListener(DestroyRecoilTracker);

            if (gunCocking)
                gunCocking.GunCockedEvent.AddListener(() => gunCocked = true);
        }

        void OnValidate()
        {
            if (!gunCocking) gunCocking = GetComponentInChildren<GunCocking>();
            if (!interactable) interactable = GetComponent<XRGrabInteractable>();
        }

        // Expression-bodied for simple subscribe/unsubscribe
        void OnEnable() => Application.onBeforeRender += RecoilUpdate;
        void OnDisable() => Application.onBeforeRender -= RecoilUpdate;

        void Update()
        {
            if (!automaticFiring) return;

            // Fire continuously if trigger is held
            if (isFiring && fireTimer >= fireSpeed)
            {
                FireGun();
                fireTimer = 0f;
            }
            fireTimer += Time.deltaTime;
        }

        private void TryFire(bool state)
        {
            isFiring = state;
            // If not automatic, fire immediately once
            if (state && !automaticFiring) FireGun();
        }

        
        public void FireGun()
        {
            // Prevent firing with no bullets per shot
            if (bulletsPerShot < 1) return;

            // Check if we have ammo, or if the gun is cocked
            if (magazineAttach && !infiniteAmmo && (CheckIfGunCocked() || !magazineAttach.Magazine || !magazineAttach.Magazine.UseAmmo()))
            {
                OutOfAmmoEvent.Invoke();
                outOfAmmoAudio.PlayOneShot(outOfAmmoAudio.clip);
                gunCocked = false;
                return;
            }

            // If there's a GunCocking script, ensure it’s cocked
            if (gunCocking && !gunCocked)
            {
                OutOfAmmoEvent.Invoke();
                outOfAmmoAudio.PlayOneShot(outOfAmmoAudio.clip);
                return;
            }

            // Fire multiple projectiles if bulletsPerShot > 1
            for (int i = 0; i < bulletsPerShot; i++)
            {
                Vector3 shotDirection = Vector3.Slerp(
                    firePoint.forward,
                    UnityEngine.Random.insideUnitSphere,
                    bulletSpreadAngle / 180f
                );

                var bullet = Instantiate(projectilePrefab);
                IgnoreColliders(bullet);

                // Set bullet position/rotation and launch
                bullet.transform.SetPositionAndRotation(
                    firePoint.position, Quaternion.LookRotation(shotDirection)
                );
                bullet.AddForce(bullet.transform.forward * bulletSpeed, ForceMode.VelocityChange);

                // Simple haptic
                controller.GetComponentInParent<HapticImpulsePlayer>().SendHapticImpulse(hapticStrength, hapticDuration);

                BulletFiredEvent.Invoke();

                // Stop recoil coroutines (if any) and start new recoil
                StopAllCoroutines();
                StartRecoil();
            }

            // If we just fired the last bullet in the mag
            if (magazineAttach && magazineAttach.Magazine && magazineAttach.Magazine.CurrentAmmo == 0)
                FiredLastBulletEvent.Invoke();

            // Optional muzzle flash
            if (bulletFlash)
            {
                var flash = Instantiate(bulletFlash);
                flash.transform.position = firePoint.position;
                flash.positionToMatch = firePoint; // Follow the barrel
            }

            // Audio + Particle
            fireAudio?.PlayOneShot(fireAudio.clip);
            if(cartridgeEjection)
                cartridgeEjection.Play();
        }

        private void IgnoreColliders(Component bullet)
        {
            gunColliders = GetComponentsInChildren<Collider>(true);
            var bulletCollider = bullet.GetComponentInChildren<Collider>();
            foreach (var c in gunColliders) Physics.IgnoreCollision(c, bulletCollider);
        }

        // Single-line expression method
        private bool CheckIfGunCocked() => gunCocking && !gunCocked;

        #region Recoil

        private Transform recoilTracker;
        private Quaternion startingRotation;
        private Vector3 endOfRecoilPosition;
        private Quaternion endOfRecoilRotation;
        private float timer;
        private bool isRecoiling;
        private Vector3 controllerToAttachDelta;

        private void SetupRecoilVariables(SelectEnterEventArgs args)
        {
            controller = args.interactorObject as XRBaseInteractor;
            StartCoroutine(SetupRecoil(interactable.attachEaseInTime));
        }

        private void DestroyRecoilTracker(SelectExitEventArgs args)
        {
            StopAllCoroutines();
            if (recoilTracker) Destroy(recoilTracker.gameObject);
            isRecoiling = false;
        }

        private IEnumerator SetupRecoil(float interactableAttachEaseInTime)
        {
            // Quick check for a HandReference script
            var handReference = controller.GetComponentInParent<HandReference>();
            if (!handReference) yield break;

            recoilTracker = new GameObject($"{name} Recoil Tracker").transform;
            recoilTracker.parent = controller.attachTransform;

            // Optionally wait for the attach time to finish
            yield return null;
        }

        private void StartRecoil()
        {
            // If there's no recoil tracker yet, create it
            if (!recoilTracker) StartCoroutine(SetupRecoil(1));

            recoilTracker.localRotation = startingRotation;
            recoilTracker.localPosition = Vector3.zero;
            startingRotation = transform.localRotation;

            timer = 0f;
            controllerToAttachDelta = transform.position - recoilTracker.position;
            isRecoiling = true;
        }

        [BeforeRenderOrder(101)]
        private void RecoilUpdate()
        {
            if (!isRecoiling) return;

            if (timer < recoilTime / 2f)
            {
                // Move & rotate the gun backward for recoil
                if (Math.Abs(recoilAmount) > 0.001f)
                {
                    recoilTracker.position += transform.forward * recoilAmount * Time.deltaTime;
                    transform.position = recoilTracker.position + controllerToAttachDelta;
                }

                if (Math.Abs(recoilRotation) > 0.001f)
                    transform.Rotate(Vector3.right, -recoilRotation * Time.deltaTime, Space.Self);

                endOfRecoilPosition = recoilTracker.localPosition;
                endOfRecoilRotation = transform.localRotation;
            }
            else
            {
                // Return gun back to original position/rotation
                float t = remap(recoilTime / 2f, recoilTime, 0f, 1f, timer);
                recoilTracker.localPosition = Vector3.Lerp(endOfRecoilPosition, Vector3.zero, t);
                var newRotation = Quaternion.Lerp(endOfRecoilRotation, startingRotation, t);

                transform.position = recoilTracker.position + controllerToAttachDelta;
                transform.localRotation = newRotation;
            }

            timer += Time.deltaTime;
            if (timer > recoilTime)
                isRecoiling = false;
        }

        #endregion
    }
}
