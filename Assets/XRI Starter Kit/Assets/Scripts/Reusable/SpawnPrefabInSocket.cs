using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    public class SpawnPrefabInSocket : XRSocketInteractor
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Transform spawnPosition;
        public XRBaseInteractable currentObject;

        protected override void Start()
        {
            base.Start();
            SpawnPrefab();
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            if (!gameObject.scene.isLoaded) return;
            base.OnSelectExited(args);
            SpawnPrefab();
        }

        private void SpawnPrefab()
        {
            currentObject = Instantiate(prefab, spawnPosition.position, spawnPosition.rotation)
                .GetComponent<XRBaseInteractable>();
            interactionManager.SelectEnter(this, (IXRSelectInteractable)currentObject);
        }
    }
}