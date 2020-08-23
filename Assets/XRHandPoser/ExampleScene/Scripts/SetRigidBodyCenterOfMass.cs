using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SetRigidBodyCenterOfMass : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable xrGrabInteractable = null;
    [SerializeField] private Rigidbody rigidBody = null;
    [SerializeField] private Vector3 newCenterOfMass = Vector3.zero;

    private void Start()
    {
        OnValidate();
        xrGrabInteractable.onSelectEnter.AddListener((X) => rigidBody.centerOfMass = newCenterOfMass);
        xrGrabInteractable.onSelectExit.AddListener((X) => rigidBody.ResetCenterOfMass());
    }

    private void OnValidate()
    {
        if (!xrGrabInteractable)
            xrGrabInteractable = GetComponent<XRGrabInteractable>();
        if (!rigidBody)
            rigidBody = GetComponent<Rigidbody>();
    }
}