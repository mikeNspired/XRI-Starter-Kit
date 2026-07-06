// Physics-driven linear slider. Place this script on the moving handle (Rigidbody required).
// The handle should be a direct child of a static track; do not move the assembly during play.
// Place the handle at the value=0 end of its travel; it slides along local +Z (m_SliderAxis) by
// m_TravelDistance to reach value=1. The joint is anchored at the MIDPOINT of travel so the handle
// cannot overshoot either end of the track (ConfigurableJoint linear limits are symmetric, so a
// centered anchor is what bounds both ends).
// The ConfigurableJoint is created at runtime — no manual joint setup in the inspector needed.
// Optional: add XRGrabInteractable to the same GameObject for grab-and-drag interaction (no hand
// physics colliders required). When present, its movementType is forced to VelocityTracking so the
// joint constrains the grab to the track instead of the hand yanking it free.
// Note: with m_ReturnOnRelease the handle springs to the CENTER of travel (value 0.5), not an end.

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

        [Header("Return Spring (returns to center of travel)")]
        [SerializeField] private bool m_ReturnOnRelease = false;
        [SerializeField, Min(0f)] private float m_SpringForce = 100f;
        [SerializeField, Min(0f)] private float m_SpringDamper = 10f;

        [Header("Events")]
        [SerializeField] private UnityEventFloat m_OnValueChange = new UnityEventFloat();

        private Rigidbody m_Rigidbody;
        private ConfigurableJoint m_Joint;
        private XRGrabInteractable m_GrabInteractable;
        private Vector3 m_InitialLocalPosition;
        private float m_PreviousValue = -1f;
        private bool m_IsGrabbed;

        #endregion

        #region Public Properties

        public float Value { get; private set; }
        public float NormalizedValue { get; private set; }
        public bool IsGrabbed => m_IsGrabbed;

        // Matches XRSlider.OnValueChange so existing integration code can subscribe in code.
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
            Vector3 worldAxis = transform.TransformDirection(slideDir);

            Vector3 perp = Vector3.Cross(slideDir, Vector3.up);
            if (perp.sqrMagnitude < 0.01f)
                perp = Vector3.Cross(slideDir, Vector3.right);
            perp.Normalize();

            m_Joint = gameObject.AddComponent<ConfigurableJoint>();
            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.anchor = Vector3.zero;
            // Anchor at the midpoint of travel: a symmetric ±half limit then bounds BOTH ends of the
            // track exactly, so the handle can never slide off either end.
            m_Joint.connectedAnchor = transform.position + worldAxis * (m_TravelDistance * 0.5f);

            m_Joint.axis = slideDir;
            m_Joint.secondaryAxis = perp;
            m_Joint.xMotion = ConfigurableJointMotion.Limited;
            m_Joint.yMotion = ConfigurableJointMotion.Locked;
            m_Joint.zMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularXMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularYMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularZMotion = ConfigurableJointMotion.Locked;

            m_Joint.linearLimit = new SoftJointLimit { limit = m_TravelDistance * 0.5f };

            if (m_ReturnOnRelease)
                SetSpring(true);
        }

        private void ConfigureGrab()
        {
            // VelocityTracking (not Kinematic): the hand applies a tracking velocity that the joint can
            // constrain. Kinematic would make the body ignore the joint and tear off the track.
            m_GrabInteractable.movementType = XRGrabInteractable.MovementType.VelocityTracking;
            m_GrabInteractable.throwOnDetach = false;
            m_GrabInteractable.selectEntered.AddListener(OnGrabEntered);
            m_GrabInteractable.selectExited.AddListener(OnGrabExited);
        }

        private void OnGrabEntered(SelectEnterEventArgs _args)
        {
            m_IsGrabbed = true;
            // Suspend the return spring while held so it doesn't fight the hand.
            if (m_ReturnOnRelease)
                SetSpring(false);
        }

        private void OnGrabExited(SelectExitEventArgs _args)
        {
            m_IsGrabbed = false;
            if (m_ReturnOnRelease)
                SetSpring(true);
        }

        private void SetSpring(bool _enabled)
        {
            // targetPosition stays at its default (0) = the connected anchor = midpoint of travel,
            // so the drive returns the handle to the center. This avoids ConfigurableJoint's inverted
            // targetPosition sign convention entirely.
            m_Joint.xDrive = _enabled
                ? new JointDrive { positionSpring = m_SpringForce, positionDamper = m_SpringDamper, maximumForce = float.MaxValue }
                : new JointDrive { positionSpring = 0f, positionDamper = 0f, maximumForce = float.MaxValue };
        }

        #endregion

        #region Public Methods

        public void SetValue(float _normalizedValue)
        {
            float clampedT = Mathf.Clamp01(_normalizedValue);
            Vector3 targetLocal = m_InitialLocalPosition + m_SliderAxis.normalized * (clampedT * m_TravelDistance);
            Vector3 targetWorld = transform.parent != null ? transform.parent.TransformPoint(targetLocal) : targetLocal;
            m_Rigidbody.MovePosition(targetWorld);
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Log Current Value")]
        private void LogCurrentValue() =>
            Debug.Log($"[PhysicsSliderInteractable] normalized={NormalizedValue:F3} value={Value:F3} grabbed={m_IsGrabbed}");
#endif
    }
}
