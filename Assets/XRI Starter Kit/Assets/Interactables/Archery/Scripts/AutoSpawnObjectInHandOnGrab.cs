using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public class AutoSpawnObjectInHandOnGrab : MonoBehaviour
    {
        [SerializeField] private XRBaseInteractable _xrBaseInteractable;
        [SerializeField] private XRBaseInteractable prefabToSpawn;

        private HandReference currentHand, otherHand;

        private void Start()
        {
            _xrBaseInteractable.selectEntered.AddListener(OnGrab);
            _xrBaseInteractable.selectExited.AddListener(OnRelease);
        }

        private void OnRelease(SelectExitEventArgs arg0)
        {
            currentHand = null;
            otherHand = null;
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            currentHand = args.interactorObject.transform.GetComponentInParent<HandReference>();
            otherHand = currentHand.OtherHand;
            TrySpawn();
        }

        public virtual void TrySpawn()
        {
            if (!enabled) return;
            if (!currentHand || !otherHand) return;
            if (otherHand.NearFarInteractor.hasSelection) return;
            var spawnedObject = Instantiate(prefabToSpawn);
            otherHand.NearFarInteractor.interactionManager.SelectEnter(otherHand.NearFarInteractor,
                (IXRSelectInteractable)spawnedObject);
        }

        private void OnValidate()
        {
            if (!_xrBaseInteractable) _xrBaseInteractable = GetComponent<XRBaseInteractable>();
        }
    }
}