using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class Grenade : MonoBehaviour, IDamageable
    {
        [SerializeField] private XRGrabInteractable interactable = null;
        [SerializeField] private GameObject Explosion = null;
        [SerializeField] private AudioSource activationSound = null;
        [SerializeField] private GameObject meshLightActivation = null;
        [SerializeField] private float detonationTime = 3;
        [SerializeField] private bool startTimerAfterActivation = false;

        private bool canActivate;
        private XRInteractionManager interactionManager;

        // Start is called before the first frame update
        void Start()
        {
            OnValidate();
            interactable = GetComponent<XRGrabInteractable>();
            interactable.onActivate.AddListener(TurnOnGrenade);
            interactable.onSelectExited.AddListener(Activate);
            if (meshLightActivation)
                meshLightActivation.SetActive(false);
        }

        private void OnValidate()
        {
            if (!interactable)
                interactable = GetComponent<XRGrabInteractable>();
            if (!interactionManager)
                interactionManager = FindObjectOfType<XRInteractionManager>();
        }

        private void TurnOnGrenade(XRBaseInteractor interactor)
        {
            canActivate = true;
            meshLightActivation.SetActive(true);
            activationSound.Play();

            if (startTimerAfterActivation)
                Invoke(nameof(TriggerGrenade), detonationTime);
        }

        private void Activate(XRBaseInteractor interactor)
        {
            if (canActivate && !startTimerAfterActivation)
                Invoke(nameof(TriggerGrenade), detonationTime);
        }

        private void TriggerGrenade()
        {
            Explosion.SetActive(true);
            Explosion.transform.parent = null;
            Explosion.transform.localEulerAngles = Vector3.zero;

            if (interactable.selectingInteractor)
                interactionManager.SelectExit(interactable.selectingInteractor, interactable);

            StartCoroutine(MoveAndDisableCollider());
            //gameObject.SetActive(false);
            // Destroy(gameObject,1);
        }

        private IEnumerator MoveAndDisableCollider()
        {
            //objectToMove.GetComponent<CollidersSetToTrigger>()?.SetAllToTrigger();

            transform.position += Vector3.one * 9999;
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);
            //Lets physics respond to collider disappearing before disabling object physics update needs to run twice
            Destroy(gameObject);
        }

        public void TakeDamage(float damage, GameObject damager)
        {
            TriggerGrenade();
        }
    }
}