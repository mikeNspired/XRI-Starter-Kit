using System;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

public class PullInteraction : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private AudioRandomize pullBackAudio, arrowNotchedAudio, launchClip;
    [SerializeField] private Transform start, end, stringPosition;

    [FormerlySerializedAs("_autoSpawnObjectInHand")] [SerializeField]
    private AutoSpawnObjectInHandOnGrab autoSpawnObjectInHandOnGrab;

    private XRBaseInteractable xrBaseInteractable;
    private XRBaseInteractor currentInteractor;
    private float PullAmount;
    private bool isSelected, canPlayPullBackSound = true;
    private Arrow currentArrow;
    private Collider[] colliders;

    private void Start()
    {
        OnValidate();

        colliders = transform.parent.GetComponentsInChildren<Collider>(true);

        xrBaseInteractable.selectEntered.AddListener(x => OnSelectedEntered(x.interactorObject as XRBaseInteractor));
        xrBaseInteractable.selectExited.AddListener(x => OnSelectExited(x.interactorObject as XRBaseInteractor));
    }

    private void OnValidate()
    {
        if (!xrBaseInteractable)
            xrBaseInteractable = GetComponent<XRBaseInteractable>();
    }

    private void OnTriggerEnter(Collider other)
    {
        NotchArrow();

        void NotchArrow()
        {
            //Check if interactor has arrow
            if (currentArrow) return;
            if (!other.TryGetComponent(out XRBaseInteractor interactor)) return;
            if (!interactor.hasSelection) return;
            if (!interactor.firstInteractableSelected.transform.TryGetComponent(out Arrow arrow)) return;
            currentArrow = arrow;

            //Remove arrow from hand
            xrBaseInteractable.interactionManager.SelectExit(interactor, interactor.firstInteractableSelected);

            //Make arrow child of bow
            currentArrow.transform.SetParent(transform);
            currentArrow.transform.SetLocalPositionAndRotation(start.localPosition, start.localRotation);

            currentArrow.GetComponent<XRBaseInteractable>()
                .ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase.Late);
            //Disable rigidbody
            currentArrow.GetComponent<Rigidbody>().isKinematic = true;

            //Disable grabbable on arrow so player can grab string
            currentArrow.GetComponent<XRBaseInteractable>().enabled = false;

            arrowNotchedAudio.Play();
        }
    }

    private void OnSelectedEntered(XRBaseInteractor interactor)
    {
        isSelected = true;
        currentInteractor = interactor;
        currentInteractor.GetComponentInParent<XRBaseController>()?.SendHapticImpulse(.7f, .05f);
    }


    private void OnSelectExited(XRBaseInteractor interactor)
    {
        isSelected = false;
        currentInteractor = null;
        canPlayPullBackSound = true;
        launchClip.Play(PullAmount);

        if (PullAmount > 0 && currentArrow)
        {
            currentArrow.transform.SetParent(null);
            currentArrow.Release(PullAmount, colliders);
            currentArrow = null;
            autoSpawnObjectInHandOnGrab.TrySpawn();
        }

        PullAmount = 0f;
        skinnedMeshRenderer.SetBlendShapeWeight(0, PullAmount);
    }


    private float lastFramePullBack;

    public void Update()
    {
        if (!isSelected) return;
        Vector3 pullPosition = currentInteractor.transform.position;
        PullAmount = CalculatePull(pullPosition);

        skinnedMeshRenderer.SetBlendShapeWeight(0, PullAmount * 100);

        //Update string position from blend shape string position
        stringPosition.position = Vector3.Lerp(start.position, end.position, PullAmount);
        stringPosition.rotation = Quaternion.Lerp(start.rotation, end.rotation, PullAmount);

        if (currentArrow)
            currentArrow.transform.SetPositionAndRotation(stringPosition.position, stringPosition.rotation);

        if (PullAmount > .3f)
        {
            if (Math.Abs(lastFramePullBack - PullAmount) > .01f)
                currentInteractor.GetComponentInParent<XRBaseController>()?.SendHapticImpulse(PullAmount / 5f, .05f);
            if (canPlayPullBackSound)
            {
                canPlayPullBackSound = false;
                pullBackAudio.Play();
            }
        }
        else
            canPlayPullBackSound = true;

        lastFramePullBack = PullAmount;
    }

    private float CalculatePull(Vector3 pullPosition)
    {
        Vector3 pullDirection = pullPosition - start.position;
        Vector3 targetDirection = end.position - start.position;
        float maxLength = targetDirection.magnitude;

        targetDirection.Normalize();
        float pullValue = Vector3.Dot(pullDirection, targetDirection) / maxLength;
        return Mathf.Clamp(pullValue, 0, 1);
    }
}