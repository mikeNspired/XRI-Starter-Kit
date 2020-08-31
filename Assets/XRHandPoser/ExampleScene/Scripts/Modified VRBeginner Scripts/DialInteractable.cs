using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
#if UNITY_EDITOR
using UnityEditor;

#endif

/// <summary>
/// Custom interactable than will rotation around a given axis. It can be limited in range and also be made either
/// continuous or snapping to integer steps.
/// Rotation can be driven either by controller rotation (e.g. rotating a volume dial) or controller movement (e.g.
/// pulling down a lever)
/// </summary>
public class DialInteractable : XRGrabInteractable
{
    public enum InteractionType
    {
        ControllerRotation,
        ControllerPull
    }

    [System.Serializable]
    public class DialTurnedAngleEvent : UnityEvent<float>
    {
    }

    [System.Serializable]
    public class DialTurnedStepEvent : UnityEvent<int>
    {
    }

    [System.Serializable]
    public class DialChangedEvent : UnityEvent<DialInteractable>
    {
    }

    public InteractionType DialType = InteractionType.ControllerRotation;

    public Rigidbody RotatingRigidbody;
    public Vector3 LocalRotationAxis;
    public Vector3 LocalAxisStart;
    public float RotationAngleMaximum;

    [Tooltip("If 0, this is a float dial going from 0 to 1, if not 0, that dial is int with that many steps")]
    public int Steps = 0;

    [SerializeField] private float startingAngle = 0;
    [SerializeField] private bool callEventsOnStartingAngle = false;
    public bool SnapOnRelease = true;

    public AudioClip SnapAudioClip;
    public AudioSource AudioSource;
    public DialTurnedAngleEvent OnDialAngleChanged;
    public DialTurnedStepEvent OnDialStepChanged;
    public DialChangedEvent OnDialChanged;
    public DialTurnedAngleEvent OnDialPercentageChanged;

    public float CurrentAngle => m_CurrentAngle;
    public int CurrentStep => m_CurrentStep;

    XRBaseInteractor m_GrabbingInteractor;
    Quaternion m_GrabbedRotation;

    Vector3 m_StartingWorldAxis;
    float m_CurrentAngle = 0;
    int m_CurrentStep = 0;

    float m_StepSize;
    Transform m_SyncTransform;
    Transform m_OriginalTransform;
    public float percentageComplete;
    public bool ReturnToStart;
    public float returnSpeed = 20;

    void Start()
    {
        AudioSource.clip = SnapAudioClip;
        LocalAxisStart.Normalize();
        LocalRotationAxis.Normalize();

        if (RotatingRigidbody == null)
        {
            RotatingRigidbody = GetComponentInChildren<Rigidbody>();
        }

        GameObject obj = new GameObject("Dial_Start_Copy");
        m_OriginalTransform = obj.transform;
        m_OriginalTransform.SetParent(transform.parent);
        m_OriginalTransform.localRotation = transform.localRotation;
        m_OriginalTransform.localPosition = transform.localPosition;

        if (Steps > 0) m_StepSize = RotationAngleMaximum / Steps;
        else m_StepSize = 0.0f;

        SetStartingPosition(startingAngle);
        transform.Rotate(LocalRotationAxis * CurrentAngle, Space.Self);
    }

