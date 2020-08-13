// Copyright (c) MikeNspired. All Rights Reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        private void OnValidate()
        {
            if (!hand)
                hand = GetComponentInChildren<HandAnimator>();
        }

        private void Start() => OnValidate();

    }
}