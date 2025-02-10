// Author MikeNspired.

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public class FireHand : MonoBehaviour
    {
        [SerializeField] private GameObject itemModel = null;
        [SerializeField] private Material newMaterial = null;
        [SerializeField] private ParticleSystem powerParticleSystem = null;
        [SerializeField] private Collider meshCollider = null;
        [SerializeField] private ParticleCollisionEvents particleCollisionEvents = null;
        [SerializeField] private float damageAmount = 5;
        private Material originalMaterial;
        private SkinnedMeshRenderer playerHandModel;
        private XRGrabInteractable grabInteractable = null;
        private AudioSource audioSource;
        private Vector3 startingPosition;
        
        public float animationTime;

        private void Awake()
        {
            startingPosition = transform.position;
            audioSource = GetComponent<AudioSource>();

  

            grabInteractable = GetComponent<XRGrabInteractable>();

            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
            grabInteractable.activated.AddListener(StartPower);
            grabInteractable.deactivated.AddListener(StopPower);

            particleCollisionEvents.OnParticleCollisionEvent += ParticleCollided;
        }

        private void ParticleCollided(GameObject arg1, Vector3 arg2)
        {
            // Check if the object itself implements IDamageable
            if (arg1.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(damageAmount, gameObject);
            // If the object itself doesn't implement IDamageable, check its parent
            else if (arg1.GetComponentInParent<IDamageable>() is { } parentDamageable) 
                parentDamageable.TakeDamage(damageAmount,gameObject);
        }

        private void StartPower(ActivateEventArgs args)
        {
            powerParticleSystem.Play();
            audioSource.Play();
        }

        private void StopPower(DeactivateEventArgs args)
        {
            powerParticleSystem.Stop();
            audioSource.Stop();
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            itemModel.SetActive(true);
            if (playerHandModel != null)
                playerHandModel.material = originalMaterial;

            meshCollider.enabled = true;
            powerParticleSystem.Stop();
            audioSource.Stop();

            if (gameObject.activeSelf)
                StartCoroutine(SetDefaultPosition());
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            StopAllCoroutines();

            itemModel.SetActive(false);

            if (playerHandModel == null)
            {
                var handReference = args.interactorObject.transform.GetComponentInParent<HandReference>();
                if (handReference != null)
                {
                    playerHandModel = handReference.Hand.GetComponentInChildren<SkinnedMeshRenderer>();
                    originalMaterial = playerHandModel.material;
                }
            }

            meshCollider.enabled = false;
            if (playerHandModel != null)
                playerHandModel.material = newMaterial;
        }

        private IEnumerator SetDefaultPosition()
        {
            float timer = 0f;
            Quaternion startingRotation = transform.rotation;
            Vector3 endingRotation = new Vector3(0, startingRotation.eulerAngles.y, 0);
            Vector3 currentPosition = transform.position;

            while (timer <= animationTime + Time.deltaTime)
            {
                transform.rotation = Quaternion.Lerp(startingRotation, Quaternion.Euler(endingRotation), timer / animationTime);
                transform.position = Vector3.Lerp(currentPosition, startingPosition, timer / animationTime);
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }
    
    
}
