using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SpawnPrefabInSocket : XRSocketInteractor
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform spawnPosition;
    public XRBaseInteractable currentObject;

    protected override void Awake()
    {
        base.Awake();
        SpawnPrefab();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        if(!gameObject.scene.isLoaded) return;
        base.OnSelectExited(args);
        SpawnPrefab();
    }

    void SpawnPrefab()
    {
        currentObject = Instantiate(prefab, spawnPosition.position, spawnPosition.rotation).GetComponent<XRBaseInteractable>();
        interactionManager.SelectEnter(this, (IXRSelectInteractable)currentObject);
    }
}