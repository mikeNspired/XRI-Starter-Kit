using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRQuickOutline : Outline
{
    [SerializeField] private XRBaseInteractable _baseInteractable;
    [SerializeField] private bool onlyHighlightsWhenNotSelected;
    private Color _startingColor;

    private void OnValidate()
    {
        if (!_baseInteractable)
            _baseInteractable = GetComponentInParent<XRBaseInteractable>();
    }

    private void Start()
    {
        OnValidate();
        _startingColor = OutlineColor;
        _baseInteractable.hoverEntered.AddListener(  Highlight);
        _baseInteractable.hoverExited.AddListener(x => enabled = false);
        _baseInteractable.selectEntered.AddListener(x => enabled = false);
        _baseInteractable.selectExited.AddListener(x =>
        {
            if (_baseInteractable.isHovered) Highlight(null);
        });
        enabled = false;
    }

    public void Highlight(HoverEnterEventArgs args)
    {
        if (onlyHighlightsWhenNotSelected && _baseInteractable.isSelected) return;
        if (args != null && args.interactorObject.transform.GetComponent<XRBaseInteractor>().hasSelection) return;
        OutlineColor = _startingColor;
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