using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    /// <summary>
    /// Custom interactable that can be dragged along an axis. Can either be continuous or snap to integer steps.
    /// </summary>
    public class AxisDragInteractable : XRBaseInteractable
    {
        [Tooltip("The Rigidbody that will be moved. If null will try to grab one on that object or its children")]
        public Rigidbody MovingRigidbody;

        public Vector3 LocalAxis;
        public float AxisLength;

        [Tooltip("If 0, then this is a float [0,1] range slider, otherwise there is an integer slider")]
        public int Steps = 0;

        public bool SnapOnlyOnRelease = true;

        public bool ReturnOnFree;
        public float ReturnSpeed;

        public AudioClip SnapAudioClip;
        public AudioSource AudioSource;
        public UnityEventFloat OnDragDistance;
        public UnityEventInt OnDragStep;

        Vector3 m_EndPoint;
        Vector3 m_StartPoint;
        Vector3 m_GrabbedOffset;
        float m_CurrentDistance;
        int m_CurrentStep;
        XRBaseInteractor m_GrabbingInteractor;

        float m_StepLength;

        // Start is called before the first frame update
        void Start()
        {
            LocalAxis.Normalize();

            //Length can't be negative, a negative length just mean an inverted axis, so fix that
            if (AxisLength < 0)
            {
                LocalAxis *= -1;
                AxisLength *= -1;
            }

            if (Steps == 0)
            {
                m_StepLength = 0.0f;
            }
            else
            {
                m_StepLength = AxisLength / Steps;
            }

            m_StartPoint = transform.position;
            m_EndPoint = transform.position + transform.TransformDirection(LocalAxis) * AxisLength;

            if (MovingRigidbody == null)
            {
                MovingRigidbody = GetComponentInChildren<Rigidbody>();
            }

            m_CurrentStep = 0;
            AudioSource.clip = SnapAudioClip;
        }

        private void OnValidate()
        {
            if (!AudioSource)
                AudioSource = GetComponent<AudioSource>();
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (isSelected)
            {
                if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed)
                {
                    Vector3 WorldAxis = transform.TransformDirection(LocalAxis);

                    Vector3 distance = m_GrabbingInteractor.transform.position - transform.position - m_GrabbedOffset;
                    float projected = Vector3.Dot(distance, WorldAxis);

                    //ajust projected to clamp it to steps if there is steps
                    if (Steps != 0 && !SnapOnlyOnRelease)
                    {
                        int steps = Mathf.RoundToInt(projected / m_StepLength);
                        projected = steps * m_StepLength;
                    }

                    Vector3 targetPoint;
                    if (projected > 0)
                        targetPoint = Vector3.MoveTowards(transform.position, m_EndPoint, projected);
                    else
                        targetPoint = Vector3.MoveTowards(transform.position, m_StartPoint, -projected);

                    if (Steps > 0)
                    {
                        int posStep = Mathf.RoundToInt((targetPoint - m_StartPoint).magnitude / m_StepLength);
                        if (posStep != m_CurrentStep)
                        {
                            AudioSource.Play();
                            OnDragStep.Invoke(posStep);
                        }

                        m_CurrentStep = posStep;
                    }

                    OnDragDistance.Invoke((targetPoint - m_StartPoint).magnitude);

                    Vector3 move = targetPoint - transform.position;

                    if (MovingRigidbody != null)
                        MovingRigidbody.MovePosition(MovingRigidbody.position + move);
                    else
                        transform.position = transform.position + move;
                }
            }
            else
            {
                if (ReturnOnFree)
                {
                    Vector3 targetPoint = Vector3.MoveTowards(transform.position, m_StartPoint, ReturnSpeed * Time.deltaTime);
                    Vector3 move = targetPoint - transform.position;

                    if (MovingRigidbody != null)
                        MovingRigidbody.MovePosition(MovingRigidbody.position + move);
                    else
                        transform.position = transform.position + move;
                }
            }
        }

        protected override void OnSelectEntered(XRBaseInteractor interactor)
        {
            m_GrabbedOffset = interactor.transform.position - transform.position;
            m_GrabbingInteractor = interactor;
            base.OnSelectEntered(interactor);
        }

        protected override void OnSelectExited(XRBaseInteractor interactor)
        {
            base.OnSelectExited(interactor);

            if (SnapOnlyOnRelease && Steps != 0)
            {
                float dist = (transform.position - m_StartPoint).magnitude;
                int step = Mathf.RoundToInt(dist / m_StepLength);
                dist = step * m_StepLength;

                transform.position = m_StartPoint + transform.TransformDirection(LocalAxis) * dist;

                if (step != m_CurrentStep)
                {
                    OnDragStep.Invoke(step);
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector3 end = transform.position + transform.TransformDirection(LocalAxis.normalized) * AxisLength;

            Gizmos.DrawLine(transform.position, end);
            Gizmos.DrawSphere(end, 0.01f);
        }
    }
}