// Copyright (c) MikeNspired. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class Door : MonoBehaviour
    {
        private Transform mainCamera;

        private XRGrabInteractable currentGrabbedInteractable;
        [SerializeField] private XRGrabInteractable leftXRGrabInteractable = null;
        [SerializeField] private XRGrabInteractable rightXRGrabInteractable = null;

        [SerializeField] private Transform leftFacingDirection = null;
        [SerializeField] private Transform rightFacingDirection = null;

        [SerializeField] private Rigidbody follow = null;

        private ColliderDisableMoveReturn leftGrabCollider;
        private ColliderDisableMoveReturn rightGrabCollider;
        private bool isFollowActive;

        private void Start()
        {
            leftGrabCollider = leftXRGrabInteractable.GetComponent<ColliderDisableMoveReturn>();
            rightGrabCollider = rightXRGrabInteractable.GetComponent<ColliderDisableMoveReturn>();

            leftXRGrabInteractable.onSelectEnter.AddListener(x => currentGrabbedInteractable = leftXRGrabInteractable);
            leftXRGrabInteractable.onSelectEnter.AddListener(call: x => isFollowActive = true);
            leftXRGrabInteractable.onSelectExit.AddListener(call: OnRelease);

            rightXRGrabInteractable.onSelectEnter.AddListener(x => currentGrabbedInteractable = rightXRGrabInteractable);
            rightXRGrabInteractable.onSelectEnter.AddListener(call: x => isFollowActive = true);
            rightXRGrabInteractable.onSelectExit.AddListener(call: OnRelease);
            StartCoroutine(GetMainCamera());
        }

        private IEnumerator GetMainCamera()
        {
            while (!mainCamera)
            {
                yield return new WaitForEndOfFrame();
                try {mainCamera = Camera.main.transform;}
                catch{ }
            }
        }

        private void OnRelease(XRBaseInteractor x)
        {
            currentGrabbedInteractable = null;
            isFollowActive = false;
            follow.velocity = Vector3.zero;
            follow.angularVelocity = Vector3.zero;
        }

        private void Update()
        {
            CheckIfPlayerFacing(rightXRGrabInteractable, rightGrabCollider, rightFacingDirection);
            CheckIfPlayerFacing(leftXRGrabInteractable, leftGrabCollider, leftFacingDirection);
            if (isFollowActive)
                follow.MovePosition(currentGrabbedInteractable.transform.position);
        }

        private void CheckIfPlayerFacing(XRGrabInteractable grabInteractable, ColliderDisableMoveReturn collider, Transform facingDirection)
        {
            if (!mainCamera) return;

            Vector3 forward = facingDirection.forward;
            Vector3 toOther = (mainCamera.transform.position - facingDirection.position).normalized;

            var dot = Vector3.Dot(forward, toOther);

            if (dot > 0)
            {
                collider.EnableCollider();

                if (isFollowActive) return;

                grabInteractable.transform.position = facingDirection.transform.position;
                grabInteractable.transform.rotation = facingDirection.transform.rotation;
            }
            else
                collider.DisableCollider();
        }
    }
}