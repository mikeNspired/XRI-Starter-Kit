// Physics-driven axis drag. Place this script on the draggable Rigidbody.
// The draggable should be a direct child of a static anchor; do not move the assembly during play.
// Place the draggable at the value=0 (start) end; it travels along m_LocalAxis by m_AxisLength.
// The joint is anchored at the MIDPOINT of travel so the body cannot overshoot either end
// (ConfigurableJoint linear limits are symmetric, so a centered anchor bounds both ends).
// The ConfigurableJoint is created at runtime — no manual joint setup in the inspector needed.
// Optional: add XRGrabInteractable to the same GameObject for grab-and-drag interaction (no hand
// physics colliders required). When present, its movementType is forced to VelocityTracking so the
// joint constrains the grab to the axis instead of the hand pulling it free.

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

        [Header("Return To Start")]
        [SerializeField] private bool m_ReturnOnFree = false;
        [SerializeField, Min(0f)] private float m_ReturnSpeed = 0.5f;

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
        // Both trackers start at their rest values so the first FixedUpdate fires no
        // OnDragDistance/OnDragStep events and no snap audio at scene load.
        private float m_PreviousRawDistance;
        private int m_CurrentStep;
        private bool m_IsGrabbed;

        #endregion

        #region Public Properties

        public float Distance { get; private set; }
        public int CurrentStep => m_CurrentStep;
        public bool IsGrabbed => m_IsGrabbed;

        // Match AxisDragInteractable's public events so existing integration code can subscribe.
        public UnityEventFloat OnDragDistance => m_OnDragDistance;
        public UnityEventInt OnDragStep => m_OnDragStep;

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
            // Return to start when free: drive position directly (no grab/velocity-tracking is fighting).
            if (m_ReturnOnFree && !m_IsGrabbed)
                ReturnTowardStart();

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
            Vector3 worldAxis = transform.TransformDirection(axis);

            Vector3 perp = Vector3.Cross(axis, Vector3.up);
            if (perp.sqrMagnitude < 0.01f)
                perp = Vector3.Cross(axis, Vector3.right);
            perp.Normalize();

            m_Joint = gameObject.AddComponent<ConfigurableJoint>();
            m_Joint.autoConfigureConnectedAnchor = false;
            m_Joint.anchor = Vector3.zero;
            // Midpoint anchor — symmetric ±half limit bounds both ends; no overshoot past start/end.
            m_Joint.connectedAnchor = transform.position + worldAxis * (m_AxisLength * 0.5f);

            m_Joint.axis = axis;
            m_Joint.secondaryAxis = perp;
            m_Joint.xMotion = ConfigurableJointMotion.Limited;
            m_Joint.yMotion = ConfigurableJointMotion.Locked;
            m_Joint.zMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularXMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularYMotion = ConfigurableJointMotion.Locked;
            m_Joint.angularZMotion = ConfigurableJointMotion.Locked;

            m_Joint.linearLimit = new SoftJointLimit { limit = m_AxisLength * 0.5f };
        }

        private void ConfigureGrab()
        {
            m_GrabInteractable.movementType = XRGrabInteractable.MovementType.VelocityTracking;
            m_GrabInteractable.throwOnDetach = false;
            m_GrabInteractable.selectEntered.AddListener(OnGrabEntered);
            m_GrabInteractable.selectExited.AddListener(OnGrabExited);
        }

        private void OnGrabEntered(SelectEnterEventArgs _args) => m_IsGrabbed = true;

        private void OnGrabExited(SelectExitEventArgs _args)
        {
            m_IsGrabbed = false;
            // Re-seat on release in BOTH snap modes: in continuous mode the raw-change gate in
            // FixedUpdate goes quiet once the handle settles, so without this a handle released
            // between detents would rest there forever.
            if (m_Steps > 0)
                ApplySnap(m_PreviousRawDistance);
        }

        private void ReturnTowardStart()
        {
            Vector3 startWorld = transform.parent != null
                ? transform.parent.TransformPoint(m_InitialLocalPosition)
                : m_InitialLocalPosition;

            // Already home: do nothing, so a resting handle stays an ordinary pushable body
            // instead of being pinned by a MovePosition + velocity zero every step.
            if ((startWorld - m_Rigidbody.position).sqrMagnitude < 1e-8f)
                return;

            // While returning, cancel only the along-axis velocity so a hand-collider push can
            // still interrupt the return.
            Vector3 worldAxis = transform.TransformDirection(m_LocalAxis.normalized);
            Vector3 velocity = m_Rigidbody.linearVelocity;
            m_Rigidbody.linearVelocity = velocity - Vector3.Dot(velocity, worldAxis) * worldAxis;
            m_Rigidbody.MovePosition(Vector3.MoveTowards(m_Rigidbody.position, startWorld, m_ReturnSpeed * Time.fixedDeltaTime));
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
            }

            // Always re-seat on the detent (not only when the step index changed) so the handle
            // never rests between steps while Distance reports the snapped value.
            Vector3 targetLocal = m_InitialLocalPosition + m_LocalAxis.normalized * snappedDistance;
            m_Rigidbody.MovePosition(transform.parent != null
                ? transform.parent.TransformPoint(targetLocal)
                : targetLocal);
            m_Rigidbody.linearVelocity = Vector3.zero;
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
