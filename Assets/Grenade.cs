using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Grenade : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable interactable;
    [SerializeField] private GameObject Explosion;
    [SerializeField] private float detonationTime;

    // Start is called before the first frame update
    void Start()
    {
        interactable = GetComponent<XRGrabInteractable>();
        interactable.onActivate.AddListener(Activate);
    }

    private void OnValidate()
    {
        if (!interactable)
            interactable = GetComponent<XRGrabInteractable>();
    }

    private void Activate(XRBaseInteractor interactor)
    {
        Invoke(nameof(TriggerGrenade), detonationTime);
    }

    private void TriggerGrenade()
    {
        Explosion.SetActive(true);
        Explosion.transform.parent = null;
        Destroy(gameObject, 3);
    }
}