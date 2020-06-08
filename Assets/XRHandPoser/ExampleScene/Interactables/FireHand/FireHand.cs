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

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();

            startingParticleOrigin.position = powerParticleSystem.transform.localPosition;
            startingParticleOrigin.rotation = powerParticleSystem.transform.localRotation;

            grabInteractable = GetComponent<XRGrabInteractable>();

            grabInteractable.onSelectEnter.AddListener(call: OnGrab);
            grabInteractable.onSelectExit.AddListener(call: OnRelease);

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
            StartCoroutine(SetDefaultPosition());
        }

        private void OnGrab(XRBaseInteractor interactor)
        {
            StopAllCoroutines();
            itemModel.SetActive(false);
        
            if (!playerHandModel)
            {
                playerHandModel = interactor.GetComponent<HandReference>().hand.GetComponentInChildren<SkinnedMeshRenderer>();
                originalMaterial = playerHandModel.material;
            }


            meshCollider.enabled = false;
            playerHandModel.material = newMaterial;
        }

        private IEnumerator SetDefaultPosition()
        {
            float timer = 0;
            Vector3 startingRotation = transform.eulerAngles;
            Vector3 endingRotation = new Vector3(0,startingRotation.y,0);
            while (timer <= 1 + Time.deltaTime)
            {
                transform.eulerAngles = Vector3.Lerp(startingRotation, endingRotation, timer / 1);
                timer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }
        }
    }
}