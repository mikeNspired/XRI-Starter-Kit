using System;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class VelocityTracker : MonoBehaviour
    {
        private bool isActive = false;
        private Transform trackedObject;

        const int k_ThrowSmoothingFrameCount = 20;
        const float k_DefaultThrowSmoothingDuration = 0.25f;

        public Vector3 CurrentSmoothedVelocity;
        public Vector3 CurrentSmoothedAngularVelocity;

        [SerializeField] float m_ThrowSmoothingDuration = k_DefaultThrowSmoothingDuration;

        [SerializeField] [Tooltip("The curve to use to weight velocity smoothing (most recent frames to the right.")]
        AnimationCurve m_ThrowSmoothingCurve = AnimationCurve.Linear(1f, 1f, 1f, 0f);

        private bool detachInLateUpdate;
        private Vector3 detachVelocitydetachAngularVelocity;
        private int throwSmoothingCurrentFrame;
        private float[] throwSmoothingFrameTimes = new float[k_ThrowSmoothingFrameCount];
        private Vector3[] throwSmoothingVelocityFrames = new Vector3[k_ThrowSmoothingFrameCount];
        private Vector3[] throwSmoothingAngularVelocityFrames = new Vector3[k_ThrowSmoothingFrameCount];
        private Rigidbody _rigidBody;
        private Vector3 lastPosition;
        private Quaternion lastRotation;

        private bool throwOnDetach;

        protected void Start()
        {
            if (trackedObject)
                SmoothVelocityStart();
        }

        private void Update()
        {
            if (!isActive) return;
            SmoothVelocityUpdate();
            GetSmoothedVelocity();
        }

        public void SetTrackedObject(Transform controller) => this.trackedObject = controller;

        public void StartTracking() => isActive = true;

        public void StopTracking() => isActive = false;

        private void SmoothVelocityStart()
        {
            lastPosition = trackedObject.position;
            lastRotation = trackedObject.rotation;
            Array.Clear(throwSmoothingFrameTimes, 0, throwSmoothingFrameTimes.Length);
            Array.Clear(throwSmoothingVelocityFrames, 0, throwSmoothingVelocityFrames.Length);
            Array.Clear(throwSmoothingAngularVelocityFrames, 0, throwSmoothingAngularVelocityFrames.Length);
            throwSmoothingCurrentFrame = 0;
        }

        public void GetSmoothedVelocity()
        {
            Vector3 smoothedVelocity = getSmoothedVelocityValue(throwSmoothingVelocityFrames);
            Vector3 smoothedAngularVelocity = getSmoothedVelocityValue(throwSmoothingAngularVelocityFrames);
            CurrentSmoothedVelocity = smoothedVelocity;
            CurrentSmoothedAngularVelocity = smoothedAngularVelocity;
        }

        private void SmoothVelocityUpdate()
        {
            throwSmoothingFrameTimes[throwSmoothingCurrentFrame] = Time.time;
            throwSmoothingVelocityFrames[throwSmoothingCurrentFrame] = (trackedObject.position - lastPosition) / Time.deltaTime;

            Quaternion VelocityDiff = (trackedObject.rotation * Quaternion.Inverse(lastRotation));
            throwSmoothingAngularVelocityFrames[throwSmoothingCurrentFrame] = (new Vector3(Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.x), Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.y), Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.z))
                                                                               / Time.deltaTime) * Mathf.Deg2Rad;

            throwSmoothingCurrentFrame = (throwSmoothingCurrentFrame + 1) % k_ThrowSmoothingFrameCount;
            lastPosition = trackedObject.position;
            lastRotation = trackedObject.rotation;
        }

        private Vector3 getSmoothedVelocityValue(Vector3[] velocityFrames)
        {
            Vector3 calcVelocity = new Vector3();

            int frameCounter = 0;
            float totalWeights = 0.0f;

            for (; frameCounter < k_ThrowSmoothingFrameCount; frameCounter++)
            {
                int frameIdx = (((throwSmoothingCurrentFrame - frameCounter - 1) % k_ThrowSmoothingFrameCount) + k_ThrowSmoothingFrameCount) % k_ThrowSmoothingFrameCount;
                if (throwSmoothingFrameTimes[frameIdx] == 0.0f)
                    break;

                float timeAlpha = (Time.time - throwSmoothingFrameTimes[frameIdx]) / m_ThrowSmoothingDuration;
                float velocityWeight = m_ThrowSmoothingCurve.Evaluate(Mathf.Clamp(1.0f - timeAlpha, 0.0f, 1.0f));
                calcVelocity += velocityFrames[frameIdx] * velocityWeight;
                totalWeights += velocityWeight;
                if (Time.time - throwSmoothingFrameTimes[frameIdx] > m_ThrowSmoothingDuration)
                    break;
            }

            if (totalWeights > 0.0f)
                return calcVelocity / totalWeights;
            else
                return Vector3.zero;
        }
    }
}