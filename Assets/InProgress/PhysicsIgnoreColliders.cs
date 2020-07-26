using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PhysicsIgnoreColliders : MonoBehaviour
{
    [SerializeField] private List<Collider> mainColliders = null;
    [SerializeField]  private List<Collider> collidersToIgnore = null;

    protected void Awake()
    {
        IgnoreCollisions(mainColliders,collidersToIgnore);
    }


    public void IgnoreCollisions(List<Collider> colliders, List<Collider> collidersToIgnore)
    {
        foreach (var col in colliders)
        {
            foreach (var ignoreCollider in collidersToIgnore)
            {
                Physics.IgnoreCollision(col, ignoreCollider);
            }
        } 

    }
}

// public class XRInteractablePhysicsIgnoreColliders : PhysicsIgnoreColliders
// {
//     private List<Collider> mainColliders;
//     private List<Collider> collidersToIgnore;
//
//     void Start()
//     {
//         mainColliders = GetComponent<XRBaseInteractable>().colliders;
//         IgnoreCollisions(mainColliders,collidersToIgnore);
//     }
// }