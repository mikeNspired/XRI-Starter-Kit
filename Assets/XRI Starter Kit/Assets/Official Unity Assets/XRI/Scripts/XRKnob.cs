using System;
using XR.Interaction.Toolkit.Samples;using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static Unity.Mathematics.math;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// An interactable knob that follows the rotation of the interactor
    /// </summary>
    public class XRKnob : XRBaseInteractable
    {
        const float k_ModeSwitchDeadZone = 0.1f; // Prevents rapid switching between the different rotation tracking modes

        /// <summary>
        /// Helper class used to track rotations that can go beyond 180 degrees while minimizing accumulation error
        /// </summary>
        struct TrackedRotation
        {
            /// <summary>
            /// The anchor rotation we calculate an offset from
            /// </summary>
            float baseAngle;

            /// <summary>
            /// The target rotate we calculate the offset to
            /// </summary>
            float currentOffset;

            /// <summary>
            /// Any previous offsets we've added in
            /// </summary>
            float accumulatedAngle;

            /// <summary>
            /// The total rotation that occurred from when this rotation started being tracked
            /// </summary>
            public float TotalOffset
            {
                get { return accumulatedAngle + currentOffset; }
            }

            /// <summary>
            /// Resets the tracked rotation so that total offset returns 0
            /// </summary>
            public void Reset()
            {
                baseAngle = 0.0f;
                currentOffset = 0.0f;
                accumulatedAngle = 0.0f;
            }

            /// <summary>
            /// Sets a new anchor rotation while maintaining any previously accumulated offset
            /// </summary>
            /// <param name="direction">The XZ vector used to calculate a rotation angle</param>
            public void SetBaseFromVector(Vector3 direction)
            {
                // Update any accumulated angle
                accumulatedAngle += currentOffset;

                // Now set a new base angle
                baseAngle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
                currentOffset = 0.0f;
            }

            public void SetTargetFromVector(Vector3 direction)
            {
                // Set the target angle
                var targetAngle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;

                // Return the offset
                currentOffset = ShortestAngleDistance(baseAngle, targetAngle, 360.0f);

                // If the offset is greater than 90 degrees, we update the base so we can rotate beyond 180 degrees
                if (Mathf.Abs(currentOffset) > 90.0f)
                {
                    baseAngle = targetAngle;
                    accumulatedAngle += currentOffset;
                    currentOffset = 0.0f;
                }
            }
        }

        [SerializeField]
        [Tooltip("The object that is visually grabbed and manipulated")]
        Transform m_Handle = null;

        [SerializeField]
        [Tooltip("The default behaviour uses the attach transform")]
        bool m_UseControllerForPosition = true;
        
        [SerializeField]
        [Tooltip("The value of the knob")]
        [Range(0.0f, 1.0f)]
        float m_Value = 0.5f;

        [SerializeField]
        [Tooltip("Whether this knob's rotation should be clamped by the angle limits")]
        bool m_ClampedMotion = true;

        [SerializeField]
        [Tooltip("Rotation of the knob at value '1'")]
        float m_MaxAngle = 90.0f;

        [SerializeField]
        [Tooltip("Rotation of the knob at value '0'")]
        float m_MinAngle = -90.0f;

        [SerializeField]
        [Tooltip("Angle increments to support, if greater than '0'")]
        float m_AngleIncrement = 0.0f;

        [SerializeField]
        [Tooltip("The position of the interactor controls rotation when outside this radius")]
        float m_PositionTrackedRadius = 0.1f;

        [SerializeField]
        [Tooltip("How much controller rotation ")]
        float m_TwistSensitivity = 1.5f;

        [SerializeField]
        [Tooltip("Events to trigger when the knob is rotated")]
        UnityEventFloat m_OnValueChange = new UnityEventFloat();
        
        [SerializeField]
        [Tooltip("Events to trigger when the knob is incremented")]
        UnityEventInt m_OnIncrementValueChange = new UnityEventInt();

        [SerializeField]
        [Tooltip("Remap sliders min value of 0 to a new value")]
        float m_RemapValueMin = 0f;
        [SerializeField]
        [Tooltip("Remap sliders max value of 1 to a new value")]
        float m_RemapValueMax = 1f;
        
        IXRSelectInteractor m_Interactor;
        ControllerInputActionManager m_Controller;

        bool m_PositionDriven = false;
        bool m_UpVectorDriven = false;

        TrackedRotation m_PositionAngles = new TrackedRotation();
        TrackedRotation m_UpVectorAngles = new TrackedRotation();
        TrackedRotation m_ForwardVectorAngles = new TrackedRotation();

        float m_BaseKnobRotation = 0.0f;

        /// <summary>
        /// The object that is visually grabbed and manipulated
        /// </summary>
        public Transform Handle { get { return m_Handle; } set { m_Handle = value; } }

        /// <summary>
        /// The value of the knob
        /// </summary>
        public float Value
        {
            get { return m_Value; }
            set
            {
                SetValue(value);
                SetKnobRotation(ValueToRotation());
            }
        }

        /// <summary>
        /// Whether this knob's rotation should be clamped by the angle limits
        /// </summary>
        public bool ClampedMotion { get { return m_ClampedMotion; } set { m_ClampedMotion = value; } }

        /// <summary>
        /// Rotation of the knob at value '1'
        /// </summary>
        public float MaxAngle { get { return m_MaxAngle; } set { m_MaxAngle = value; } }

        /// <summary>
        /// Rotation of the knob at value '0'
        /// </summary>
        public float MinAngle { get { return m_MinAngle; } set { m_MinAngle = value; } }

        /// <summary>
        /// The position of the interactor controls rotation when outside this radius
        /// </summary>
        public float PositionTrackedRadius { get { return m_PositionTrackedRadius; } set { m_PositionTrackedRadius = value; } }

        /// <summary>
        /// Events to trigger when the knob is rotated
        /// </summary>
        public UnityEventFloat OnValueChange => m_OnValueChange;
        public UnityEventInt OnIncrementValueChange => m_OnIncrementValueChange;

        private void Start()
        {
            SetValue(m_Value);
            SetKnobRotation(ValueToRotation());
            m_previousValue = Value;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(StartGrab);
            selectExited.AddListener(EndGrab);
        }

        protected override void OnDisable()
        {
            selectEntered.RemoveListener(StartGrab);
            selectExited.RemoveListener(EndGrab);
            base.OnDisable();
        }

        private void StartGrab(SelectEnterEventArgs args)
        {
            m_Interactor = args.interactorObject;
            m_Controller = m_Interactor.transform.GetComponentInParent<ControllerInputActionManager>();
            m_PositionAngles.Reset();
            m_UpVectorAngles.Reset();
            m_ForwardVectorAngles.Reset();

            UpdateBaseKnobRotation();
            UpdateRotation(true);
        }

        private void EndGrab(SelectExitEventArgs args)
        {
            m_Interactor = null;
            m_Controller = null;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (isSelected)
                {
                    UpdateRotation();
                }
            }
        }

        private void UpdateRotation(bool freshCheck = false)
        {
            // Are we in position offset or direction rotation mode?
            var interactorTransform = m_UseControllerForPosition ? m_Controller.transform : m_Interactor.GetAttachTransform(this);
            
            // We cache the three potential sources of rotation - the position offset, the forward vector of the controller, and up vector of the controller
            // We store any data used for determining which rotation to use, then flatten the vectors to the local xz plane
            var localOffset = transform.InverseTransformVector(interactorTransform.position - m_Handle.position);
            localOffset.y = 0.0f;
            var radiusOffset = transform.TransformVector(localOffset).magnitude;
            localOffset.Normalize();

            var localForward = transform.InverseTransformDirection(interactorTransform.forward);
            var localY = Math.Abs(localForward.y);
            localForward.y = 0.0f;
            localForward.Normalize();

            var localUp = transform.InverseTransformDirection(interactorTransform.up);
            localUp.y = 0.0f;
            localUp.Normalize();


            if (m_PositionDriven && !freshCheck)
                radiusOffset *= (1.0f + k_ModeSwitchDeadZone);

            // Determine when a certain source of rotation won't contribute - in that case we bake in the offset it has applied
            // and set a new anchor when they can contribute again
            if (radiusOffset >= m_PositionTrackedRadius)
            {
                if (!m_PositionDriven || freshCheck)
                {
                    m_PositionAngles.SetBaseFromVector(localOffset);
                    m_PositionDriven = true;
                }
            }
            else
                m_PositionDriven = false;

            // If it's not a fresh check, then we weight the local Y up or down to keep it from flickering back and forth at boundaries
            if (!freshCheck)
            {
                if (!m_UpVectorDriven)
                    localY *= (1.0f - (k_ModeSwitchDeadZone * 0.5f));
                else
                    localY *= (1.0f + (k_ModeSwitchDeadZone * 0.5f));
            }

            if (localY > 0.707f)
            {
                if (!m_UpVectorDriven || freshCheck)
                {
                    m_UpVectorAngles.SetBaseFromVector(localUp);
                    m_UpVectorDriven = true;
                }
            }
            else
            {
                if (m_UpVectorDriven || freshCheck)
                {
                    m_ForwardVectorAngles.SetBaseFromVector(localForward);
                    m_UpVectorDriven = false;
                }
            }

            // Get angle from position
            if (m_PositionDriven)
                m_PositionAngles.SetTargetFromVector(localOffset);

            if (m_UpVectorDriven)
                m_UpVectorAngles.SetTargetFromVector(localUp);
            else
                m_ForwardVectorAngles.SetTargetFromVector(localForward);

            // Apply offset to base knob rotation to get new knob rotation
            var knobRotation = m_BaseKnobRotation - ((m_UpVectorAngles.TotalOffset + m_ForwardVectorAngles.TotalOffset) * m_TwistSensitivity) - m_PositionAngles.TotalOffset;

            // Clamp to range
            if (m_ClampedMotion)
                knobRotation = Mathf.Clamp(knobRotation, m_MinAngle, m_MaxAngle);

            SetKnobRotation(knobRotation);

            // Reverse to get value
            var knobValue = (knobRotation - m_MinAngle) / (m_MaxAngle - m_MinAngle);
            SetValue(knobValue);
        }

        private void SetKnobRotation(float angle)
        {
            if (m_AngleIncrement > 0)
            {
                var normalizeAngle = angle - m_MinAngle;
                angle = (Mathf.Round(normalizeAngle / m_AngleIncrement) * m_AngleIncrement) + m_MinAngle;
            }

            if (m_Handle != null)
                m_Handle.localEulerAngles = new Vector3(0.0f, angle, 0.0f);
        }

        private float m_previousValue;

        private void SetValue(float value)
        {
            if (m_ClampedMotion)
                value = Mathf.Clamp01(value);

            if (m_AngleIncrement > 0)
            {
                var angleRange = m_MaxAngle - m_MinAngle;
                var angle = Mathf.Lerp(0.0f, angleRange, value);
                angle = Mathf.Round(angle / m_AngleIncrement) * m_AngleIncrement;
                value = Mathf.InverseLerp(0.0f, angleRange, angle);

                if (Math.Abs(m_previousValue - value) > .001f)
                {
                    m_previousValue = value;
                    m_OnIncrementValueChange.Invoke(Mathf.RoundToInt(angle/m_AngleIncrement));
                }
            }

            m_Value = value;
            m_OnValueChange.Invoke(remap(0,1,m_RemapValueMin,m_RemapValueMax,m_Value));

            // m_OnValueChange.Invoke(m_Value);
        }

        private float ValueToRotation()
        {
            return m_ClampedMotion ? Mathf.Lerp(m_MinAngle, m_MaxAngle, m_Value) : Mathf.LerpUnclamped(m_MinAngle, m_MaxAngle, m_Value);
        }

        void UpdateBaseKnobRotation()
        {
            m_BaseKnobRotation = Mathf.LerpUnclamped(m_MinAngle, m_MaxAngle, m_Value);
        }

        static float ShortestAngleDistance(float start, float end, float max)
        {
            var angleDelta = end - start;
            var angleSign = Mathf.Sign(angleDelta);

            angleDelta = Math.Abs(angleDelta) % max;
            if (angleDelta > (max * 0.5f))
                angleDelta = -(max - angleDelta);

            return angleDelta * angleSign;
        }

        void OnDrawGizmosSelected()
        {
            const int k_CircleSegments = 16;
            const float k_SegmentRatio = 1.0f / k_CircleSegments;

            // Nothing to do if position radius is too small
            if (m_PositionTrackedRadius <= Mathf.Epsilon)
                return;

            // Draw a circle from the handle point at size of position tracked radius
            var circleCenter = transform.position;

            if (m_Handle != null)
                circleCenter = m_Handle.position;

            var circleX = transform.right;
            var circleY = transform.forward;

            Gizmos.color = Color.green;
            var segmentCounter = 0;
            while (segmentCounter < k_CircleSegments)
            {
                var startAngle = (float)segmentCounter * k_SegmentRatio * 2.0f * Mathf.PI;
                segmentCounter++;
                var endAngle = (float)segmentCounter * k_SegmentRatio * 2.0f * Mathf.PI;

                Gizmos.DrawLine(circleCenter + (Mathf.Cos(startAngle) * circleX + Mathf.Sin(startAngle) * circleY) * m_PositionTrackedRadius,
                    circleCenter + (Mathf.Cos(endAngle) * circleX + Mathf.Sin(endAngle) * circleY) * m_PositionTrackedRadius);
            }
        }

        void OnValidate()
        {
            if (m_ClampedMotion)
                m_Value = Mathf.Clamp01(m_Value);

            if (m_MinAngle > m_MaxAngle)
                m_MinAngle = m_MaxAngle;

            SetKnobRotation(ValueToRotation());
        }
    }
}
