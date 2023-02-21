using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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
        otherHand = currentHand.otherHand;
        TrySpawn();
    }

    public void TrySpawn()
    {
        if (!enabled) return;
        if (!currentHand || !otherHand) return;
        if (otherHand.xrDirectInteractor.hasSelection) return;
        var spawnedObject = Instantiate(prefabToSpawn);
        otherHand.xrDirectInteractor.interactionManager.SelectEnter(otherHand.xrDirectInteractor, spawnedObject);
    }

    private void OnValidate()
    {
        if (!_xrBaseInteractable) _xrBaseInteractable = GetComponent<XRBaseInteractable>();
    }
}
