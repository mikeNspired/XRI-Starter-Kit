// Physics-driven lever. Place this script on the moving lever arm (Rigidbody required).
// The arm should be a direct child of a static pivot; do not move the assembly during play.
// Rotation axis is the lever arm's local X axis. Min/max angles define the travel range.
// The HingeJoint is created at runtime — no manual joint setup in the inspector needed.
// Optional: add XRGrabInteractable to the same GameObject for grab-and-push interaction.
// If XRGrabInteractable is present its movementType is set to VelocityTracking automatically.

using UnityEngine;
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
        [SerializeField] private bool m_LockToValue = true;

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

        #endregion

        #region Public Properties

        public bool LeverValue => m_LeverValue;
        public float NormalizedAngle { get; private set; }
        public bool IsGrabbed => m_IsGrabbed;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.useGravity = false;
            SetupJoint();

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

            if (Mathf.Abs(normalized - m_PreviousNormalized) < c_ValueChangeTolerance)
                return;

            m_PreviousNormalized = normalized;
            m_OnValueChange.Invoke(normalized);

            // Dead-zone hysteresis: lever must travel past (0.5 ± deadZone) to switch
            float activateThreshold = 0.5f + m_DeadZone;
            float deactivateThreshold = 0.5f - m_DeadZone;

            bool shouldBeActive = m_LeverValue
                ? normalized > deactivateThreshold
                : normalized > activateThreshold;

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

            if (m_LockToValue && !m_IsGrabbed)
                SnapToLockedAngle();
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

        private void SnapToLockedAngle()
        {
            float targetAngle = m_LeverValue ? m_MaxAngle : m_MinAngle;
            if (Mathf.Abs(m_Joint.angle - targetAngle) < 1f) return;

            float delta = Mathf.DeltaAngle(m_Joint.angle, targetAngle);
            m_Rigidbody.angularVelocity = transform.TransformDirection(Vector3.right * (delta * 10f * Time.fixedDeltaTime));
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
