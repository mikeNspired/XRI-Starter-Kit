using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TestScript : MonoBehaviour
{

    public Rigidbody rb;

    public XRGrabInteractable interactable;
    // Start is called before the first frame update
    void Start()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (!rb)
            rb = GetComponent<Rigidbody>();
        if (!interactable)
            interactable = GetComponent<XRGrabInteractable>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
