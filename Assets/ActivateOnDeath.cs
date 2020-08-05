using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateOnDeath : MonoBehaviour
{
    [SerializeField] private Health health = null;
    [SerializeField] private GameObject[] objectsToActivate = null;
    [SerializeField] private GameObject[] objectsToDectivate = null;
    [SerializeField] private Behaviour[] componentToDeactivate = null;
    [SerializeField] private Behaviour[] componentToActivate = null;


    void Start()
    {
        OnValidate();
        health.onDie += SetActive;
        health.onDie += DeActivate;
    }

    private void DeActivate(GameObject arg0)
    {
        foreach (var gameObject in objectsToDectivate)
        {
            gameObject.SetActive(false);
        }
        foreach (var comp in componentToDeactivate)
        {
            comp.enabled = false;
        }
    }

    private void SetActive(GameObject arg0)
    {
        foreach (var gameObject in objectsToActivate)
        {
            gameObject.SetActive(true);
        }
        foreach (var comp in componentToActivate)
        {
            comp.enabled = true;
        }
    }
    

    private void OnValidate()
    {
        if (!health)
            health = GetComponent<Health>();
    }
}