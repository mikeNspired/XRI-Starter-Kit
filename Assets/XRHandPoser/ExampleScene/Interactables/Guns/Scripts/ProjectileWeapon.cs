using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{    
    

    public class ProjectileWeapon : MonoBehaviour
    {
        private XRGrabInteractable interactable;

        [SerializeField] private Transform firePoint = null;
        [SerializeField] private Rigidbody projectilePrefab = null;
        [SerializeField] private ParticleSystem cartridgeEjection = null;
        [SerializeField] private AudioSource fireAudio = null;
        [SerializeField] private MatchTransform bulletFlash = null;

        public float recoilAmount = -0.03f;
        public float recoilRotation = 1;
        public float recoilTime = .06f;
        public int bulletsPerShot = 1;
        public float bulletSpreadAngle = 1;
        public float bulletSpeed = 150;

        private XRBaseInteractor controller;

        private void Start()
        {

            interactable = GetComponent<XRGrabInteractable>();
            interactable.onActivate.AddListener(FireBullets);
            interactable.onSelectEnter.AddListener(SetupRecoilVariables);
            interactable.onSelectExit.AddListener(DestroyRecoilTracker);
        }
        private void OnEnable() => Application.onBeforeRender += RecoilUpdate;

        private void OnDisable() => Application.onBeforeRender -= RecoilUpdate;


        //private void LateUpdate() if (Input.GetKeyDown(KeyCode.Space)) FireBullets(null);


        public void FireBullets(XRBaseInteractor interactor)
        {
            if (bulletsPerShot < 1) return;

            for (int i = 0; i < bulletsPerShot; i++)
            {
                Vector3 shotDirection = Vector3.Slerp(firePoint.forward, UnityEngine.Random.insideUnitSphere, bulletSpreadAngle / 180f);
                var bullet = Instantiate(projectilePrefab);
                bullet.transform.SetPositionAndRotation(firePoint.position, Quaternion.LookRotation(shotDirection));
                bullet.AddForce((bullet.transform.forward * bulletSpeed), ForceMode.VelocityChange);
                StopAllCoroutines();
                StartRecoil();
            }

            var flash = Instantiate(bulletFlash);
            flash.positionToMatch = firePoint;
            fireAudio.PlayOneShot(fireAudio.clip);

            if (cartridgeEjection)
                cartridgeEjection.Play();
        }


        private void SetupRecoilVariables(XRBaseInteractor interactor)
        {
            controller = interactor;
            StartCoroutine(SetupRecoil(interactable.attachEaseInTime));
        }

        private void DestroyRecoilTracker(XRBaseInteractor interactor)
        {
            StopAllCoroutines();
            if (recoilTracker)
                DestroyImmediate(recoilTracker.gameObject);
        }


        private Transform recoilTracker;
        private Quaternion startingRotation;

        private IEnumerator SetupRecoil(float interactableAttachEaseInTime)
        {
            yield return new WaitForSeconds(interactableAttachEaseInTime);

            recoilTracker = new GameObject().transform;
            recoilTracker.parent = controller.attachTransform;
            recoilTracker.name = gameObject.name + " Recoil Tracker";
            if (controller.GetComponent<HandReference>().hand.handType == LeftRight.Right)
                recoilTracker.localRotation = Quaternion.Inverse(GetComponent<HandPoser>().rightHandAttach.localRotation);
            else
                recoilTracker.localRotation = Quaternion.Inverse(GetComponent<HandPoser>().leftHandAttach.localRotation);

            startingRotation = recoilTracker.localRotation;
        }

        private Vector3 endOfRecoilPosition;
        private Quaternion endOfRecoilRotation;

        private float timer = 0;
        private bool isRecoiling;
        private Vector3 controllerToAttachDelta;

        private void StartRecoil()
        {
            recoilTracker.localRotation = startingRotation;
            recoilTracker.localPosition = Vector3.zero;
            timer = 0;
            controllerToAttachDelta = transform.position - recoilTracker.transform.position;
            isRecoiling = true;
        }

        private void RecoilUpdate()
        {
            if (!isRecoiling) return;

            if (timer < recoilTime / 2)
            {
                if (Math.Abs(recoilAmount) > .001f)
                {
                    recoilTracker.position += (transform.forward * recoilAmount * Time.deltaTime);
                    transform.position = recoilTracker.position + controllerToAttachDelta;
                }

                if (Math.Abs(recoilRotation) > .001f)
                {
                    recoilTracker.Rotate(Vector3.right, -recoilRotation * Time.deltaTime, Space.Self);
                    transform.rotation = recoilTracker.rotation;
                }

                endOfRecoilPosition = recoilTracker.localPosition;
                endOfRecoilRotation = recoilTracker.localRotation;
            }
            else
            {
                float timerRemappedPercentage = Remap(timer, recoilTime / 2, recoilTime, 0, 1);
                var newPosition = Vector3.Lerp(endOfRecoilPosition, Vector3.zero, timerRemappedPercentage);
                var newRotation = Quaternion.Lerp(endOfRecoilRotation, startingRotation, timerRemappedPercentage);

                recoilTracker.localPosition = newPosition;
                recoilTracker.localRotation = newRotation;
                transform.rotation = recoilTracker.rotation;

                transform.position = recoilTracker.position + controllerToAttachDelta;
                transform.rotation = recoilTracker.rotation;
            }

            
            if (timer > recoilTime)
                isRecoiling = false;
        }
        
    
        private float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}

  // private IEnumerator Recoil()
        // {
        //     recoilTracker.localRotation = startingRotation;
        //     recoilTracker.localPosition = Vector3.zero;
        //
        //     float timer = 0;
        //
        //     var controllerToAttachDelta = transform.position - recoilTracker.transform.position;
        //
        //     while (timer < recoilTime)
        //     {
        //         yield return new WaitForFixedUpdate();
        //
        //         if (timer < recoilTime / 2)
        //         {
        //             if (Math.Abs(recoilAmount) > .001f)
        //             {
        //                 recoilTracker.position += (transform.forward * recoilAmount * Time.deltaTime);
        //                 transform.position = recoilTracker.position + controllerToAttachDelta;
        //             }
        //
        //             if (Math.Abs(recoilRotation) > .001f)
        //             {
        //                 recoilTracker.Rotate(Vector3.right, -recoilRotation * Time.deltaTime, Space.Self);
        //                 transform.rotation = recoilTracker.rotation;
        //             }
        //
        //             endOfRecoilPosition = recoilTracker.localPosition;
        //             endOfRecoilRotation = recoilTracker.localRotation;
        //         }
        //         else
        //         {
        //             float timerRemappedPercentage = Remap(timer, recoilTime / 2, recoilTime, 0, 1);
        //             var newPosition = Vector3.Lerp(endOfRecoilPosition, Vector3.zero, timerRemappedPercentage);
        //             var newRotation = Quaternion.Lerp(endOfRecoilRotation, startingRotation, timerRemappedPercentage);
        //
        //             recoilTracker.localPosition = newPosition;
        //             recoilTracker.localRotation = newRotation;
        //             transform.rotation = recoilTracker.rotation;
        //
        //             transform.position = recoilTracker.position + controllerToAttachDelta;
        //             transform.rotation = recoilTracker.rotation;
        //         }
        //
        //         timer += Time.deltaTime;
        //     }
        // }