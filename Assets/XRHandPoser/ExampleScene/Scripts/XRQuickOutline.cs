using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRQuickOutline : Outline
{
    [SerializeField] private XRBaseInteractable _baseInteractable;
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
        _baseInteractable.hoverEntered.AddListener(x => Highlight());
        _baseInteractable.hoverExited.AddListener(x => enabled = false);
        _baseInteractable.selectEntered.AddListener(x => enabled = false);
        _baseInteractable.selectExited.AddListener(x =>
        {
            if (_baseInteractable.isHovered) Highlight();
        });
        enabled = false;
    }

    public void Highlight()
    {
        OutlineColor = _startingColor;
        enabled = true;
    }

    public void HighlightWithColor(Color color)
    {
        OutlineColor = color;
        enabled = true;
    }

    public void StopHighlight() => enabled = false;
}