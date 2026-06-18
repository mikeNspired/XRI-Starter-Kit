// Physics-driven rotary knob. Place this script on the rotating knob body (Rigidbody required).
// The knob should be a direct child of a static base; do not move the assembly during play.
// Rotation axis is the knob body's local Y axis. Min/max angles define the travel range.
// The HingeJoint is created at runtime — no manual joint setup in the inspector needed.
// Cumulative angle is tracked across frames to handle HingeJoint's ±180° wrap-around.

using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsKnobInteractable : MonoBehaviour
    {
        #region Constants

        private const float c_ValueChangeTolerance = 0.001f;

        #endregion

        #region Private Fields

        [Header("Knob Settings")]
        [SerializeField] private bool m_ClampedMotion = true;
        [SerializeField] private float m_MinAngle = -180f;
        [SerializeField] private float m_MaxAngle = 180f;
        [SerializeField, Min(0f)] private float m_AngleIncrement = 0f;
        [SerializeField] private float m_RemapValueMin = 0f;
        [SerializeField] private float m_RemapValueMax = 1f;

        [Header("Motor Damping")]
        [SerializeField] private bool m_UseMotor = true;
        [SerializeField, Min(0f)] private float m_MotorDamper = 5f;

        [Header("Events")]
        [SerializeField] private UnityEventFloat m_OnValueChange = new UnityEventFloat();
        [SerializeField] private UnityEventInt m_OnIncrementValueChange = new UnityEventInt();

        private Rigidbody m_Rigidbody;
        private HingeJoint m_Joint;
        private float m_CumulativeAngle;
        private float m_LastRawAngle;
        private float m_PreviousNormalized = -1f;
        private int m_PreviousStep = int.MinValue;
        private bool m_Initialized;

        #endregion

        #region Public Properties

        public float CumulativeAngle => m_CumulativeAngle;
        public float Value { get; private set; }
        public int StepValue { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.useGravity = false;
            SetupJoint();
            m_LastRawAngle = m_Joint.angle;
            m_CumulativeAngle = 0f;
            m_Initialized = true;
        }

        private void FixedUpdate()
        {
            if (!m_Initialized) return;

            // Accumulate angle delta to survive HingeJoint's ±180° wrap
            float rawAngle = m_Joint.angle;
            float delta = Mathf.DeltaAngle(m_LastRawAngle, rawAngle);
            m_LastRawAngle = rawAngle;
            m_CumulativeAngle += delta;

            float clampedAngle = m_ClampedMotion
                ? Mathf.Clamp(m_CumulativeAngle, m_MinAngle, m_MaxAngle)
                : m_CumulativeAngle;

            float range = m_MaxAngle - m_MinAngle;
            float normalized = range > 0f
                ? Mathf.Clamp01((clampedAngle - m_MinAngle) / range)
                : 0f;

            if (m_AngleIncrement > 0f)
            {
                int steps = Mathf.RoundToInt(clampedAngle / m_AngleIncrement);
                float snappedAngle = steps * m_AngleIncrement;
                normalized = range > 0f ? Mathf.Clamp01((snappedAngle - m_MinAngle) / range) : 0f;

                if (steps != m_PreviousStep)
                {
                    m_PreviousStep = steps;
                    StepValue = steps;
                    m_OnIncrementValueChange.Invoke(steps);
                }
            }

            if (Mathf.Abs(normalized - m_PreviousNormalized) < c_ValueChangeTolerance)
                return;

            m_PreviousNormalized = normalized;
            float remapped = Mathf.Lerp(m_RemapValueMin, m_RemapValueMax, normalized);
            Value = remapped;
            m_OnValueChange.Invoke(remapped);
        }

        #endregion

        #region Private Methods

        private void SetupJoint()
        {
            m_Joint = gameObject.AddComponent<HingeJoint>();
            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.anchor = Vector3.zero;
            m_Joint.connectedAnchor = transform.position;
            m_Joint.axis = Vector3.up; // rotate around local Y

            if (m_ClampedMotion)
            {
                m_Joint.useLimits = true;
                m_Joint.limits = new JointLimits
                {
                    min = m_MinAngle,
                    max = m_MaxAngle,
                    bounciness = 0f,
                    bounceMinVelocity = 0f
                };
            }

            if (m_UseMotor)
            {
                m_Joint.useMotor = true;
                m_Joint.motor = new JointMotor
                {
                    targetVelocity = 0f,
                    force = 0f,
                    freeSpin = false
                };
                // Angular damping expressed via Rigidbody
                m_Rigidbody.angularDamping = m_MotorDamper;
            }
        }

        #endregion

        #region Public Methods

        public void SetAngle(float _angle)
        {
            m_CumulativeAngle = m_ClampedMotion
                ? Mathf.Clamp(_angle, m_MinAngle, m_MaxAngle)
                : _angle;
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Log Current Value")]
        private void LogCurrentValue() =>
            Debug.Log($"[PhysicsKnobInteractable] cumulative={m_CumulativeAngle:F1}° value={Value:F3} step={StepValue}");
#endif
    }
}
