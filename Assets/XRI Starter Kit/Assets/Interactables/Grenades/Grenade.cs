using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.UnityXRHandPoser
{
    public class Grenade : MonoBehaviour, IDamageable
    {
        [SerializeField] private XRGrabInteractable interactable = null;
        [SerializeField] private GameObject explosionPrefab = null;
        [SerializeField] private AudioSource activationSound = null;
        [SerializeField] private GameObject meshLightActivation = null;
        [SerializeField] private float detonationTime = 3f;
        [SerializeField] private bool startTimerAfterActivation = false;

        private bool canActivate;
        private XRInteractionManager interactionManager;

        private void Awake()
        {
            interactable = GetComponent<XRGrabInteractable>();
            interactionManager = FindFirstObjectByType<XRInteractionManager>();

            interactable.activated.AddListener(TurnOnGrenade);
            interactable.selectExited.AddListener(Activate);

            if (meshLightActivation)
                meshLightActivation.SetActive(false);
        }

        private void TurnOnGrenade(ActivateEventArgs args)
        {
            canActivate = true;
            if (meshLightActivation) meshLightActivation.SetActive(true);
            activationSound?.Play();

            if (startTimerAfterActivation)
                Invoke(nameof(TriggerGrenade), detonationTime);
        }

        private void Activate(SelectExitEventArgs args)
        {
            if (canActivate && !startTimerAfterActivation)
                Invoke(nameof(TriggerGrenade), detonationTime);
        }

        private void TriggerGrenade()
        {
            if (explosionPrefab)
            {
                explosionPrefab.SetActive(true);
                explosionPrefab.transform.parent = null;
                explosionPrefab.transform.localEulerAngles = Vector3.zero;
            }

            if (interactable.firstInteractorSelecting != null)
                interactionManager.SelectExit(interactable.firstInteractorSelecting, interactable);

            StartCoroutine(MoveAndDisable());
        }

        private IEnumerator MoveAndDisable()
        {
            transform.position += Vector3.one * 9999;
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);
            Destroy(gameObject);
        }

        public void TakeDamage(float damage, GameObject damager)
        {
            TriggerGrenade();
        }
    }
}
