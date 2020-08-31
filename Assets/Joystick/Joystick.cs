using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[ExecuteInEditMode]
public class Joystick : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable xrGrabInteractable;
    [SerializeField] private Transform rotationPoint;
    [SerializeField] private float maxAngle = 60;
    [SerializeField] private float shaftLength = .2f;
    [SerializeField] private float returnSpeed = 1;

    private Transform hand;
    private Vector2 currentVector;
    
    public Vector2 CurrentVector => currentVector;
    public UnityEventVector2 OnValueChanged;

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
        Vector3 projectedPoint = transform.position + Vector3.ClampMagnitude(projection, 1);

        var locRot = transform.InverseTransformPoint(projectedPoint);

        locRot = Vector3.ClampMagnitude(locRot, shaftLength);

        float x = Remap(locRot.x, -shaftLength, shaftLength, -1, 1);
        float z = Remap(locRot.z, -shaftLength, shaftLength, -1, 1);

        currentVector = Vector2.ClampMagnitude(new Vector2(x, z), 1);
        rotationPoint.localEulerAngles = new Vector3(currentVector.y * maxAngle, 0, -currentVector.x * maxAngle);

        OnValueChanged.Invoke(currentVector);
    }

    private IEnumerator ReturnToZero()
    {
        while (currentVector.magnitude >= .01f)
        {
            currentVector = Vector2.Lerp(currentVector, Vector2.zero, Time.deltaTime * returnSpeed);
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
        Gizmos.DrawWireSphere(transform.position, shaftLength);
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