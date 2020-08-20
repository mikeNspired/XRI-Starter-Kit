using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class Highlight : MonoBehaviour
    {
        public XRGrabInteractable xrGrabInteractable;
        public GameObject highlightModel;

        private bool disableHighlighting;

        // Start is called before the first frame update
        void Start()
        {
            OnValidate();
            highlightModel.SetActive(false);
            xrGrabInteractable.onSelectEnter.AddListener(x => DisableHighlighting());
            xrGrabInteractable.onSelectExit.AddListener(x => EnableHighlighting());
        }

        private void OnValidate()
        {
            if (!xrGrabInteractable)
                xrGrabInteractable = GetComponent<XRGrabInteractable>();
        }

        public void DisableHighlighting()
        {
            disableHighlighting = true;
            RemoveHighlight();
        }

        public void EnableHighlighting()
        {
            disableHighlighting = false;
        }

        public void HighlightMesh()
        {
            if (!disableHighlighting)
                highlightModel.SetActive(true);
        }

        public void RemoveHighlight()
        {
            highlightModel.SetActive(false);
        }
    }
}