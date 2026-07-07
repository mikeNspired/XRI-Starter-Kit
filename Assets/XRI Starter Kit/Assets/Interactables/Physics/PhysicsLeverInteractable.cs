// Physics-driven lever. Place this script on the moving lever arm (Rigidbody required).
// The arm should be a direct child of a static pivot; do not move the assembly during play.
// Rotation axis is the lever arm's local X axis. Min/max angles define the travel range and are
// RELATIVE TO THE AUTHORED POSE — the hinge reads 0° wherever the arm sits in the scene, so an
// arm authored at its "on" end should use e.g. min -120 / max 0. The lever state is seeded from
// that authored angle at startup.
// The HingeJoint is created at runtime — no manual joint setup in the inspector needed.
// Optional: add XRGrabInteractable to the same GameObject for grab-and-push interaction (no hand
// physics colliders required). When present, its movementType is forced to VelocityTracking so the
// hinge constrains the grab to the arc instead of the hand pulling the arm off its pivot.
// Use EITHER m_LockToValue OR m_UseSpring, not both — a return spring and a snap-to-end fight.
// If both are enabled, the lock wins and the return spring is disabled with a warning.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsLeverInteractable : MonoBehaviour
    {
        #region Constants

        private const float c_ValueChangeTolerance = 0.005f;

        #endregion

        #region Private Fields

        [Header("Lever Settings")]
        [SerializeField] private float m_MinAngle = -60f;
        [SerializeField] private float m_MaxAngle = 60f;
        [SerializeField, Range(0f, 0.49f)] private float m_DeadZone = 0.1f;
        [SerializeField, Min(0f)] private float m_AngularDamping = 1f;
        [SerializeField] private bool m_LockToValue = true;
        [SerializeField, Min(0f)] private float m_LockSpringForce = 50f;
        [SerializeField, Min(0f)] private float m_LockSpringDamper = 5f;

        [Header("Return Spring")]
        [SerializeField] private bool m_UseSpring = false;
        [SerializeField, Min(0f)] private float m_SpringForce = 50f;
        [SerializeField, Min(0f)] private float m_SpringDamper = 5f;
        [SerializeField] private float m_SpringTargetAngle = 0f;

        [Header("Events")]
        [SerializeField] private UnityEvent m_OnLeverActivate = new UnityEvent();
        [SerializeField] private UnityEvent m_OnLeverDeactivate = new UnityEvent();
        [SerializeField] private UnityEventFloat m_OnValueChange = new UnityEventFloat();

        private Rigidbody m_Rigidbody;
        private HingeJoint m_Joint;
        private XRGrabInteractable m_GrabInteractable;
        private float m_PreviousNormalized = -1f;
        private bool m_LeverValue;
        private bool m_IsGrabbed;
        private bool m_LockSpringActive;
        private float m_LockSpringTarget = float.NaN;

        #endregion

        #region Public Properties

        public bool LeverValue => m_LeverValue;
        public float NormalizedAngle { get; private set; }
        public bool IsGrabbed => m_IsGrabbed;

        // Match XRLever's public events so existing integration code can subscribe in code.
        public UnityEvent OnLeverActivate => m_OnLeverActivate;
        public UnityEvent OnLeverDeactivate => m_OnLeverDeactivate;
        public UnityEventFloat OnValueChange => m_OnValueChange;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.useGravity = false;
            m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_Rigidbody.angularDamping = m_AngularDamping;

            if (m_MinAngle >= m_MaxAngle)
                Debug.LogWarning($"[PhysicsLeverInteractable] Min Angle ({m_MinAngle}) must be less than Max Angle ({m_MaxAngle}).", this);

            if (m_LockToValue && m_UseSpring)
            {
                Debug.LogWarning("[PhysicsLeverInteractable] Lock To Value and Use Spring are both enabled and fight each other; using the lock and disabling the return spring.", this);
                m_UseSpring = false;
            }

            SetupJoint();

            // The hinge reads 0° at the authored pose, so seed the lever state from where the arm
            // was placed (no events) — otherwise the lock would drag an arm authored at its "on"
            // end down to MinAngle on scene start.
            float initialNormalized = Mathf.InverseLerp(m_MinAngle, m_MaxAngle, 0f);
            m_LeverValue = initialNormalized > 0.5f;
            m_PreviousNormalized = initialNormalized;

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
            float angle = m_Joint.angle;
            float normalized = Mathf.InverseLerp(m_MinAngle, m_MaxAngle, angle);
            NormalizedAngle = normalized;

            if (Mathf.Abs(normalized - m_PreviousNormalized) >= c_ValueChangeTolerance)
            {
                m_PreviousNormalized = normalized;
                m_OnValueChange.Invoke(normalized);
                UpdateLeverState(normalized);
            }

            if (m_LockToValue)
                UpdateLockSpring();
        }

        private void UpdateLeverState(float _normalized)
        {
            // Dead-zone hysteresis: lever must travel past (0.5 ± deadZone) to switch
            float activateThreshold = 0.5f + m_DeadZone;
            float deactivateThreshold = 0.5f - m_DeadZone;

            bool shouldBeActive = m_LeverValue
                ? _normalized > deactivateThreshold
                : _normalized > activateThreshold;

            if (shouldBeActive && !m_LeverValue)
            {
                m_LeverValue = true;
                m_OnLeverActivate.Invoke();
            }
            else if (!shouldBeActive && m_LeverValue)
            {
                m_LeverValue = false;
                m_OnLeverDeactivate.Invoke();
            }
        }

        #endregion

        #region Private Methods

        private void SetupJoint()
        {
            m_Joint = gameObject.AddComponent<HingeJoint>();
            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.anchor = Vector3.zero;
            m_Joint.connectedAnchor = transform.position;
            m_Joint.axis = Vector3.right; // rotation around local X

            m_Joint.useLimits = true;
            m_Joint.limits = new JointLimits
            {
                min = m_MinAngle,
                max = m_MaxAngle,
                bounciness = 0f,
                bounceMinVelocity = 0f
            };

            if (m_UseSpring)
            {
                m_Joint.useSpring = true;
                m_Joint.spring = new JointSpring
                {
                    spring = m_SpringForce,
                    damper = m_SpringDamper,
                    targetPosition = m_SpringTargetAngle
                };
            }
        }

        private void ConfigureGrab()
        {
            m_GrabInteractable.movementType = XRGrabInteractable.MovementType.VelocityTracking;
            m_GrabInteractable.throwOnDetach = false;
            m_GrabInteractable.selectEntered.AddListener(OnGrabEntered);
            m_GrabInteractable.selectExited.AddListener(OnGrabExited);
        }

        private void OnGrabEntered(SelectEnterEventArgs _args) => m_IsGrabbed = true;
        private void OnGrabExited(SelectExitEventArgs _args)   => m_IsGrabbed = false;

        // The lock is a hinge spring toward the active end: spring torque composes with contact
        // impulses, so a hand collider can push the arm through the lock (a direct angularVelocity
        // write would overwrite the push every step), and the damped spring parks a settled arm
        // exactly at its end against the limit. Disabled while grabbed so it never fights the hand.
        private void UpdateLockSpring()
        {
            bool active = !m_IsGrabbed;
            float target = m_LeverValue ? m_MaxAngle : m_MinAngle;
            if (active == m_LockSpringActive && Mathf.Approximately(target, m_LockSpringTarget))
                return;

            m_LockSpringActive = active;
            m_LockSpringTarget = target;
            m_Joint.useSpring = active;
            if (active)
            {
                m_Joint.spring = new JointSpring
                {
                    spring = m_LockSpringForce,
                    damper = m_LockSpringDamper,
                    targetPosition = target
                };
            }
        }

        #endregion

        #region Public Methods

        public void SetLeverValue(bool _value, bool _fireEvents = true)
        {
            if (m_LeverValue == _value) return;
            m_LeverValue = _value;
            if (!_fireEvents) return;
            if (m_LeverValue) m_OnLeverActivate.Invoke();
            else m_OnLeverDeactivate.Invoke();
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Log Current Value")]
        private void LogCurrentValue() =>
            Debug.Log($"[PhysicsLeverInteractable] angle={m_Joint?.angle:F1}° normalized={NormalizedAngle:F3} active={m_LeverValue} grabbed={m_IsGrabbed}");
#endif
    }
}
