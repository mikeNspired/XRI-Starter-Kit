using System;
using System.Collections;
using MikeNspired.UnityXRHandPoser;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[ExecuteInEditMode]
public class Knob : MonoBehaviour
{
    [SerializeField] protected XRGrabInteractable xrGrabInteractable = null;
    [SerializeField] protected Transform rotationPoint = null;
    [SerializeField] protected bool returnToStartOnRelease = true;
    [SerializeField] protected float startingAngle = 0, minAngle = 0, maxAngle = 90, returnSpeed = 1, currentAngle;

    public Transform hand;
    public TextMeshProUGUI text;

    public UnityEventFloat OnSingleValuesChanged;
    public UnityEventFloat OnMinAngle, OnMaxAngle;

    public float StartingAngle => startingAngle;
    public float MinAngle => minAngle;
    public float MaxAngle => maxAngle;
    public float CurrentAngle => currentAngle;
    private void Start()
    {
        OnValidate();
        xrGrabInteractable.onSelectEnter.AddListener(OnGrab);
        xrGrabInteractable.onSelectExit.AddListener((x) => hand = null);
        xrGrabInteractable.onSelectExit.AddListener((x) => StartCoroutine(ReturnToStartingAngle()));
    }

    private void OnValidate()
    {
        if (!xrGrabInteractable)
            xrGrabInteractable = GetComponent<XRGrabInteractable>();

        SetStartPosition();
    }

    private void SetStartPosition()
    {
        currentAngle = StartingAngle;
        rotationPoint.localEulerAngles = new Vector3(0, StartingAngle, 0);
    }

    private void OnGrab(XRBaseInteractor interactor)
    {
        StopAllCoroutines();
        hand = hand.transform;
    }

    private void Update()
    {
        if (!hand) return;
        UpdateRotation();
        InvokeEvents();
    }

    private Vector3 oldDir;

    private void UpdateRotation()
    {
        currentAngle = CurrentAngle + Vector3.SignedAngle(oldDir, transform.InverseTransformDirection(hand.up), -Vector3.forward);
        currentAngle = Mathf.Clamp(CurrentAngle, MinAngle, MaxAngle);
        rotationPoint.localEulerAngles = new Vector3(0, CurrentAngle, 0);

        oldDir = transform.InverseTransformDirection(hand.up);
    }

    private void InvokeEvents()
    {
        text.text = CurrentAngle.ToString();
        OnSingleValuesChanged.Invoke(CurrentAngle);
        if (Math.Abs(CurrentAngle - MinAngle) < .01f) OnMinAngle.Invoke(CurrentAngle);
        if (Math.Abs(CurrentAngle - MaxAngle) < .01f) OnMaxAngle.Invoke(CurrentAngle);
    }

    private IEnumerator ReturnToStartingAngle()
    {
        if (!returnToStartOnRelease) yield break;

        Vector3 startRotation = rotationPoint.localEulerAngles;
        Vector3 goalRotation = new Vector3(0, StartingAngle, 0);
        while (rotationPoint.localEulerAngles != Vector3.zero)
        {
            rotationPoint.localEulerAngles = new Vector3(0, CurrentAngle, 0);

            rotationPoint.localEulerAngles = Vector3.Lerp(startRotation, goalRotation, Time.deltaTime * returnSpeed);

            OnSingleValuesChanged.Invoke(-CurrentAngle);
            yield return null;
        }

        // currentVector = Vector2.zero;
        // rotationPoint.localEulerAngles = Vector3.zero;
        // OnValueChanged.Invoke(currentVector);
    }
}

