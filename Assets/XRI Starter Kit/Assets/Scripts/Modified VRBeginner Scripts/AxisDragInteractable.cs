using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Custom interactable that can be dragged along an axis. 
    /// Can either be continuous or snap to integer steps.
    /// </summary>
    public class AxisDragInteractable : XRBaseInteractable
    {
        [Header("Motion Settings")]
        [Tooltip("The Rigidbody that will be moved. If null, the first Rigidbody in children will be used.")]
        public Rigidbody MovingRigidbody;

        [Tooltip("Local axis along which the object can be dragged.")]
        public Vector3 LocalAxis;

        [Tooltip("Maximum distance along the axis the object can travel.")]
        public float AxisLength;

        [Tooltip("Number of discrete steps. If zero, it behaves like a continuous slider.")]
        public int Steps = 0;

        [Tooltip("If true, snapping to steps only happens when the grip is released.")]
        public bool SnapOnlyOnRelease = true;

        [Header("Return Settings")]
        [Tooltip("If true, the object will return to start when not being grabbed.")]
        public bool ReturnOnFree;
        public float ReturnSpeed = 1f;

        [Header("Audio & Events")]
        public AudioClip SnapAudioClip;
        public AudioSource AudioSource;
        public UnityEventFloat OnDragDistance;
        public UnityEventInt OnDragStep;

        private Vector3 m_EndPoint;
        private Vector3 m_StartPoint;
        private Vector3 m_GrabbedOffset;
        private float m_StepLength;
        private int m_CurrentStep;
        private XRBaseInteractor m_GrabbingInteractor;

        void Start()
        {
            // Normalize the specified local axis
            LocalAxis.Normalize();

            // If AxisLength is negative, flip the axis direction & make length positive
            if (AxisLength < 0)
            {
                LocalAxis *= -1;
                AxisLength *= -1;
            }

            // Calculate how far one "step" is
            m_StepLength = Steps == 0 ? 0f : (AxisLength / Steps);

            // Cache start & end points in world space
            m_StartPoint = transform.position;
            m_EndPoint = transform.position + transform.TransformDirection(LocalAxis) * AxisLength;

            // If no rigidbody was specified, try to find one
            if (!MovingRigidbody)
                MovingRigidbody = GetComponentInChildren<Rigidbody>();

            m_CurrentStep = 0;

            // Setup audio source
            if (AudioSource && SnapAudioClip)
                AudioSource.clip = SnapAudioClip;
        }

        void OnValidate()
        {
            if (!AudioSource)
                AudioSource = GetComponent<AudioSource>();
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            // Only process if actively held
            if (isSelected)
            {
                if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed)
                {
                    var worldAxis = transform.TransformDirection(LocalAxis);
                    var distance = m_GrabbingInteractor.transform.position - transform.position - m_GrabbedOffset;
                    var projected = Vector3.Dot(distance, worldAxis);

                    // If we have steps & snap is not only on release, snap continuously
                    if (Steps != 0 && !SnapOnlyOnRelease)
                    {
                        int steps = Mathf.RoundToInt(projected / m_StepLength);
                        projected = steps * m_StepLength;
                    }

                    // Determine the final target point, clamped between start & end
                    Vector3 targetPoint;
                    if (projected > 0)
                        targetPoint = Vector3.MoveTowards(transform.position, m_EndPoint, projected);
                    else
                        targetPoint = Vector3.MoveTowards(transform.position, m_StartPoint, -projected);

                    // If we have discrete steps, fire event when crossing a step boundary
                    if (Steps > 0)
                    {
                        var posStep = Mathf.RoundToInt((targetPoint - m_StartPoint).magnitude / m_StepLength);
                        if (posStep != m_CurrentStep)
                        {
                            AudioSource?.Play();
                            OnDragStep.Invoke(posStep);
                        }
                        m_CurrentStep = posStep;
                    }

                    // Fire distance event
                    OnDragDistance.Invoke((targetPoint - m_StartPoint).magnitude);

                    // Move the object or its Rigidbody
                    var move = targetPoint - transform.position;
                    if (MovingRigidbody)
                        MovingRigidbody.MovePosition(MovingRigidbody.position + move);
                    else
                        transform.position += move;
                }
            }
            else
            {
                // If not being grabbed & configured to return, move towards start
                if (ReturnOnFree)
                {
                    var targetPoint = Vector3.MoveTowards(transform.position, m_StartPoint, ReturnSpeed * Time.deltaTime);
                    var move = targetPoint - transform.position;
                    if (MovingRigidbody)
                        MovingRigidbody.MovePosition(MovingRigidbody.position + move);
                    else
                        transform.position += move;
                }
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Record offset between the object's position and the grab point
            m_GrabbedOffset = args.interactorObject.transform.position - transform.position;
            m_GrabbingInteractor = args.interactorObject as XRBaseInteractor;
            base.OnSelectEntered(args);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            // If snapping only on release, then snap to the nearest step
            if (SnapOnlyOnRelease && Steps != 0)
            {
                var dist = (transform.position - m_StartPoint).magnitude;
                int step = Mathf.RoundToInt(dist / m_StepLength);
                dist = step * m_StepLength;

                transform.position = m_StartPoint + transform.TransformDirection(LocalAxis) * dist;

                if (step != m_CurrentStep)
                    OnDragStep.Invoke(step);
            }
        }

        void OnDrawGizmosSelected()
        {
            // Draw a line & sphere to illustrate the drag axis in the Editor
            var end = transform.position + transform.TransformDirection(LocalAxis.normalized) * AxisLength;
            Gizmos.DrawLine(transform.position, end);
            Gizmos.DrawSphere(end, 0.01f);
        }
    }
}
