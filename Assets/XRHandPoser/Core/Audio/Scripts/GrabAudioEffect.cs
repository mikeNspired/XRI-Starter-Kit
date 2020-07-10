// Copyright (c) MikeNspired. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    [RequireComponent(typeof(AudioRandomize))]
    public class GrabAudioEffect : MonoBehaviour
    {
        private AudioRandomize audioRandomizer;
        public XRGrabInteractable interactable;

        private void Start()
        {
            GetVariables();

            if (interactable)
                interactable.onSelectEnter.AddListener(x => PlaySound());

            else
                Debug.Log("XRGrabInteractable not found on : " + gameObject.name + " to play hand grabbing sound effect");
        }

        private void OnValidate() => GetVariables();

        private void GetVariables()
        {
            if (!interactable)
                interactable = GetComponentInParent<XRGrabInteractable>();
            if (!audioRandomizer)
                audioRandomizer = GetComponent<AudioRandomize>();
        }

        private void PlaySound()
        {
            audioRandomizer.PlaySound();
        }
    }
}