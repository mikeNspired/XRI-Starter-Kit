using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Grenade : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable interactable = null;
    [SerializeField] private GameObject Explosion = null;
    [SerializeField] private AudioSource activationSound = null;
    [SerializeField] private GameObject meshLightActivation = null;
    [SerializeField] private float detonationTime = 3;
    public bool canActivate;

    // Start is called before the first frame update
    void Start()
    {
        OnValidate();
        interactable = GetComponent<XRGrabInteractable>();
        interactable.onActivate.AddListener(TurnOnGrenade);
        interactable.onSelectExit.AddListener(Activate);
        if (meshLightActivation)
            meshLightActivation.SetActive(false);
    }

    private void OnValidate()
    {
        if (!interactable)
            interactable = GetComponent<XRGrabInteractable>();
    }

    private void TurnOnGrenade(XRBaseInteractor interactor)
    {
        canActivate = true;
        meshLightActivation.SetActive(true);
        activationSound.Play();
    }

    private void Activate(XRBaseInteractor interactor)
    {
        if (canActivate)
            Invoke(nameof(TriggerGrenade), detonationTime);
    }

    private void TriggerGrenade()
    {
        Explosion.SetActive(true);
        Explosion.transform.parent = null;
        Explosion.transform.localEulerAngles = Vector3.zero;
        Destroy(gameObject);
    }
}