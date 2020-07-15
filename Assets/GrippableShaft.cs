using System;
using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class GrippableShaft : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable Interactable = null;

    [SerializeField] private Transform LeftHandGrip = null, RightHandGrip = null;

    [SerializeField] private bool rotateToFollowHand = true;
    
    [SerializeField] private float maxHeight;
    [SerializeField] private float minHeight;

    private Transform LeftHand;
    private Transform RightHand;
    private bool leftFollow = true;
    private bool rightFollow = true;


    private void Start()
    {
        OnValidate();
        Interactable.onSelectEnter.AddListener(controller => EnableFollowOnHand(controller, false));
        Interactable.onSelectExit.AddListener(controller => EnableFollowOnHand(controller, true));
    }

    private void OnValidate()
    {
        if (!Interactable)
            Interactable = GetComponentInParent<XRGrabInteractable>();
    }

    private void Update()
    {
        if (LeftHand && leftFollow) MoveGripPosition(LeftHandGrip, LeftHand);
        if (RightHand && rightFollow) MoveGripPosition(RightHandGrip, RightHand);
    }

    private void MoveGripPosition(Transform grip, Transform hand)
    {
        Vector3 newPosition = Vector3.Project((hand.position - transform.position), transform.up);

        newPosition += transform.position;

        if (!CheckIfPositionInHeightConstraints(newPosition)) return;

        grip.position = newPosition;
        if (rotateToFollowHand)
            grip.rotation = Quaternion.LookRotation(-((hand.position) - grip.transform.position), transform.up);
    }

    private bool CheckIfPositionInHeightConstraints(Vector3 newPosition)
    {
        return newPosition.y >= (transform.position - transform.up * minHeight).y
               && newPosition.y <= (transform.position + transform.up * maxHeight).y;
    }

    private void OnDrawGizmosSelected()
    {
        var localPosition = transform.localPosition;

        Gizmos.matrix = transform.parent.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(localPosition - Vector3.up * minHeight, localPosition + Vector3.up * maxHeight);
        Gizmos.DrawWireSphere(localPosition - Vector3.up * minHeight, .005f);
        Gizmos.DrawWireSphere(localPosition + Vector3.up * maxHeight, .005f);
    }


    private void OnTriggerEnter(Collider other)
    {
        HandReference hand = other.GetComponent<HandReference>();
        if (!hand) return;

        if (hand.hand.handType == LeftRight.Left)
            LeftHand = other.transform;
        else
            RightHand = other.transform;
    }

    private void EnableFollowOnHand(XRBaseInteractor hand, bool state)
    {
        if (hand.GetComponent<XRController>().controllerNode == XRNode.LeftHand)
            leftFollow = state;
        else
            rightFollow = state;
    }


    private void OnTriggerExit(Collider other)
    {
        HandReference hand = other.GetComponent<HandReference>();
        if (!hand) return;

        if (hand.hand.handType == LeftRight.Left)
        {
            LeftHand = null;
            leftFollow = true;
        }
        else
        {
            RightHand = null;
            rightFollow = true;
        }
    }
}