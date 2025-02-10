using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static Unity.Mathematics.math;

namespace MikeNspired.XRIStarterKit
{
    public class XRJoystick : MonoBehaviour
    {
        [SerializeField] private XRBaseInteractable xrGrabInteractable = null;
        [SerializeField] private Transform rotationPoint = null;
        [SerializeField] private float maxAngle = 60;
        [SerializeField] private float shaftLength = .2f;
        [SerializeField] private bool returnToStartOnRelease = true;
        [SerializeField] private float returnSpeed = 5;
        [SerializeField] private Vector2 startingPosition = Vector2.zero;
        [SerializeField] private Vector2 returnToPosition = Vector2.zero;
        [SerializeField] private bool xAxis = true, yAxis = true;
        [SerializeField] private float remapValueMin = -1, remapValueMax = 1;
        [SerializeField] private bool InvokeEventsAtStart;

        private Transform hand, originalPositionTracker;
        public Vector2 CurrentValue { get; private set; }
        public Vector2 RemapValue { get; private set; }

        public UnityEventVector2 ValueChanged;
        public UnityEventFloat SingleValueChanged;
        private Vector3 handOffSetFromStartOfGrab;

        private void Start()
        {
            OnValidate();

            originalPositionTracker = new GameObject("originalPositionTracker").transform;
            originalPositionTracker.parent = transform.parent;
            originalPositionTracker.localPosition = transform.localPosition;
            originalPositionTracker.localRotation = transform.localRotation;

            xrGrabInteractable.selectEntered.AddListener(OnGrab);
            xrGrabInteractable.selectExited.AddListener((x) => hand = null);
            xrGrabInteractable.selectExited.AddListener((x) => StartCoroutine(ReturnToZero()));
            if (InvokeEventsAtStart)
                InvokeUnityEvents();
        }

        private void OnValidate()
        {
            if (!xrGrabInteractable)
                xrGrabInteractable = GetComponent<XRBaseInteractable>();

            if(rotationPoint)
                SetStartPosition();
        }

        private void SetStartPosition()
        {
            float x = remap(-1, 1, -shaftLength, shaftLength, startingPosition.x);
            float z = remap(-1, 1, -shaftLength, shaftLength, startingPosition.y);
            SetHandleRotation(new Vector3(x, 0, z));
        }

        private void OnGrab(SelectEnterEventArgs x)
        {
            StopAllCoroutines();
            hand = x.interactorObject.transform;
            handOffSetFromStartOfGrab = x.interactorObject.transform.position - transform.position;
        }


        private void Update()
        {
            if (!hand) return;

            GetVectorProjectionFromHand(out var locRot);
            SetHandleRotation(locRot);
            InvokeUnityEvents();
        }

        private void GetVectorProjectionFromHand(out Vector3 locRot)
        {
            //Projection
            Vector3 positionToProject = hand.position - handOffSetFromStartOfGrab;
            Vector3 v = positionToProject - transform.position;
            Vector3 projection = Vector3.ProjectOnPlane(v, originalPositionTracker.up);

            Vector3 projectedPoint;
            if (xAxis & yAxis)
                projectedPoint = transform.position + Vector3.ClampMagnitude(projection, 1);
            else
                projectedPoint = transform.position + new Vector3(Mathf.Clamp(projection.x, -1, 1), 0, Mathf.Clamp(projection.z, -1, 1));

            locRot = transform.InverseTransformPoint(projectedPoint);
        }

        private void SetHandleRotation(Vector3 locRot)
        {
            float x = remap(-shaftLength, shaftLength, -1, 1, locRot.x);
            float z = remap(-shaftLength, shaftLength, -1, 1, locRot.z);

            Vector3 newValue = Vector3.zero;
            if (xAxis & yAxis)
                newValue = Vector2.ClampMagnitude(new Vector2(x, z), 1);

            if (!xAxis)
                newValue = new Vector2(0, Mathf.Clamp(z, -1, 1));
            if (!yAxis)
                newValue = new Vector2(Mathf.Clamp(x, -1, 1), 0);

            rotationPoint.localEulerAngles = new Vector3(newValue.y * maxAngle, 0, -newValue.x * maxAngle);

            CurrentValue = newValue;
        }

        private void InvokeUnityEvents()
        {
            RemapValue = remap(-1, 1, remapValueMin, remapValueMax, CurrentValue);
            ValueChanged.Invoke(RemapValue);
            if (!xAxis)
                SingleValueChanged.Invoke(RemapValue.y);
            if (!yAxis)
                SingleValueChanged.Invoke(RemapValue.x);
        }

        private IEnumerator ReturnToZero()
        {
            if (!returnToStartOnRelease) yield break;

            while (CurrentValue.magnitude >= .01f)
            {
                CurrentValue = Vector2.Lerp(CurrentValue, returnToPosition, Time.deltaTime * returnSpeed);
                rotationPoint.localEulerAngles = new Vector3(CurrentValue.y * maxAngle, 0, -CurrentValue.x * maxAngle);
                InvokeUnityEvents();
                yield return null;
            }

            CurrentValue = returnToPosition;
            rotationPoint.localEulerAngles = returnToPosition;
            InvokeUnityEvents();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            if (xAxis && yAxis)
                Gizmos.DrawWireSphere(transform.position, shaftLength);
            if (!xAxis && yAxis)
                Gizmos.DrawLine(transform.position - transform.forward * shaftLength, transform.position + transform.forward * shaftLength);
            if (!yAxis && xAxis)
                Gizmos.DrawLine(transform.position - transform.right * shaftLength, transform.position + transform.right * shaftLength);

            Gizmos.DrawLine(transform.position, transform.position + transform.up * shaftLength);
        }
    }
}