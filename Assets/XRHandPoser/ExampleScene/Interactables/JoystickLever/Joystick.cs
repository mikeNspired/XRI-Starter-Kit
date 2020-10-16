using System;
using System.Collections;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[ExecuteInEditMode]
public class Joystick : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable xrGrabInteractable = null;
    [SerializeField] private Transform rotationPoint= null;
    [SerializeField] private float maxAngle = 60;
    [SerializeField] private float shaftLength = .2f;
    [SerializeField] private bool returnToStartOnRelease = true;
    [SerializeField] private float returnSpeed = 5;
    [SerializeField] private Vector2 startingPosition = Vector2.zero;
    [SerializeField] private Vector2 returnToPosition = Vector2.zero;
    [SerializeField] private bool xAxis = true, yAxis = true;

    private Transform hand;
    private Vector2 currentVector;
    
    public Vector2 CurrentVector => currentVector;
    public UnityEventVector2 OnValueChanged;
    public UnityEventFloat OnSingleValuesChanged;

    private void Start()
    {
        OnValidate();
        xrGrabInteractable.onSelectEnter.AddListener(OnGrab);
        xrGrabInteractable.onSelectExit.AddListener((x) => hand = null);
        xrGrabInteractable.onSelectExit.AddListener((x) => StartCoroutine(ReturnToZero()));
    }

    private void OnValidate()
    {
        if (!xrGrabInteractable)
            xrGrabInteractable = GetComponent<XRGrabInteractable>();

        SetStartPosition();
    }

    private void SetStartPosition()
    {
        SetPosition(new Vector3(startingPosition.x, 0, startingPosition.y));
    }

    private void OnGrab(XRBaseInteractor hand)
    {
        StopAllCoroutines();
        this.hand = hand.transform;
    }

    private void Update()
    {
        if (!hand) return;
        //Projection
        Vector3 positionToProject = hand.position;
        Vector3 v = positionToProject - transform.position;
        Vector3 projection = Vector3.ProjectOnPlane(v, transform.up);

        Vector3 projectedPoint;
        if (xAxis & yAxis)
            projectedPoint = transform.position + Vector3.ClampMagnitude(projection, 1);
        else
            projectedPoint = transform.position + new Vector3(Mathf.Clamp(projection.x, -1, 1), 0, Mathf.Clamp(projection.z, -1, 1));

        var locRot = transform.InverseTransformPoint(projectedPoint);

       // locRot = Vector3.ClampMagnitude(locRot, shaftLength);

        SetPosition(locRot);
    }

    private void SetPosition(Vector3 locRot)
    {
        float x = Remap(locRot.x, -shaftLength, shaftLength, -1, 1);
        float z = Remap(locRot.z, -shaftLength, shaftLength, -1, 1);

        if (xAxis & yAxis)
            currentVector = Vector2.ClampMagnitude(new Vector2(x, z), 1);
        
        if (!xAxis)
            currentVector = new Vector2(0, Mathf.Clamp(z, -1, 1));
        if (!yAxis) 
                currentVector = new Vector2(Mathf.Clamp(x,-1,1), 0);


        // if (xAxis & yAxis)
        //     currentVector = Vector2.ClampMagnitude(new Vector2(x, z), 1);
        //
        // if (!xAxis)
        //     currentVector = new Vector2(0, Mathf.Clamp(z,-1,1));
        // if (!yAxis) 
        //     currentVector = new Vector2(Mathf.Clamp(x,-1,1), 0);
        rotationPoint.localEulerAngles = new Vector3(currentVector.y * maxAngle, 0, -currentVector.x * maxAngle);

        InvokeEvents(currentVector);
    }

    private void InvokeEvents(Vector2 vector2)
    {
        OnValueChanged.Invoke(vector2);
        if (!xAxis)
            OnSingleValuesChanged.Invoke(vector2.y);
        if (!yAxis)
            OnSingleValuesChanged.Invoke(vector2.x);
    }

    private IEnumerator ReturnToZero()
    {
        if (!returnToStartOnRelease) yield break;

        while (currentVector.magnitude >= .01f)
        {
            currentVector = Vector2.Lerp(currentVector, returnToPosition, Time.deltaTime * returnSpeed);
            rotationPoint.localEulerAngles = new Vector3(currentVector.y * maxAngle, 0, -currentVector.x * maxAngle);
            OnValueChanged.Invoke(currentVector);
            yield return null;
        }

        currentVector = Vector2.zero;
        rotationPoint.localEulerAngles = Vector3.zero;
        OnValueChanged.Invoke(currentVector);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        if (xAxis && yAxis)
            Gizmos.DrawWireSphere(transform.position, shaftLength);
        if (!xAxis && yAxis)
            Gizmos.DrawLine(transform.position - transform.forward * shaftLength, transform.position + transform.forward * shaftLength);
        if (!yAxis && xAxis)
            Gizmos.DrawLine(transform.position - transform.right * shaftLength, transform.position + transform.right * shaftLength);

        Gizmos.DrawLine(transform.position, transform.position + transform.up * shaftLength);
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}

[Serializable]
public class UnityEventVector2 : UnityEvent<Vector2>
{
}