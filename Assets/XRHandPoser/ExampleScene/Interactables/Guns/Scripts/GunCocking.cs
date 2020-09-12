using System;
using System.Collections;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

/// <summary>
/// Custom interactable that can be dragged along an axis. Can either be continuous or snap to integer steps.
/// </summary>
public class GunCocking : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable xrGrabInteractable = null;
    [SerializeField] private XRGrabInteractable mainGrabInteractable = null;
    [SerializeField] private ProjectileWeapon projectileWeapon = null;
    [SerializeField] private Vector3 LocalAxis = -Vector3.forward;
    [SerializeField] private float AxisLength = .1f;
    [SerializeField] private float ReturnSpeed = 1;
    [SerializeField] private AudioRandomize pullBackAudio = null;
    [SerializeField] private AudioRandomize releaseAudio = null;

    private XRBaseInteractor currentHand;
    private XRInteractionManager interactionManager;
    private XRBaseInteractor grabbingInteractor;
    private Transform originalParent;
    private Vector3 grabbedOffset, endPoint, startPoint;
    private float currentDistance;
    private bool hasReachedEnd, isSelected;
    public UnityEvent GunCockedEvent;
    

    private void Start()
    {
        OnValidate();

        xrGrabInteractable.onSelectEnter.AddListener(OnGrabbed);
        xrGrabInteractable.onSelectExit.AddListener(OnRelease);
        mainGrabInteractable.onSelectExit.AddListener(ReleaseIfMainHandReleased);

        originalParent = transform.parent;
        LocalAxis.Normalize();

        //Length can't be negative, a negative length just mean an inverted axis, so fix that
        if (AxisLength < 0)
        {
            LocalAxis *= -1;
            AxisLength *= -1;
        }

        startPoint = transform.localPosition;
        endPoint = transform.localPosition + LocalAxis * AxisLength;
    }

    private void OnValidate()
    {
        if (!interactionManager)
            interactionManager = FindObjectOfType<XRInteractionManager>();
        if (!xrGrabInteractable)
            xrGrabInteractable = GetComponent<XRGrabInteractable>();
        if (!mainGrabInteractable)
            mainGrabInteractable = transform.parent.GetComponentInParent<XRGrabInteractable>();
        if (!projectileWeapon)
            projectileWeapon = GetComponentInParent<ProjectileWeapon>();
    }

    public Vector3 GetEndPoint() => endPoint;
    public Vector3 GetStartPoint() => startPoint;


    public void FixedUpdate()
    {
        if (stopAnimation) return;

        if (isSelected)
            SlideFromHandPosition();
        else
            ReturnToOriginalPosition();
    }

    private void ReleaseIfMainHandReleased(XRBaseInteractor hand)
    {
        if (currentHand && xrGrabInteractable)
            interactionManager.SelectExit_public(currentHand, xrGrabInteractable);
    }

    private void SlideFromHandPosition()
    {
        transform.parent = originalParent;

        Vector3 worldAxis = transform.TransformDirection(LocalAxis);

        Vector3 distance = grabbingInteractor.transform.position - transform.position - grabbedOffset;
        float projected = Vector3.Dot(distance, worldAxis);

        Vector3 targetPoint;
        if (projected > 0)
            targetPoint = Vector3.MoveTowards(transform.localPosition, endPoint, projected);

        else
            targetPoint = Vector3.MoveTowards(transform.localPosition, startPoint, -projected);
        
        Vector3 move = targetPoint - transform.localPosition;

        transform.localPosition = transform.localPosition + move;

        if (hasReachedEnd == false && (transform.localPosition - endPoint).magnitude <= .001f)
        {
            hasReachedEnd = true;
            pullBackAudio.PlaySound();
        }
    }

    private void ReturnToOriginalPosition()
    {
        Vector3 targetPoint = Vector3.MoveTowards(transform.localPosition, startPoint, ReturnSpeed * Time.deltaTime);
        Vector3 move = targetPoint - transform.localPosition;

        transform.localPosition += move;

        if (hasReachedEnd && (transform.localPosition - startPoint).magnitude <= .001f)
        {
            hasReachedEnd = false;
            GunCockedEvent.Invoke();
            releaseAudio.PlaySound();
        }
    }

    private bool stopAnimation;
    
    public void SetClosed()
    {
        stopAnimation = false;
    }

    private void OnGrabbed(XRBaseInteractor interactor)
    {
        stopAnimation = false;
        currentHand = interactor;
        isSelected = true;
        grabbedOffset = interactor.transform.position - transform.position;
        grabbingInteractor = interactor;
        transform.localPosition = startPoint;
    }

    private void OnRelease(XRBaseInteractor interactor)
    {
        currentHand = null;
        isSelected = false;
        transform.localPosition = startPoint;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 end = transform.position + transform.TransformDirection(LocalAxis.normalized) * AxisLength;
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawSphere(end, 0.01f);
    }

    public void Pause()
    {
        stopAnimation = true;
    }
}