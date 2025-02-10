using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(HingeJoint))]
    public class Door : MonoBehaviour
    {
        [Header("Door Joint & Puller")]
        [Tooltip("Reference to the HingeJoint controlling the door.")]
        [SerializeField] private HingeJoint m_DoorJoint;

        [Tooltip("Optional TransformJoint (or similar) that lets you physically pull the door once unlocked.")]
        [SerializeField] private TransformJoint m_DoorPuller;

        [Header("Handle & Angle Thresholds")]
        [Tooltip("Knob value below which the door unlocks (i.e. handle turned far enough to open).")]
        [SerializeField] private float m_HandleOpenValue = 0.1f;

        [Tooltip("Knob value above which the door can re-lock when near closed.")]
        [SerializeField] private float m_HandleCloseValue = 0.5f;

        [Tooltip("Angle (degrees) below which we consider the door 'closed' so it can lock.")]
        [SerializeField] private float m_HingeCloseAngle = 5.0f;

        [Header("Initial Door State")]
        [Tooltip("If true, door starts opened. If false, starts fully closed.")]
        [SerializeField] private bool startOpened = false;

        [Header("Knobs (Both Stay in Sync)")]
        [Tooltip("XRKnob for front handle (0 = handle turned open, 1 = handle closed).")]
        [SerializeField] private XRKnob m_FrontKnob;

        [Tooltip("XRKnob for back handle (0 = handle turned open, 1 = handle closed).")]
        [SerializeField] private XRKnob m_BackKnob;

        [Header("Auto-Close Spring Settings")]
        [Tooltip("If true, hinge uses a spring to help the door auto-close when near closed.")]
        [SerializeField] private bool m_UseSpringToClose = true;

        [Tooltip("Spring force pulling the door closed when near the hingeCloseAngle.")]
        [SerializeField] private float m_SpringForce = 150f;

        [Tooltip("Damping factor to reduce door oscillations.")]
        [SerializeField] private float m_SpringDamper = 20f;

        [Tooltip("If the door angle is below this extra threshold, we fully lock it (no more swinging).")]
        [SerializeField] private float m_LockWhenAngleLessThan = 2f;

        [SerializeField] private AudioRandomize lockDoorSound;
        [SerializeField] private AudioRandomize unlockDoorSound;

        // Internal
        private JointLimits m_OpenDoorLimits;   // The original hinge joint limits as "open"
        private JointLimits m_ClosedDoorLimits; // The "closed" hinge limits
        private bool m_Closed;                 // Tracks whether the door is currently locked
        private Rigidbody m_DoorRigidBody;
        private Vector3 m_StartingLocalPos = Vector3.one;

        void Start()
        {
            // Safety check
            if (!m_DoorJoint)
                m_DoorJoint = GetComponent<HingeJoint>();

            m_DoorRigidBody = m_DoorJoint.GetComponent<Rigidbody>();
            if (!m_DoorRigidBody)
                m_DoorRigidBody = m_DoorJoint.gameObject.AddComponent<Rigidbody>();

            // Store the current (inspector) hinge limits as the "open door" range
            m_OpenDoorLimits = m_DoorJoint.limits;

            // Create a "closed" limit (0..0), i.e. locked
            m_ClosedDoorLimits = m_OpenDoorLimits;
            m_ClosedDoorLimits.min = 0f;
            m_ClosedDoorLimits.max = 0f;

            // Start by recording the door pivot’s local position after it settles
            SetDoorStartingPosition();

            // Force the door either open or closed on start
            if (startOpened)
            {
                // Set the joint to open limits
                m_DoorJoint.limits = m_OpenDoorLimits;
                m_Closed = false;

                // Force knobs to "open" position (0)
                if (m_FrontKnob) m_FrontKnob.Value = 0f;
                if (m_BackKnob)  m_BackKnob.Value  = 0f;

                // Also place the door visually at an "open" angle
                MoveDoorToOpenAngle();
            }
            else
            {
                // Lock the joint
                m_DoorJoint.limits = m_ClosedDoorLimits;
                m_Closed = true;

                // Force knobs to "closed" position (1)
                if (m_FrontKnob) m_FrontKnob.Value = 1f;
                if (m_BackKnob)  m_BackKnob.Value  = 1f;

                // Visually put the door at angle 0 (fully closed)
                m_DoorRigidBody.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }

            // Ensure puller is disabled initially
            if (m_DoorPuller)
            {
                m_DoorPuller.enabled = false;
                m_DoorPuller.ConnectedBody = null;
            }

            // If using knobs, hook up their select events so we can do door pulling
            if (m_FrontKnob)
            {
                m_FrontKnob.selectEntered.AddListener(OnKnobGrab);
                m_FrontKnob.selectExited.AddListener(OnKnobRelease);
            }
            if (m_BackKnob)
            {
                m_BackKnob.selectEntered.AddListener(OnKnobGrab);
                m_BackKnob.selectExited.AddListener(OnKnobRelease);
            }
        }

        private async void SetDoorStartingPosition()
        {
            // Wait until the Rigidbody has settled
            while (!m_DoorRigidBody.IsSleeping())
                await Task.Yield();

            // Record local position so we can "snap" the door pivot if drifting
            m_StartingLocalPos = m_DoorJoint.transform.localPosition;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Main update loop
        // ─────────────────────────────────────────────────────────────────────────────

        void FixedUpdate()
        {
            // Keep door pivot from drifting if physically asleep
            ForceDoorPivotPosition();

            // 1) Sync both knobs so they always share the same value
            SyncKnobValues();

            // 2) Determine the final handle value to decide unlocking/locking
            float handleValue = m_FrontKnob 
                ? m_FrontKnob.Value 
                : (m_BackKnob ? m_BackKnob.Value : 1f);

            // 3) Unlock if handle turned enough
            if (m_Closed && handleValue < m_HandleOpenValue)
                UnlockDoor();

            // 4) Possibly lock if handle is high enough and door is near closed
            if (!m_Closed && handleValue >= m_HandleCloseValue)
            {
                if (Mathf.Abs(m_DoorJoint.angle) < m_HingeCloseAngle)
                    LockDoor();
            }

            // 5) If using a spring, auto‐close if near the closed angle
            if (m_UseSpringToClose)
                UpdateHingeSpring(handleValue);
        }

        private void ForceDoorPivotPosition()
        {
            if (m_StartingLocalPos == Vector3.one || !m_DoorRigidBody.IsSleeping())
                return;

            // Maintain that anchor pivot so the hinge doesn't drift
            m_DoorJoint.transform.localPosition = m_StartingLocalPos;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Knob Sync
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Makes the two knobs always have the same Value.
        /// If one is grabbed and the other is not, we let the grabbed knob drive the other.
        /// If both are grabbed, we take the average.
        /// If neither is grabbed, we leave them as is (or you could do something else).
        /// </summary>
        private void SyncKnobValues()
        {
            if (m_FrontKnob == null || m_BackKnob == null)
                return;

            bool frontGrabbed = m_FrontKnob.isSelected;
            bool backGrabbed  = m_BackKnob.isSelected;

            if (frontGrabbed && !backGrabbed)
            {
                // Front is driver
                m_BackKnob.Value = m_FrontKnob.Value;
            }
            else if (backGrabbed && !frontGrabbed)
            {
                // Back is driver
                m_FrontKnob.Value = m_BackKnob.Value;
            }
            else if (frontGrabbed && backGrabbed)
            {
                // Both are grabbed: pick an average
                float avg = 0.5f * (m_FrontKnob.Value + m_BackKnob.Value);
                m_FrontKnob.Value = avg;
                m_BackKnob.Value  = avg;
            }
            // else: neither is grabbed => do nothing special
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Lock / Unlock
        // ─────────────────────────────────────────────────────────────────────────────

        private void UnlockDoor()
        {
            m_DoorJoint.limits = m_OpenDoorLimits;
            m_Closed = false;
            // turn off the hinge spring if any
            m_DoorJoint.useSpring = false;
            unlockDoorSound.Play();
        }

        private void LockDoor()
        {
            m_DoorJoint.limits = m_ClosedDoorLimits;
            m_Closed = true;
            // also disable the hinge spring
            m_DoorJoint.useSpring = false;
            lockDoorSound.Play();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Spring / Auto-Close
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// If the door is near closed, apply a hinge spring so it pulls shut.
        /// Then, if the door gets *very* close to zero angle (e.g. under 2 degrees), fully lock it.
        /// </summary>
        private void UpdateHingeSpring(float handleValue)
        {
            // If the handle is actually in 'open' range, we do nothing
            if (handleValue < m_HandleCloseValue || m_Closed)
            {
                m_DoorJoint.useSpring = false;
                return;
            }

            float currentAngle = Mathf.Abs(m_DoorJoint.angle);

            // We'll enable the hinge spring if the door is within ~30 degrees of closed
            float springThreshold = 30f;
            if (currentAngle < springThreshold)
            {
                m_DoorJoint.useSpring = true;

                JointSpring spring = m_DoorJoint.spring;
                spring.spring = m_SpringForce;
                spring.damper = m_SpringDamper;
                spring.targetPosition = 0f;  // tries to rotate the door to angle=0
                m_DoorJoint.spring = spring;

                // If it is super close, we can forcibly lock to remove that final wiggle
                if (currentAngle < m_LockWhenAngleLessThan)
                {
                    LockDoor();
                }
            }
            else
            {
                m_DoorJoint.useSpring = false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Move door to open angle on Start
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Places the door transform at a "fully open" angle based on whichever limit 
        /// has the larger absolute angle. Adjust if your door geometry differs.
        /// </summary>
        private void MoveDoorToOpenAngle()
        {
            float openAngle = 0f;
            float absMin = Mathf.Abs(m_OpenDoorLimits.min);
            float absMax = Mathf.Abs(m_OpenDoorLimits.max);

            // We'll pick the limit with the bigger range in absolute terms
            if (absMax > absMin)
                openAngle = m_OpenDoorLimits.max;
            else
                openAngle = m_OpenDoorLimits.min;

            // Snap the transform
            m_DoorRigidBody.transform.localRotation = Quaternion.Euler(0, openAngle, 0);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Knob Grab / Release -> door pulling
        // ─────────────────────────────────────────────────────────────────────────────

        private void OnKnobGrab(SelectEnterEventArgs args)
        {
            if (!m_DoorPuller) 
                return;

            // Connect to the XR Interactor's attach point, so we can physically pull door
            m_DoorPuller.ConnectedBody = args.interactorObject.GetAttachTransform(args.interactableObject);
            m_DoorPuller.enabled = true;
        }

        private void OnKnobRelease(SelectExitEventArgs args)
        {
            if (m_DoorPuller)
            {
                m_DoorPuller.enabled = false;
                m_DoorPuller.ConnectedBody = null;
            }

            // If you want to auto‐reset the knob to closed (1) upon release, do it here.
            // But since we are *always* syncing knobs, either knob's value in FixedUpdate 
            // will get overwritten if the other is still grabbed, etc.
            // If you specifically want to forcibly revert the *released* knob:
            XRKnob justReleasedKnob = args.interactableObject.transform.GetComponent<XRKnob>();
            if (justReleasedKnob)
            {
                // We forcibly set that knob to 1 (closed)
                justReleasedKnob.Value = 1f;
            }
        }
    }
}
