using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilmingCamera : MonoBehaviour
{
    [SerializeField] private float lerpSpeed = 5;
    [SerializeField] private Vector3 offSet = Vector3.zero;

    private Transform playerCamera;

    private void Start()
    {
        playerCamera = Camera.main.transform;
    }

    private void Update()
    {
        Vector3 goalPosition = playerCamera.TransformPoint(playerCamera.localPosition + offSet);
        transform.position = Vector3.Lerp(transform.position, goalPosition, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, playerCamera.rotation, Time.deltaTime * lerpSpeed);
    }
}