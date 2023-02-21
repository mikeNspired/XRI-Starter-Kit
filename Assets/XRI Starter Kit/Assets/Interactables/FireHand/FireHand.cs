// Author MikeNspired. 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class FireHand : MonoBehaviour
    {
        [SerializeField] private GameObject itemModel = null;
        [SerializeField] private Material newMaterial = null;
        [SerializeField] private ParticleSystem powerParticleSystem = null;
        [SerializeField] private Collider meshCollider = null;
        private Material originalMaterial;
        private SkinnedMeshRenderer playerHandModel;
        private XRGrabInteractable grabInteractable = null;
        private TransformStruct startingParticleOrigin; //Still need to set
        private AudioSource audioSource;
        private Vector3 startingPosition;

        private void Awake()
        {
            startingPosition = transform.position;
            audioSource = GetComponent<AudioSource>();

            startingParticleOrigin.position = powerParticleSystem.transform.localPosition;
            startingParticleOrigin.rotation = powerParticleSystem.transform.localRotation;

            grabInteractable = GetComponent<XRGrabInteractable>();

            grabInteractable.onSelectEntered.AddListener(call: OnGrab);
            grabInteractable.onSelectExited.AddListener(call: OnRelease);

            grabInteractable.onActivate.AddListener(call: StartPower);
            grabInteractable.onDeactivate.AddListener(call: StopPower);
        }

        private void StartPower(XRBaseInteractor interactor)
        {
            powerParticleSystem.Play();
            audioSource.Play();
        }

        private void StopPower(XRBaseInteractor interactor)
        {
            powerParticleSystem.Stop();
            audioSource.Stop();
        }

        private void OnRelease(XRBaseInteractor interactor)
        {
            itemModel.SetActive(true);
            playerHandModel.material = originalMaterial;
            meshCollider.enabled = true;
            startingParticleOrigin.position = powerParticleSystem.transform.localPosition;
            startingParticleOrigin.rotation = powerParticleSystem.transform.localRotation;
            powerParticleSystem.Stop();
            audioSource.Stop();
            if (gameObject.activeSelf)
                StartCoroutine(SetDefaultPosition());
        }

        public float animationTime;

        private void OnGrab(XRBaseInteractor interactor)
        {
            StopAllCoroutines();

            itemModel.SetActive(false);

            if (!playerHandModel)
            {
                playerHandModel = interactor.GetComponentInParent<HandReference>().Hand.GetComponentInChildren<SkinnedMeshRenderer>();
                originalMaterial = playerHandModel.material;
            }
            
            meshCollider.enabled = false;
            playerHandModel.material = newMaterial;
        }

        private IEnumerator SetDefaultPosition()
        {
            float timer = 0;
            Quaternion startingRotation = transform.rotation;
            Vector3 endingRotation = new Vector3(0, startingRotation.y, 0);
            Vector3 currentPosition = transform.position;
            while (timer <= animationTime + Time.deltaTime)
            {
                transform.rotation = Quaternion.Lerp(startingRotation, Quaternion.Euler(endingRotation), timer / animationTime);
                transform.position = Vector3.Lerp(currentPosition, startingPosition, timer / animationTime);
                timer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }
        }
    }
}