using System;
using System.Collections;
using MikeNspired.UnityXRHandPoser;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[ExecuteInEditMode]
public class Wheel : MonoBehaviour
{
    [SerializeField] protected XRGrabInteractable xrGrabInteractable = null;
    [SerializeField] protected Transform rotationPoint = null;
    [SerializeField] protected bool returnToStartOnRelease = true;
    [SerializeField] protected float startingAngle = 0, minAngle = -60, maxAngle = 60, returnSpeed = 1, currentAngle;

    public float followSpeed = 10;

    public Transform hand;
    public TextMeshProUGUI text;
    public float StartingAngle => startingAngle;
    public float MinAngle => minAngle;
    public float MaxAngle => maxAngle;
    public float CurrentAngle => currentAngle;

    public Transform groundedPosition;
    public Transform handFollow;
    private Vector3 lastPoint;
    
    public UnityEventFloat OnSingleValuesChanged;
    public UnityEventFloat OnMinAngle, OnMaxAngle;
    private void Start()
    {
        OnValidate();
        xrGrabInteractable.onSelectEntered.AddListener(OnGrab);
        xrGrabInteractable.onSelectExited.AddListener((x) => hand = null);
        xrGrabInteractable.onSelectExited.AddListener((x) => StartCoroutine(ReturnToStartingAngle()));
    }

    private void OnValidate()
    {
        if (!xrGrabInteractable)
            xrGrabInteractable = GetComponent<XRGrabInteractable>();

        SetStartPosition();
        GetStartingPositionForRotationCalculation();
    }

    private void GetStartingPositionForRotationCalculation()
    {
        lastPoint = rotationPoint.parent.transform.InverseTransformDirection(rotationPoint.forward);
        lastPoint.y = 0;
    }
    
    private void SetStartPosition()
    {
        currentAngle = StartingAngle;
        rotationPoint.localEulerAngles = new Vector3(0, currentAngle % 360, 0);
    }

    private void OnGrab(XRBaseInteractor hand)
    {
        StopAllCoroutines();

        this.hand = hand.transform;
    }

    private Quaternion prevAngle;

    private void Update()
    {
        if (!hand) return;

        handFollow.position = Vector3.Lerp(handFollow.position, hand.position, Time.deltaTime * followSpeed);
        //Projection
        Vector3 positionToProject = handFollow.position;
        Vector3 v = positionToProject - transform.position;
        Vector3 projection = Vector3.ProjectOnPlane(v, transform.up);
        Vector3 projectedPoint = transform.position + projection;

        groundedPosition.position = projectedPoint;
        rotationPoint.LookAt(groundedPosition);

        CalculateTotalAngle();

        if (currentAngle > maxAngle)
        {
            rotationPoint.rotation = prevAngle;
            currentAngle = maxAngle;
        }
        else if (currentAngle < minAngle)
        {
            rotationPoint.rotation = prevAngle;
            currentAngle = minAngle;
        }

        rotationPoint.localEulerAngles = new Vector3(0, currentAngle % 360, 0);

        prevAngle = rotationPoint.rotation;
    }

    //Credit to Talzor
    private void CalculateTotalAngle()
    {
        Vector3 facing = rotationPoint.parent.transform.InverseTransformDirection(rotationPoint.forward);

        facing.y = 0;

        float angle = Vector3.Angle(lastPoint, facing);
        if (Vector3.Cross(lastPoint, facing).y < 0)
            angle *= -1;

        currentAngle += angle;
        lastPoint = facing;
    }

    private void InvokeEvents()
    {
        text.text = currentAngle.ToString();
        OnSingleValuesChanged.Invoke(currentAngle);
    }

    private IEnumerator ReturnToStartingAngle()
    {
        if (!returnToStartOnRelease) yield break;

        float startingAngle = currentAngle;
        Vector3 goalRotation = new Vector3(0, startingAngle, 0);
        while (Math.Abs(currentAngle) > .01f)
        {
            currentAngle = Mathf.Lerp(startingAngle, StartingAngle, Time.deltaTime * returnSpeed);
            rotationPoint.localEulerAngles = new Vector3(0, currentAngle % 360, 0);
            InvokeEvents();
            yield return null;
        }
    }
}