using MikeNspired.XRIStarterKit.ChrisNolet;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    public class XRQuickOutline : Outline
    {
        [SerializeField] private XRBaseInteractable _baseInteractable;
        [SerializeField] private bool onlyHighlightsWhenNotSelected;
        private Color startingColor;

        private void OnValidate()
        {
            if (!_baseInteractable)
                _baseInteractable = GetComponentInParent<XRBaseInteractable>();
        }

        private void Start()
        {
            OnValidate();
            startingColor = OutlineColor;

            _baseInteractable.hoverEntered.AddListener(OnHoverEnter);
            _baseInteractable.hoverExited.AddListener(OnHoverExit);
            _baseInteractable.selectEntered.AddListener(OnSelectEnter);
            _baseInteractable.selectExited.AddListener(OnSelectExit);

            enabled = false;
        }

        private void OnDestroy()
        {
            if (_baseInteractable != null)
            {
                _baseInteractable.hoverEntered.RemoveListener(OnHoverEnter);
                _baseInteractable.hoverExited.RemoveListener(OnHoverExit);
                _baseInteractable.selectEntered.RemoveListener(OnSelectEnter);
                _baseInteractable.selectExited.RemoveListener(OnSelectExit);
            }
        }

        private void OnHoverEnter(HoverEnterEventArgs args) => Highlight(args);
        private void OnHoverExit(HoverExitEventArgs args) => StopHighlight();
        private void OnSelectEnter(SelectEnterEventArgs args) => StopHighlight();

        private void OnSelectExit(SelectExitEventArgs args)
        {
            if (_baseInteractable.isHovered)
                Highlight(null);
        }

        public void Highlight(HoverEnterEventArgs args)
        {
            if (onlyHighlightsWhenNotSelected && _baseInteractable.isSelected) return;
            if (args != null && args.interactorObject.transform.GetComponent<XRBaseInteractor>().hasSelection) return;
            OutlineColor = startingColor;
            enabled = true;
        }

        public void HighlightWithColor(Color color)
        {
            if (onlyHighlightsWhenNotSelected && _baseInteractable.isSelected) return;
            OutlineColor = color;
            enabled = true;
        }

        public void StopHighlight() => enabled = false;
    }
}
