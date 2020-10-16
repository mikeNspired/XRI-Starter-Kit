using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityTracker : MonoBehaviour
{
    [SerializeField] private Transform controller;
    [SerializeField] private bool isActive = false;

    const int k_ThrowSmoothingFrameCount = 20;
    const float k_DefaultThrowSmoothingDuration = 0.25f;

    public Vector3 CurrentSmoothedVelocity;
    public Vector3 CurrentSmoothedAngularVelocity;

    [SerializeField] float m_ThrowSmoothingDuration = k_DefaultThrowSmoothingDuration;
    [SerializeField] [Tooltip("The curve to use to weight velocity smoothing (most recent frames to the right.")]
    AnimationCurve m_ThrowSmoothingCurve = AnimationCurve.Linear(1f, 1f, 1f, 0f);

    bool m_DetachInLateUpdate;
    Vector3 m_DetachVelocity;
    Vector3 m_DetachAngularVelocity;

    int m_ThrowSmoothingCurrentFrame;
    float[] m_ThrowSmoothingFrameTimes = new float[k_ThrowSmoothingFrameCount];
    Vector3[] m_ThrowSmoothingVelocityFrames = new Vector3[k_ThrowSmoothingFrameCount];
    Vector3[] m_ThrowSmoothingAngularVelocityFrames = new Vector3[k_ThrowSmoothingFrameCount];

    Rigidbody m_RigidBody;
    Vector3 m_LastPosition;
    Quaternion m_LastRotation;

    private bool m_ThrowOnDetach;


    public void SetController(Transform controller)
    {
        this.controller = controller;
    }
    public void StartTracking()
    {
        isActive = true;
    }

    public void StopTracking()
    {
        isActive = false;
    }
    private void Start()
    {
        SmoothVelocityStart();
    }

    private void Update()
    {
        if (!isActive) return;
        SmoothVelocityUpdate();
        GetSmoothedVelocity();
    }
    
    void SmoothVelocityStart()
    {
        m_LastPosition = controller.position;
        m_LastRotation = controller.rotation;
        Array.Clear(m_ThrowSmoothingFrameTimes, 0, m_ThrowSmoothingFrameTimes.Length);
        Array.Clear(m_ThrowSmoothingVelocityFrames, 0, m_ThrowSmoothingVelocityFrames.Length);
        Array.Clear(m_ThrowSmoothingAngularVelocityFrames, 0, m_ThrowSmoothingAngularVelocityFrames.Length);
        m_ThrowSmoothingCurrentFrame = 0;
    }

    public void GetSmoothedVelocity()
    {
        Vector3 smoothedVelocity = getSmoothedVelocityValue(m_ThrowSmoothingVelocityFrames);
        Vector3 smoothedAngularVelocity = getSmoothedVelocityValue(m_ThrowSmoothingAngularVelocityFrames);
        CurrentSmoothedVelocity = smoothedVelocity;
        CurrentSmoothedAngularVelocity = smoothedAngularVelocity;
    }

    void SmoothVelocityUpdate()
    {
        m_ThrowSmoothingFrameTimes[m_ThrowSmoothingCurrentFrame] = Time.time;
        m_ThrowSmoothingVelocityFrames[m_ThrowSmoothingCurrentFrame] = (controller.position - m_LastPosition) / Time.deltaTime;

        Quaternion VelocityDiff = (controller.rotation * Quaternion.Inverse(m_LastRotation));
        m_ThrowSmoothingAngularVelocityFrames[m_ThrowSmoothingCurrentFrame] = (new Vector3(Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.x), Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.y), Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.z))
                                                                               / Time.deltaTime) * Mathf.Deg2Rad;

        m_ThrowSmoothingCurrentFrame = (m_ThrowSmoothingCurrentFrame + 1) % k_ThrowSmoothingFrameCount;
        m_LastPosition = controller.position;
        m_LastRotation = controller.rotation;
    }

    Vector3 getSmoothedVelocityValue(Vector3[] velocityFrames)
    {
        Vector3 calcVelocity = new Vector3();
        
        int frameCounter = 0;
        float totalWeights = 0.0f;
        
        for (; frameCounter < k_ThrowSmoothingFrameCount; frameCounter++)
        {
            int frameIdx = (((m_ThrowSmoothingCurrentFrame - frameCounter - 1) % k_ThrowSmoothingFrameCount) + k_ThrowSmoothingFrameCount) % k_ThrowSmoothingFrameCount;
            if (m_ThrowSmoothingFrameTimes[frameIdx] == 0.0f)
                break;

            float timeAlpha = (Time.time - m_ThrowSmoothingFrameTimes[frameIdx]) / m_ThrowSmoothingDuration;
            float velocityWeight = m_ThrowSmoothingCurve.Evaluate(Mathf.Clamp(1.0f - timeAlpha, 0.0f, 1.0f));
            calcVelocity += velocityFrames[frameIdx] * velocityWeight;
            totalWeights += velocityWeight;
            if (Time.time - m_ThrowSmoothingFrameTimes[frameIdx] > m_ThrowSmoothingDuration)
                break;
        }

        if (totalWeights > 0.0f)
            return calcVelocity / totalWeights;
        else
            return Vector3.zero;
    }
}

