using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilmingCamera : MonoBehaviour
{
    [SerializeField] private float speed = 5;

    private Transform playerCamera;

    // Start is called before the first frame update
    void Start()
    {
        playerCamera = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, playerCamera.position, Time.deltaTime * speed);
        transform.rotation = Quaternion.Lerp(transform.rotation, playerCamera.rotation, Time.deltaTime * speed);
    }
}