// Physics-driven push button. Place this script on the moving cap (Rigidbody required).
// The cap should be a direct child of a static anchor; do not move the assembly during play.
// Press direction is determined by m_PressAxis in the cap's local space (default: local -Y = press down).
// The ConfigurableJoint is created at runtime — no manual joint setup in the inspector needed.

using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsButtonInteractable : MonoBehaviour
    {
        #region Constants

        private const float c_ValueChangeTolerance = 0.001f;

        #endregion

        #region Private Fields

        [Header("Press Settings")]
        [SerializeField] private Vector3 m_PressAxis = new Vector3(0f, -1f, 0f);
        [SerializeField, Min(0.001f)] private float m_PressDistance = 0.05f;
        [SerializeField, Range(0f, 1f)] private float m_PressThreshold = 0.5f;
        [SerializeField] private bool m_ToggleButton = false;

        [Header("Spring")]
        [SerializeField, Min(0f)] private float m_SpringForce = 200f;
        [SerializeField, Min(0f)] private float m_SpringDamper = 20f;

        [Header("Events")]
        [SerializeField] private UnityEvent m_OnPress = new UnityEvent();
        [SerializeField] private UnityEvent m_OnRelease = new UnityEvent();
        [SerializeField] private UnityEventFloat m_OnValueChange = new UnityEventFloat();

        private Rigidbody m_Rigidbody;
        private ConfigurableJoint m_Joint;
        private Vector3 m_InitialLocalPosition;
        private float m_PreviousValue; // starts at 0 = the rest value, so no startup OnValueChange fires
        private bool m_Pressed;
        private bool m_Toggled;

        #endregion

        #region Public Properties

        public bool IsPressed => m_Pressed;
        public bool IsToggled => m_Toggled;
        public float Value { get; private set; }

        // Match XRPushButton's public events so existing integration code can subscribe in code.
        public UnityEvent OnPress => m_OnPress;
        public UnityEvent OnRelease => m_OnRelease;
        public UnityEventFloat OnValueChange => m_OnValueChange;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.useGravity = false;
            m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_InitialLocalPosition = transform.localPosition;
            SetupJoint();
        }

        private void FixedUpdate()
        {
            Vector3 localDelta = transform.localPosition - m_InitialLocalPosition;
            float displacement = Vector3.Dot(localDelta, m_PressAxis.normalized);
            float value = Mathf.Clamp01(displacement / m_PressDistance);
            Value = value;

            if (Mathf.Abs(value - m_PreviousValue) < c_ValueChangeTolerance)
                return;

            m_PreviousValue = value;
            m_OnValueChange.Invoke(value);

            bool shouldBePressed = value >= m_PressThreshold;
            if (shouldBePressed && !m_Pressed)
                HandlePressStart();
            else if (!shouldBePressed && m_Pressed)
                HandlePressEnd();
        }

        #endregion

        #region Private Methods

        private void SetupJoint()
        {
            Vector3 pressDir = m_PressAxis.normalized;

            Vector3 perp = Vector3.Cross(pressDir, Vector3.up);
            if (perp.sqrMagnitude < 0.01f)
                perp = Vector3.Cross(pressDir, Vector3.right);
            perp.Normalize();

            m_Joint = gameObject.AddComponent<ConfigurableJoint>();
            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.anchor = Vector3.zero;
            // Anchor at the MIDPOINT of travel: ConfigurableJoint linear limits are symmetric, so a
            // rest-position anchor let the cap be pulled OUT of the housing by a full press distance.
            // A centered anchor with a ±half limit bounds both flush and fully-pressed.
            m_Joint.connectedAnchor = transform.position + transform.TransformDirection(pressDir) * (m_PressDistance * 0.5f);

            // Primary axis = press direction; xMotion = the constrained axis
            m_Joint.axis = pressDir;
            m_Joint.secondaryAxis = perp;
            m_Joint.xMotion = ConfigurableJointMotion.Limited;
            m_Joint.yMotion = ConfigurableJointMotion.Locked;
            m_Joint.zMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularXMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularYMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularZMotion = ConfigurableJointMotion.Locked;

            m_Joint.linearLimit = new SoftJointLimit { limit = m_PressDistance * 0.5f };

            // Drive springs the cap back to rest. Rest sits at -half press distance from the
            // mid-travel anchor along the joint x-axis; Unity's drive convention moves the body
            // toward -targetPosition, so +half targets rest.
            // MANUAL VERIFY: cap must rest flush, not hover mid-travel — if it rests pressed-in,
            // flip this sign.
            m_Joint.targetPosition = new Vector3(m_PressDistance * 0.5f, 0f, 0f);
            m_Joint.xDrive = new JointDrive
            {
                positionSpring = m_SpringForce,
                positionDamper = m_SpringDamper,
                maximumForce = float.MaxValue
            };
        }

        private void HandlePressStart()
        {
            m_Pressed = true;
            if (m_ToggleButton)
            {
                m_Toggled = !m_Toggled;
                if (m_Toggled) m_OnPress.Invoke();
                else m_OnRelease.Invoke();
            }
            else
            {
                m_OnPress.Invoke();
            }
        }

        private void HandlePressEnd()
        {
            m_Pressed = false;
            if (!m_ToggleButton)
                m_OnRelease.Invoke();
        }

        #endregion

        #region Public Methods

        public void SetToggleState(bool _state) => m_Toggled = _state;

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Log Current Value")]
        private void LogCurrentValue() =>
            Debug.Log($"[PhysicsButtonInteractable] value={Value:F3} pressed={m_Pressed} toggled={m_Toggled}");
#endif
    }
}
