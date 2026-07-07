// Physics-driven rotary knob. Place this script on the rotating knob body (Rigidbody required).
// The knob should be a direct child of a static base; do not move the assembly during play.
// Rotation axis is the knob body's local Y axis. Min/max angles define the travel range,
// relative to the authored pose (the hinge reads 0° wherever the knob sits in the scene).
// The HingeJoint is created at runtime — no manual joint setup in the inspector needed.
// Cumulative angle is tracked across frames to handle HingeJoint's ±180° wrap-around.
// Clamped ranges within ±178° are enforced by HingeJoint limits; wider ranges (e.g. 0..270,
// multi-turn) exceed what joint limits can represent and are enforced against the cumulative
// angle in FixedUpdate instead.
// Optional: add XRGrabInteractable to the same GameObject for grab-and-twist interaction.
// If XRGrabInteractable is present its movementType is set to VelocityTracking automatically.

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsKnobInteractable : MonoBehaviour
    {
        #region Constants

        private const float c_ValueChangeTolerance = 0.001f;
        // HingeJoint limits are only reliable inside ±178°; wider clamped ranges are enforced
        // against the cumulative angle in FixedUpdate instead.
        private const float c_JointLimitRange = 178f;

        #endregion

        #region Private Fields

        [Header("Knob Settings")]
        [SerializeField] private bool m_ClampedMotion = true;
        [SerializeField] private float m_MinAngle = -180f;
        [SerializeField] private float m_MaxAngle = 180f;
        [SerializeField, Min(0f)] private float m_AngleIncrement = 0f;
        [SerializeField] private float m_RemapValueMin = 0f;
        [SerializeField] private float m_RemapValueMax = 1f;

        [Header("Rest Damping")]
        [FormerlySerializedAs("m_UseMotor")]
        [SerializeField] private bool m_UseRestDamping = true;
        [FormerlySerializedAs("m_MotorDamper")]
        [SerializeField, Min(0f)] private float m_RestDamping = 5f;

        [Header("Events")]
        [SerializeField] private UnityEventFloat m_OnValueChange = new UnityEventFloat();
        [SerializeField] private UnityEventInt m_OnIncrementValueChange = new UnityEventInt();

        private Rigidbody m_Rigidbody;
        private HingeJoint m_Joint;
        private XRGrabInteractable m_GrabInteractable;
        private float m_CumulativeAngle;
        private float m_LastRawAngle;
        private float m_PreviousNormalized = -1f;
        private int m_PreviousStep; // starts at the rest step (0) so no startup event fires
        private bool m_Initialized;
        private bool m_IsGrabbed;
        private bool m_UseJointLimits;
        private Quaternion m_InitialLocalRotation;

        #endregion

        #region Public Properties

        public float CumulativeAngle => m_CumulativeAngle;
        public float Value { get; private set; }
        public int StepValue { get; private set; }
        public bool IsGrabbed => m_IsGrabbed;

        // Match XRKnob's public events so existing integration code can subscribe in code.
        public UnityEventFloat OnValueChange => m_OnValueChange;
        public UnityEventInt OnIncrementValueChange => m_OnIncrementValueChange;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.useGravity = false;
            m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_InitialLocalRotation = transform.localRotation;

            if (m_MinAngle >= m_MaxAngle)
                Debug.LogWarning($"[PhysicsKnobInteractable] Min Angle ({m_MinAngle}) must be less than Max Angle ({m_MaxAngle}).", this);

            SetupJoint();
            m_LastRawAngle = m_Joint.angle;
            m_CumulativeAngle = 0f;
            // Seed the previous-normalized tracker with the rest value so the first FixedUpdate
            // fires no startup OnValueChange.
            float range = m_MaxAngle - m_MinAngle;
            m_PreviousNormalized = range > 0f ? Mathf.Clamp01(-m_MinAngle / range) : 0f;
            m_Initialized = true;

            if (TryGetComponent(out m_GrabInteractable))
                ConfigureGrab();
        }

        private void OnDestroy()
        {
            if (m_GrabInteractable == null) return;
            m_GrabInteractable.selectEntered.RemoveListener(OnGrabEntered);
            m_GrabInteractable.selectExited.RemoveListener(OnGrabExited);
        }

        private void FixedUpdate()
        {
            if (!m_Initialized) return;

            // Accumulate angle delta to survive HingeJoint's ±180° wrap
            float rawAngle = m_Joint.angle;
            float delta = Mathf.DeltaAngle(m_LastRawAngle, rawAngle);
            m_LastRawAngle = rawAngle;
            m_CumulativeAngle += delta;

            // Ranges the HingeJoint limits can't represent are enforced here: park the body at
            // the bound so the visual knob can't spin past the reported value.
            if (m_ClampedMotion && !m_UseJointLimits &&
                (m_CumulativeAngle < m_MinAngle || m_CumulativeAngle > m_MaxAngle))
                PinToAngle(Mathf.Clamp(m_CumulativeAngle, m_MinAngle, m_MaxAngle));

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
                // Joint limits only when the range fits; wider ranges (0..270, multi-turn) are
                // enforced against the cumulative angle in FixedUpdate.
                m_UseJointLimits = m_MinAngle >= -c_JointLimitRange && m_MaxAngle <= c_JointLimitRange;
                if (m_UseJointLimits)
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
            }

            if (m_UseRestDamping)
                m_Rigidbody.angularDamping = m_RestDamping;
        }

        // Rotate the body to the given cumulative angle and re-baseline the wrap tracking so the
        // next frame's delta reads ~0.
        private void PinToAngle(float _angle)
        {
            m_CumulativeAngle = _angle;
            m_LastRawAngle = Mathf.DeltaAngle(0f, _angle);
            m_Rigidbody.angularVelocity = Vector3.zero;
            Quaternion local = m_InitialLocalRotation * Quaternion.AngleAxis(_angle, Vector3.up);
            m_Rigidbody.MoveRotation(transform.parent != null ? transform.parent.rotation * local : local);
        }

        private void ConfigureGrab()
        {
            m_GrabInteractable.movementType = XRGrabInteractable.MovementType.VelocityTracking;
            m_GrabInteractable.throwOnDetach = false;
            m_GrabInteractable.selectEntered.AddListener(OnGrabEntered);
            m_GrabInteractable.selectExited.AddListener(OnGrabExited);
        }

        private void OnGrabEntered(SelectEnterEventArgs _args)
        {
            m_IsGrabbed = true;
            // Drop the rest damping while held so the knob tracks the hand instead of lagging behind it.
            if (m_UseRestDamping)
                m_Rigidbody.angularDamping = 0f;
        }

        private void OnGrabExited(SelectExitEventArgs _args)
        {
            m_IsGrabbed = false;
            if (m_UseRestDamping)
                m_Rigidbody.angularDamping = m_RestDamping;
        }

        #endregion

        #region Public Methods

        public void SetAngle(float _angle)
        {
            float target = m_ClampedMotion
                ? Mathf.Clamp(_angle, m_MinAngle, m_MaxAngle)
                : _angle;

            // Rotate the body to match, not just the reported value.
            if (m_Initialized)
                PinToAngle(target);
            else
                m_CumulativeAngle = target;
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Log Current Value")]
        private void LogCurrentValue() =>
            Debug.Log($"[PhysicsKnobInteractable] cumulative={m_CumulativeAngle:F1}° value={Value:F3} step={StepValue} grabbed={m_IsGrabbed}");
#endif
    }
}
