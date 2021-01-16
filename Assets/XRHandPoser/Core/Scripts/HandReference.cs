// Copyright (c) MikeNspired. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    /// <summary>
    /// Required on controllers for handposer to work.
    /// References the XRGrabinteractable because the hand will unparent it self when grabbed.
    /// This allows the scripts to quickly reference the hand.
    /// </summary>
    public class HandReference : MonoBehaviour
    {
        public HandAnimator hand;

        public LeftRight LeftRight;

        private void OnValidate()
        {
            if (!hand)
                hand = GetComponentInChildren<HandAnimator>();
        }

        private void Start() => OnValidate();

        private void Awake()
        {
            GetComponent<XRDirectInteractor>().onSelectEntered.AddListener(OnGrab);
            GetComponent<XRDirectInteractor>().onSelectExited.AddListener(Reset);

            XRDirectInteractor = GetComponent<XRDirectInteractor>();
            startPosition = XRDirectInteractor.attachTransform.localPosition;
            startRotation = XRDirectInteractor.attachTransform.localRotation;
            attachTransform = XRDirectInteractor.attachTransform;
        }

        private XRDirectInteractor XRDirectInteractor;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Transform attachTransform;

        private void OnGrab(XRBaseInteractable x)
        {
            //Vector3 offset = x.GetComponent<XRHandPoser>().rightHandAttach.localPosition - startPosition;
            Vector3 finalPosition = x.GetComponent<XRHandPoser>().rightHandAttach.localPosition * -1; //- offset;
            
            Quaternion finalRotation = Quaternion.Inverse(x.GetComponent<XRHandPoser>().rightHandAttach.localRotation);

            finalPosition = RotatePointAroundPivot(finalPosition, Vector3.zero, finalRotation.eulerAngles);

            attachTransform.localPosition = finalPosition;
            attachTransform.localRotation = finalRotation;

            attachTransform.parent = transform;
            // Debug.Log(x.GetComponent<XRHandPoser>().rightHandAttach.name + " " + x.GetComponent<XRHandPoser>().rightHandAttach.localPosition.ToString("f3") + " " + finalPosition.ToString("f3"));
        }

        private void Reset(XRBaseInteractable x)
        {
            attachTransform.parent = hand.transform;

            GetComponent<XRDirectInteractor>().attachTransform.localPosition = startPosition;
            GetComponent<XRDirectInteractor>().attachTransform.localRotation = startRotation;
            // x.GetComponent<XRGrabInteractable>().attachTransform.position = x.GetComponent<XRHandPoser>().rightHandAttach.position;
            //x.GetComponent<XRGrabInteractable>().attachTransform.rotation = x.GetComponent<XRHandPoser>().rightHandAttach.rotation;
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
                Debug.Break();
        }

        public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            Vector3 direction = point - pivot;
            direction = Quaternion.Euler(angles) * direction;
            return direction + pivot;
        }
    }
}