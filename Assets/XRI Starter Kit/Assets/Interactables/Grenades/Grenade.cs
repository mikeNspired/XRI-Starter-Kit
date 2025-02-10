using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public class Grenade : MonoBehaviour, IDamageable
    {
        [SerializeField] private XRGrabInteractable interactable;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private AudioSource activationSound;
        [SerializeField] private GameObject meshLightActivation;
        [SerializeField] private float detonationTime = 3f;
        [SerializeField] private bool startTimerAfterActivation;

        private bool canActivate;
        private XRInteractionManager interactionManager;

        private void Awake()
        {
            if (meshLightActivation)
                meshLightActivation.SetActive(false);
            
            interactable = GetComponent<XRGrabInteractable>();
            interactionManager = FindFirstObjectByType<XRInteractionManager>();

            if (!interactable) return;
            interactable.activated.AddListener(TurnOnGrenade);
            interactable.selectExited.AddListener(Activate);
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

            if (interactable?.firstInteractorSelecting != null)
                interactionManager.SelectExit(interactable.firstInteractorSelecting, interactable);
            
            Destroy(gameObject);
        }

        public void TakeDamage(float damage, GameObject damager) => TriggerGrenade();
    }
}
