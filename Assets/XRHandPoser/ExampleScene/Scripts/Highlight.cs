using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class Highlight : MonoBehaviour
    {
        public XRGrabInteractable xrGrabInteractable;
        public GameObject highlightModel;

        private bool canHighlight;

        private void Start()
        {
            OnValidate();
            highlightModel.SetActive(false);
            xrGrabInteractable.onSelectEntered.AddListener(x => DisableHighlighting());
            xrGrabInteractable.onSelectExited.AddListener(x => EnableHighlighting());
            
            //TODO add highlight on hover
            // xrGrabInteractable.onHoverEnter.AddListener(x => HighlightMesh());
            // xrGrabInteractable.onHoverExit.AddListener(x => RemoveHighlight());
        }

        private void OnValidate()
        {
            if (!xrGrabInteractable)
                xrGrabInteractable = GetComponent<XRGrabInteractable>();
        }

        public void DisableHighlighting()
        {
            canHighlight = true;
            RemoveHighlight();
        }

        public void EnableHighlighting()
        {
            canHighlight = false;
        }

        public void HighlightMesh()
        {
            if (!canHighlight)
                highlightModel.SetActive(true);
        }

        public void RemoveHighlight()
        {
            highlightModel.SetActive(false);
        }
    }
}