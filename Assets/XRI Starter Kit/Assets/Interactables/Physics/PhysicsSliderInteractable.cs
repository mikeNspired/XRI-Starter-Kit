// Physics-driven linear slider. Place this script on the moving handle (Rigidbody required).
// The handle should be a direct child of a static track; do not move the assembly during play.
// Place the handle at the value=0 position before entering play mode. Drag direction is local +Z by default.
// The ConfigurableJoint is created at runtime — no manual joint setup in the inspector needed.

using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsSliderInteractable : MonoBehaviour
    {
        #region Constants

        private const float c_ValueChangeTolerance = 0.001f;

        #endregion

        #region Private Fields

        [Header("Slider Settings")]
        [SerializeField] private Vector3 m_SliderAxis = Vector3.forward;
        [SerializeField, Min(0.001f)] private float m_TravelDistance = 0.2f;
        [SerializeField] private float m_RemapValueMin = 0f;
        [SerializeField] private float m_RemapValueMax = 1f;

        [Header("Return Spring")]
        [SerializeField] private bool m_ReturnOnRelease = false;
        [SerializeField, Min(0f)] private float m_SpringForce = 100f;
        [SerializeField, Min(0f)] private float m_SpringDamper = 10f;

        [Header("Events")]
        [SerializeField] private UnityEventFloat m_OnValueChange = new UnityEventFloat();

        private Rigidbody m_Rigidbody;
        private ConfigurableJoint m_Joint;
        private Vector3 m_InitialLocalPosition;
        private float m_PreviousValue = -1f;

        #endregion

        #region Public Properties

        public float Value { get; private set; }
        public float NormalizedValue { get; private set; }

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
            float displacement = Vector3.Dot(localDelta, m_SliderAxis.normalized);
            float normalized = Mathf.Clamp01(displacement / m_TravelDistance);
            float remapped = Mathf.Lerp(m_RemapValueMin, m_RemapValueMax, normalized);

            NormalizedValue = normalized;
            Value = remapped;

            if (Mathf.Abs(normalized - m_PreviousValue) < c_ValueChangeTolerance)
                return;

            m_PreviousValue = normalized;
            m_OnValueChange.Invoke(remapped);
        }

        #endregion

        #region Private Methods

        private void SetupJoint()
        {
            Vector3 slideDir = m_SliderAxis.normalized;

            Vector3 perp = Vector3.Cross(slideDir, Vector3.up);
            if (perp.sqrMagnitude < 0.01f)
                perp = Vector3.Cross(slideDir, Vector3.right);
            perp.Normalize();

            m_Joint = gameObject.AddComponent<ConfigurableJoint>();
            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.anchor = Vector3.zero;
            m_Joint.connectedAnchor = transform.position;

            m_Joint.axis = slideDir;
            m_Joint.secondaryAxis = perp;
            m_Joint.xMotion = ConfigurableJointMotion.Limited;
            m_Joint.yMotion = ConfigurableJointMotion.Locked;
            m_Joint.zMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularXMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularYMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularZMotion = ConfigurableJointMotion.Locked;

            // Symmetric limit — handle placed at value=0 (start) can travel ±TravelDistance.
            // Only the 0→+TravelDistance range maps to valid values; clamped in FixedUpdate.
            m_Joint.linearLimit = new SoftJointLimit { limit = m_TravelDistance };

            if (m_ReturnOnRelease)
            {
                m_Joint.xDrive = new JointDrive
                {
                    positionSpring = m_SpringForce,
                    positionDamper = m_SpringDamper,
                    maximumForce = float.MaxValue
                };
            }
        }

        #endregion

        #region Public Methods

        public void SetValue(float _normalizedValue)
        {
            float clampedT = Mathf.Clamp01(_normalizedValue);
            Vector3 targetLocal = m_InitialLocalPosition + m_SliderAxis.normalized * (clampedT * m_TravelDistance);
            m_Rigidbody.MovePosition(transform.parent.TransformPoint(targetLocal));
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Log Current Value")]
        private void LogCurrentValue() =>
            Debug.Log($"[PhysicsSliderInteractable] normalized={NormalizedValue:F3} value={Value:F3}");
#endif
    }
}
