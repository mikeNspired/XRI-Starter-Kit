// Physics-driven axis drag. Place this script on the draggable Rigidbody.
// The draggable should be a direct child of a static anchor; do not move the assembly during play.
// Place the draggable at the value=0 position before entering play mode.
// The ConfigurableJoint is created at runtime — no manual joint setup in the inspector needed.
// Optional: add XRGrabInteractable to the same GameObject for grab-and-drag interaction.
// If XRGrabInteractable is present its movementType is set to VelocityTracking automatically.

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsAxisDragInteractable : MonoBehaviour
    {
        #region Constants

        private const float c_ValueChangeTolerance = 0.0005f;

        #endregion

        #region Private Fields

        [Header("Drag Settings")]
        [SerializeField] private Vector3 m_LocalAxis = Vector3.forward;
        [SerializeField, Min(0.001f)] private float m_AxisLength = 0.3f;
        [SerializeField, Min(0)] private int m_Steps = 0;
        [SerializeField] private bool m_SnapOnlyOnRelease = false;

        [Header("Return Spring")]
        [SerializeField] private bool m_ReturnOnRelease = false;
        [SerializeField, Min(0f)] private float m_SpringForce = 100f;
        [SerializeField, Min(0f)] private float m_SpringDamper = 10f;

        [Header("Snap Audio")]
        [SerializeField] private AudioClip m_SnapAudioClip;
        [SerializeField] private AudioSource m_AudioSource;

        [Header("Events")]
        [SerializeField] private UnityEventFloat m_OnDragDistance = new UnityEventFloat();
        [SerializeField] private UnityEventInt m_OnDragStep = new UnityEventInt();

        private Rigidbody m_Rigidbody;
        private ConfigurableJoint m_Joint;
        private XRGrabInteractable m_GrabInteractable;
        private Vector3 m_InitialLocalPosition;
        private float m_PreviousRawDistance = -1f;
        private int m_CurrentStep = -1;
        private bool m_IsGrabbed;

        #endregion

        #region Public Properties

        public float Distance { get; private set; }
        public int CurrentStep => m_CurrentStep;
        public bool IsGrabbed => m_IsGrabbed;

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
            float rawDistance = Mathf.Clamp(Vector3.Dot(localDelta, m_LocalAxis.normalized), 0f, m_AxisLength);

            if (Mathf.Abs(rawDistance - m_PreviousRawDistance) < c_ValueChangeTolerance)
                return;

            m_PreviousRawDistance = rawDistance;

            if (m_Steps > 0 && !m_SnapOnlyOnRelease)
                ApplySnap(rawDistance);
            else
            {
                Distance = rawDistance;
                m_OnDragDistance.Invoke(Distance);
            }
        }

        #endregion

        #region Private Methods

        private void SetupJoint()
        {
            Vector3 axis = m_LocalAxis.normalized;

            Vector3 perp = Vector3.Cross(axis, Vector3.up);
            if (perp.sqrMagnitude < 0.01f)
                perp = Vector3.Cross(axis, Vector3.right);
            perp.Normalize();

            m_Joint = gameObject.AddComponent<ConfigurableJoint>();
            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.anchor = Vector3.zero;
            m_Joint.connectedAnchor = transform.position;

            m_Joint.axis = axis;
            m_Joint.secondaryAxis = perp;
            m_Joint.xMotion = ConfigurableJointMotion.Limited;
            m_Joint.yMotion = ConfigurableJointMotion.Locked;
            m_Joint.zMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularXMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularYMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularZMotion = ConfigurableJointMotion.Locked;

            m_Joint.linearLimit = new SoftJointLimit { limit = m_AxisLength };

            if (m_ReturnOnRelease)
                SetSpring(true);
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
            if (m_ReturnOnRelease)
                SetSpring(false);
        }

        private void OnGrabExited(SelectExitEventArgs _args)
        {
            m_IsGrabbed = false;
            if (m_ReturnOnRelease)
                SetSpring(true);
            if (m_Steps > 0 && m_SnapOnlyOnRelease)
                ApplySnap(m_PreviousRawDistance);
        }

        private void SetSpring(bool _enabled)
        {
            m_Joint.xDrive = _enabled
                ? new JointDrive { positionSpring = m_SpringForce, positionDamper = m_SpringDamper, maximumForce = float.MaxValue }
                : new JointDrive { positionSpring = 0f, positionDamper = 0f, maximumForce = float.MaxValue };
        }

        private void ApplySnap(float _rawDistance)
        {
            if (m_Steps <= 0) return;

            float stepLength = m_AxisLength / m_Steps;
            int newStep = Mathf.RoundToInt(_rawDistance / stepLength);
            newStep = Mathf.Clamp(newStep, 0, m_Steps);

            float snappedDistance = newStep * stepLength;
            Distance = snappedDistance;
            m_OnDragDistance.Invoke(snappedDistance);

            if (newStep != m_CurrentStep)
            {
                m_CurrentStep = newStep;
                m_OnDragStep.Invoke(m_CurrentStep);
                PlaySnapAudio();

                Vector3 targetLocal = m_InitialLocalPosition + m_LocalAxis.normalized * snappedDistance;
                m_Rigidbody.MovePosition(transform.parent != null
                    ? transform.parent.TransformPoint(targetLocal)
                    : targetLocal);
                m_Rigidbody.linearVelocity = Vector3.zero;
            }
        }

        private void PlaySnapAudio()
        {
            if (m_SnapAudioClip == null || m_AudioSource == null) return;
            m_AudioSource.PlayOneShot(m_SnapAudioClip);
        }

        #endregion

        #region Public Methods

        public void SetDistance(float _distance)
        {
            float clamped = Mathf.Clamp(_distance, 0f, m_AxisLength);
            Vector3 targetLocal = m_InitialLocalPosition + m_LocalAxis.normalized * clamped;
            m_Rigidbody.MovePosition(transform.parent != null
                ? transform.parent.TransformPoint(targetLocal)
                : targetLocal);
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Log Current Value")]
        private void LogCurrentValue() =>
            Debug.Log($"[PhysicsAxisDragInteractable] distance={Distance:F4} step={m_CurrentStep} grabbed={m_IsGrabbed}");
#endif
    }
}