    private void OnValidate()
    {
        if (!AudioSource)
            AudioSource = GetComponent<AudioSource>();
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        if (isSelected)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed)
            {
                m_StartingWorldAxis = m_OriginalTransform.TransformDirection(LocalAxisStart);

                Vector3 worldAxisStart = m_SyncTransform.TransformDirection(LocalAxisStart);
                Vector3 worldRotationAxis = m_SyncTransform.TransformDirection(LocalRotationAxis);

                float angle = 0.0f;
                Vector3 newRight = worldAxisStart;

                if (DialType == InteractionType.ControllerRotation)
                {
                    Quaternion difference = m_GrabbingInteractor.transform.rotation * Quaternion.Inverse(m_GrabbedRotation);

                    newRight = difference * worldAxisStart;

                    //get the new angle between the original right and this new right along the up axis
                    angle = Vector3.SignedAngle(m_StartingWorldAxis, newRight, worldRotationAxis);

                    if (angle < 0) angle = 360 + angle;
                }
                else
                {
                    Vector3 centerToController = m_GrabbingInteractor.transform.position - transform.position;
                    centerToController.Normalize();

                    newRight = centerToController;

                    angle = Vector3.SignedAngle(m_StartingWorldAxis, newRight, worldRotationAxis);

                    if (angle < 0) angle = 360 + angle;
                }

                //if the angle is < 0 or > to the max rotation, we clamp but TO THE CLOSEST (a simple clamp would clamp
                // of an angle of 350 for a 0-180 angle range would clamp to 180, when we want to clamp to 0)
                if (angle > RotationAngleMaximum)
                {
                    float upDiff = 360 - angle;
                    float lowerDiff = angle - RotationAngleMaximum;

                    if (upDiff < lowerDiff) angle = 0;
                    else angle = RotationAngleMaximum;
                }

                float finalAngle = angle;
                if (!SnapOnRelease && Steps > 0)
                {
                    int step = Mathf.RoundToInt(angle / m_StepSize);
                    finalAngle = step * m_StepSize;

                    if (!Mathf.Approximately(finalAngle, m_CurrentAngle))
                    {
                        AudioSource.Play();

                        OnDialStepChanged.Invoke(step);
                        OnDialChanged.Invoke(this);
                        m_CurrentStep = step;
                    }
                }

                //first, we use the raw angle to move the sync transform, that allow to keep the proper current rotation
                //even if we snap during rotation
                newRight = Quaternion.AngleAxis(angle, worldRotationAxis) * m_StartingWorldAxis;
                angle = Vector3.SignedAngle(worldAxisStart, newRight, worldRotationAxis);
                Quaternion newRot = Quaternion.AngleAxis(angle, worldRotationAxis) * m_SyncTransform.rotation;

                //then we redo it but this time using finalAngle, that will snap if needed.
                newRight = Quaternion.AngleAxis(finalAngle, worldRotationAxis) * m_StartingWorldAxis;
                m_CurrentAngle = finalAngle;
                OnDialAngleChanged.Invoke(finalAngle);
                OnDialChanged.Invoke(this);
                finalAngle = Vector3.SignedAngle(worldAxisStart, newRight, worldRotationAxis);
                Quaternion newRBRotation = Quaternion.AngleAxis(finalAngle, worldRotationAxis) * m_SyncTransform.rotation;

                if (RotatingRigidbody != null)
                {
                    RotatingRigidbody.MoveRotation(newRBRotation);
                }
                else
                    transform.rotation = newRBRotation;

                m_SyncTransform.transform.rotation = newRot;

                m_GrabbedRotation = m_GrabbingInteractor.transform.rotation;

                CalculateRotationPercentage();
            }
        }
    }

    private void SetStartingPosition(float angle)
    {
        float finalAngle = angle;
        if (!SnapOnRelease && Steps > 0)
        {
            int step = Mathf.RoundToInt(angle / m_StepSize);
            finalAngle = step * m_StepSize;

            if (!Mathf.Approximately(finalAngle, m_CurrentAngle))
            {
                AudioSource.Play();

                OnDialStepChanged.Invoke(step);
                OnDialChanged.Invoke(this);
                m_CurrentStep = step;
            }
        }

        if (m_SyncTransform == null)
            m_SyncTransform = transform;


        m_CurrentAngle = finalAngle;
        percentageComplete = Remap(m_CurrentAngle, 0, RotationAngleMaximum, 0, 1);

        if (callEventsOnStartingAngle)
        {
            OnDialAngleChanged.Invoke(finalAngle);
            OnDialChanged.Invoke(this);
            OnDialPercentageChanged.Invoke(percentageComplete);
        }
    }


    private IEnumerator ReturnToStartPosition()
    {
        if(Steps > 0) yield break;
        while (Math.Abs(CurrentAngle - startingAngle) > .1f)
        {
            float newAngle = 1;
            if (CurrentAngle - startingAngle > 0)
                newAngle = -1;

            newAngle *= .01f * returnSpeed * Math.Abs(CurrentAngle - startingAngle);

            transform.Rotate(LocalRotationAxis * newAngle, Space.Self);

            m_CurrentAngle += newAngle;
            CalculateRotationPercentage();
            yield return new WaitForSeconds(Time.deltaTime);
        }

        m_CurrentAngle = startingAngle;
        CalculateRotationPercentage();

    }

    private void CalculateRotationPercentage()
    {
        percentageComplete = Remap(m_CurrentAngle, 0, RotationAngleMaximum, 0, 1);
        OnDialPercentageChanged.Invoke(percentageComplete);
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    protected override void OnSelectEnter(XRBaseInteractor interactor)
    {
        StopAllCoroutines();

        m_GrabbedRotation = interactor.transform.rotation;
        m_GrabbingInteractor = interactor;

//create an object that will track the rotation
        var syncObj = new GameObject("TEMP_DialSyncTransform");
        m_SyncTransform = syncObj.transform;
        if (RotatingRigidbody != null)
        {
            m_SyncTransform.rotation = RotatingRigidbody.transform.rotation;
            m_SyncTransform.position = RotatingRigidbody.position;
        }

        else
        {
            m_SyncTransform.rotation = transform.rotation;
            m_SyncTransform.position = transform.position;
        }

        base.OnSelectEnter(interactor);
    }

    protected override void OnSelectExit(XRBaseInteractor interactor)
    {
        base.OnSelectExit(interactor);
        if (SnapOnRelease && Steps > 0)
        {
            Vector3 right = transform.TransformDirection(LocalAxisStart);
            Vector3 up = transform.TransformDirection(LocalRotationAxis);

            float angle = Vector3.SignedAngle(m_StartingWorldAxis, right, up);
            if (angle < 0) angle = 360 + angle;

            int step = Mathf.RoundToInt(angle / m_StepSize);
            angle = step * m_StepSize;

            // if (angle != m_CurrentAngle)
            // {
            AudioSource.Play();
            OnDialStepChanged.Invoke(step);
            OnDialChanged.Invoke(this);
            m_CurrentStep = step;
            //}

            Vector3 newRight = Quaternion.AngleAxis(angle, up) * m_StartingWorldAxis;
            angle = Vector3.SignedAngle(right, newRight, up);

            m_CurrentAngle = angle;

            if (RotatingRigidbody != null)
            {
                Quaternion newRot = Quaternion.AngleAxis(angle, up) * RotatingRigidbody.rotation;
                RotatingRigidbody.MoveRotation(newRot);
            }
            else
            {
                Quaternion newRot = Quaternion.AngleAxis(angle, up) * transform.rotation;
                transform.rotation = newRot;
            }
        }

        if (ReturnToStart)
            StartCoroutine(ReturnToStartPosition());
        Destroy(m_SyncTransform.gameObject);
    }

    public override bool IsSelectableBy(XRBaseInteractor interactor)
    {
        int interactorLayerMask = 1 << interactor.gameObject.layer;
        return base.IsSelectableBy(interactor) && (interactionLayerMask.value & interactorLayerMask) != 0;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
        Handles.DrawSolidArc(transform.position, transform.TransformDirection(LocalRotationAxis), transform.TransformDirection(LocalAxisStart), RotationAngleMaximum, 0.2f);
    }
#endif
}