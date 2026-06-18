// Physics-driven joystick. Place this script on the joystick shaft Rigidbody.
// The shaft should be a direct child of a static base; do not move the assembly during play.
// The shaft tilts around its local X (pitch) and Z (roll) axes, mapping to output Y and X axes.
// A ConfigurableJoint with angular X/Z limits and optional return spring is created at runtime.

using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsJoystickInteractable : MonoBehaviour
    {
        #region Constants

        private const float c_ValueChangeTolerance = 0.005f;

        #endregion

        #region Private Fields

        [Header("Joystick Settings")]
        [SerializeField, Range(1f, 90f)] private float m_MaxAngle = 45f;
        [SerializeField] private bool m_XAxis = true;
        [SerializeField] private bool m_YAxis = true;
        [SerializeField] private float m_RemapValueMin = -1f;
        [SerializeField] private float m_RemapValueMax = 1f;

        [Header("Return Spring")]
        [SerializeField] private bool m_ReturnToZero = true;
        [SerializeField, Min(0f)] private float m_SpringForce = 100f;
        [SerializeField, Min(0f)] private float m_SpringDamper = 10f;

        [Header("Events")]
        [SerializeField] private UnityEventVector2 m_ValueChanged = new UnityEventVector2();
        [SerializeField] private UnityEventFloat m_SingleValueChanged = new UnityEventFloat();

        private Rigidbody m_Rigidbody;
        private ConfigurableJoint m_Joint;
        private Vector2 m_PreviousValue = new Vector2(-2f, -2f);

        #endregion

        #region Public Properties

        public Vector2 CurrentValue { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.useGravity = false;
            SetupJoint();
        }

        private void FixedUpdate()
        {
            // Read tilt from local Euler angles; map ±maxAngle to ±1 then remap
            Vector3 euler = transform.localEulerAngles;

            // Unity Euler angles are 0-360; wrap to ±180
            float pitchRaw = euler.x > 180f ? euler.x - 360f : euler.x; // tilt forward/back → Y output
            float rollRaw  = euler.z > 180f ? euler.z - 360f : euler.z; // tilt left/right  → X output

            float normalizedX = m_XAxis ? Mathf.Clamp(rollRaw  / m_MaxAngle, -1f, 1f) : 0f;
            float normalizedY = m_YAxis ? Mathf.Clamp(pitchRaw / m_MaxAngle, -1f, 1f) : 0f;

            float remappedX = Mathf.Lerp(m_RemapValueMin, m_RemapValueMax, (normalizedX + 1f) * 0.5f);
            float remappedY = Mathf.Lerp(m_RemapValueMin, m_RemapValueMax, (normalizedY + 1f) * 0.5f);

            Vector2 value = new Vector2(remappedX, remappedY);
            CurrentValue = value;

            if (Vector2.Distance(value, m_PreviousValue) < c_ValueChangeTolerance)
                return;

            m_PreviousValue = value;
            m_ValueChanged.Invoke(value);

            // Single-axis mode: use whichever axis is enabled (X takes priority if both)
            if (m_XAxis && !m_YAxis) m_SingleValueChanged.Invoke(remappedX);
            else if (m_YAxis && !m_XAxis) m_SingleValueChanged.Invoke(remappedY);
        }

        #endregion

        #region Private Methods

        private void SetupJoint()
        {
            m_Joint = gameObject.AddComponent<ConfigurableJoint>();
            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.anchor = Vector3.zero;
            m_Joint.connectedAnchor = transform.position;

            // Lock all linear DOF and angular Z (twist)
            m_Joint.xMotion = ConfigurableJointMotion.Locked;
            m_Joint.yMotion = ConfigurableJointMotion.Locked;
            m_Joint.zMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularZMotion = ConfigurableJointMotion.Locked;

            // Angular X and Y limited to ±maxAngle (pitch and yaw of the shaft)
            var angularLimit = new SoftJointLimit { limit = m_MaxAngle };
            m_Joint.angularXMotion = m_XAxis
                ? ConfigurableJointMotion.Limited
                : ConfigurableJointMotion.Locked;
            m_Joint.angularYMotion = m_YAxis
                ? ConfigurableJointMotion.Limited
                : ConfigurableJointMotion.Locked;

            m_Joint.highAngularXLimit = angularLimit;
            m_Joint.lowAngularXLimit  = new SoftJointLimit { limit = -m_MaxAngle };
            m_Joint.angularYLimit     = angularLimit;

            if (m_ReturnToZero)
            {
                var drive = new JointDrive
                {
                    positionSpring = m_SpringForce,
                    positionDamper = m_SpringDamper,
                    maximumForce   = float.MaxValue
                };
                m_Joint.angularXDrive  = drive;
                m_Joint.angularYZDrive = drive;
                m_Joint.targetRotation = Quaternion.identity;
            }
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Log Current Value")]
        private void LogCurrentValue() =>
            Debug.Log($"[PhysicsJoystickInteractable] value={CurrentValue}");
#endif
    }
}
