using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class ReleaseHandAtDistance : MonoBehaviour
    {
        [SerializeField] private XRBaseInteractable baseInteractable;
        [SerializeField] private float distance = .2f;
        [SerializeField] public bool debugSpheresEnabled;

        private IXRSelectInteractor interactor;
        private IXRSelectInteractable interactable;
        private XRInteractionManager interactionManager;

        private void Start()
        {
            OnValidate();
            LogMessages();
            interactable = baseInteractable.GetComponent<IXRSelectInteractable>();
            interactable.selectEntered.AddListener(x => interactor = x.interactorObject);
            interactable.selectExited.AddListener(x => interactor = null);
        }

        private void OnValidate()
        {
            if (!baseInteractable)
                baseInteractable = GetComponentInParent<XRBaseInteractable>();
            if (!interactionManager)
                interactionManager = FindObjectOfType<XRInteractionManager>();
        }

        private void Update()
        {
            if (interactor == null) return;
            if (Vector3.Distance(interactable.transform.position, interactor.transform.position) < distance) return;
            ReleaseItemFromHand();
        }

        private void ReleaseItemFromHand()
        {
            interactionManager.SelectExit(interactor, interactable);
            interactor = null;
        }

        private void LogMessages()
        {
            if (interactable == null)
            {
                Debug.LogWarning(this + " missing interactable on : " + gameObject);
                enabled = false;
            }

            if (interactionManager == null)
            {
                Debug.LogWarning(this + " No XRInteractionManager found in scene: " + gameObject);
                enabled = false;
            }
        }


        private void OnDrawGizmosSelected()
        {
            if (debugSpheresEnabled) Gizmos.DrawWireSphere(transform.position, distance);
        }
    }
}