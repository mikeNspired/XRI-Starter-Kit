using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class Door : MonoBehaviour
    {
        [SerializeField]
        HingeJoint m_DoorJoint;

        [SerializeField]
        [Tooltip("Transform joint that pulls a door to follow an interactor")]
        TransformJoint m_DoorPuller;

        [SerializeField]
        GameObject m_KeyKnob;

        [SerializeField]
        float m_HandleOpenValue = 0.1f;

        [SerializeField]
        float m_HandleCloseValue = 0.5f;

        [SerializeField]
        float m_HingeCloseAngle = 5.0f;

        [SerializeField]
        float m_KeyLockValue = 0.9f;

        [SerializeField]
        float m_KeyUnlockValue = 0.1f;

        [SerializeField]
        float m_KeyPullDistance = 0.1f;

        [SerializeField] bool m_Locked = false;

        [SerializeField]
        [Tooltip("Events to fire when the door is locked.")]
        UnityEvent m_OnLock = new UnityEvent();

        [SerializeField]
        [Tooltip("Events to fire when the door is unlocked.")]
        UnityEvent m_OnUnlock = new UnityEvent();

        JointLimits m_OpenDoorLimits;
        JointLimits m_ClosedDoorLimits;
        bool m_Closed = false;
        float m_LastHandleValue = 1.0f;


        GameObject m_KeySocket;
        IXRSelectInteractable m_Key;

        XRBaseInteractor m_KnobInteractor;
        Transform m_KnobInteractorAttachTransform;

        /// <summary>
        /// Events to fire when the door is locked.
        /// </summary>
        public UnityEvent OnLock => m_OnLock;

        /// <summary>
        /// Events to fire when the door is unlocked.
        /// </summary>
        public UnityEvent OnUnlock => m_OnUnlock;

        private Vector3 m_StartingDoorJointPosition = Vector3.one;
        private Rigidbody m_JointRigidBody;
        void Start()
        {
            m_OpenDoorLimits = m_DoorJoint.limits;
            m_ClosedDoorLimits = m_OpenDoorLimits;
            m_ClosedDoorLimits.min = 0.0f;
            m_ClosedDoorLimits.max = 0.0f;
            m_DoorJoint.limits = m_ClosedDoorLimits;
            m_KeyKnob.SetActive(false);
            m_Closed = true;

            m_JointRigidBody = m_DoorJoint.GetComponent<Rigidbody>();
            SetDoorPosition();
        }

        private async void SetDoorPosition()
        {
            //Set position after hinge joint has settled into joint
            while (!m_JointRigidBody.IsSleeping()) await Task.Yield();
            m_StartingDoorJointPosition = m_DoorJoint.transform.localPosition;
        }

        void Update()
        {
            ForceDoorPosition();
            
            // If the door is open, keep track of the hinge joint and see if it enters a state where it should close again
            if (!m_Closed)
            {
                if (m_LastHandleValue < m_HandleCloseValue)
                    return;

                if (Mathf.Abs(m_DoorJoint.angle) < m_HingeCloseAngle)
                {
                    m_DoorJoint.limits = m_ClosedDoorLimits;
                    m_Closed = true;
                }
            }

            if (m_KnobInteractor != null && m_KnobInteractorAttachTransform != null)
            {
                var distance = (m_KnobInteractorAttachTransform.position - m_KeyKnob.transform.position).magnitude;

                // If over threshold, break and grant the key back to the interactor
                if (distance > m_KeyPullDistance)
                {
                    var newKeyInteractor = m_KnobInteractor;
                    m_KeySocket.SetActive(true);
                    m_Key.transform.gameObject.SetActive(true);
                    newKeyInteractor.interactionManager.SelectEnter(newKeyInteractor, m_Key);
                    m_KeyKnob.SetActive(false);
                }
            }
        }
     
        private void ForceDoorPosition()
        {
            if (m_StartingDoorJointPosition == Vector3.one || m_JointRigidBody.IsSleeping()) return;
            m_DoorJoint.transform.localPosition = m_StartingDoorJointPosition;
        }

        public void BeginDoorPulling(SelectEnterEventArgs args)
        {
            m_DoorPuller.ConnectedBody = args.interactorObject.GetAttachTransform(args.interactableObject);
            m_DoorPuller.enabled = true;
        }

        public void EndDoorPulling()
        {
            m_DoorPuller.enabled = false;
            m_DoorPuller.ConnectedBody = null;
        }

        public void DoorHandleUpdate(float handleValue)
        {
            m_LastHandleValue = handleValue;

            if (!m_Closed || m_Locked)
                return;

            if (handleValue < m_HandleOpenValue)
            {
                m_DoorJoint.limits = m_OpenDoorLimits;
                m_Closed = false;
            }
        }

        public void KeyDropUpdate(SelectEnterEventArgs args)
        {
            m_KeySocket = args.interactorObject.transform.gameObject;
            m_Key = args.interactableObject;
            m_KeySocket.SetActive(false);
            m_Key.transform.gameObject.SetActive(false);
            m_KeyKnob.SetActive(true);
        }

        public void KeyUpdate(float keyValue)
        {
            if (!m_Locked && keyValue > m_KeyLockValue)
            {
                m_Locked = true;
                m_OnLock.Invoke();
            }

            if (m_Locked && keyValue < m_KeyUnlockValue)
            {
                m_Locked = false;
                m_OnUnlock.Invoke();
            }
        }

        public void KeyLockSelect(SelectEnterEventArgs args)
        {
            m_KnobInteractor = args.interactorObject as XRBaseInteractor;
            m_KnobInteractorAttachTransform = args.interactorObject.GetAttachTransform(args.interactableObject);
        }

        public void KeyLockDeselect(SelectExitEventArgs args)
        {
            m_KnobInteractor = null;
            m_KnobInteractorAttachTransform = null;
        }
    }
}
