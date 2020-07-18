using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class IgnorePlayerColliders : MonoBehaviour
{
    private Collider[] mainColliders;
    private List<Collider> collidersToIgnore;

    void Start()
    {
        mainColliders = GetComponentsInChildren<Collider>(true);
        var playerCollider = FindObjectOfType<XRRig>().GetComponent<CharacterController>();
        foreach (var c in mainColliders)
        {
            Physics.IgnoreCollision(c, playerCollider);

        }
    }
}