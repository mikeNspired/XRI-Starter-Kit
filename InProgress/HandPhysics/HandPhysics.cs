using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPhysics : MonoBehaviour
{
    private bool isColliding = false;
    [SerializeField] private float speed = 10;
    [SerializeField] private Transform interactorRoot;
    [SerializeField] private Rigidbody rigidbody;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (isColliding)
        {
            rigidbody.velocity = Vector3.MoveTowards(rigidbody.velocity, (interactorRoot.transform.position - rigidbody.position), Time.deltaTime * speed);

            // Rotations stack right to left,
            // so first we undo our rotation, then apply the target.
            //Credit = DMGregory
            var delta = interactorRoot.rotation * Quaternion.Inverse(transform.rotation);

            float angle;
            Vector3 axis;
            delta.ToAngleAxis(out angle, out axis);

            // We get an infinite axis in the event that our rotation is already aligned.
            if (float.IsInfinity(axis.x))
                return;

            if (angle > 180f)
                angle -= 360f;

            // Here I drop down to 0.9f times the desired movement,
            // since we'd rather undershoot and ease into the correct angle
            // than overshoot and oscillate around it in the event of errors.
            Vector3 angular = (0.9f * Mathf.Deg2Rad * angle / .1f) * axis.normalized;

            rigidbody.angularVelocity = angular;
        }
        else
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.MovePosition(interactorRoot.position);
            rigidbody.MoveRotation(interactorRoot.rotation);
            
            
        }
    }

    private void OnCollisionStay(Collision other)
    {
        isColliding = true;
        Debug.Log("t " + other.collider);
        

    }
    

    private void OnCollisionExit(Collision collision)
    {
        isColliding = false;
        Debug.Log("FALSE!");
    }
}