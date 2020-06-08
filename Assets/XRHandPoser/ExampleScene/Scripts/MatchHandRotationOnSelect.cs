// Copyright (c) MikeNspired. All Rights Reserved.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class MatchHandRotationOnSelect : MonoBehaviour
    {
        public XRGrabInteractable interactable;
        public Transform HandAttachTransformParent;

        private void Start()
        {

            interactable.onSelectEnter.AddListener(x => SetPosition(x.GetComponent<HandReference>()?.hand));
        }

        private void SetPosition(HandAnimator handAnimator)
        {
            var handDirection = handAnimator.transform.forward;
            HandAttachTransformParent.transform.forward = Vector3.ProjectOnPlane(handDirection, transform.up);
        }
    }
}
