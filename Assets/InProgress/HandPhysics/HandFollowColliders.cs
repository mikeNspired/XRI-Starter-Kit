using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandFollowColliders : MonoBehaviour
{
    [SerializeField] private Transform collidersRoot;
    [SerializeField] private float lerpSpeed = .1f;
    
    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, collidersRoot.position, lerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, collidersRoot.rotation, lerpSpeed * Time.deltaTime);
    }
}