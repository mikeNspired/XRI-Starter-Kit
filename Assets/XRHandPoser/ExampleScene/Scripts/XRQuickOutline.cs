using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRQuickOutline : Outline
{
    [SerializeField] private XRBaseInteractable _baseInteractable;

    private new void OnValidate()
    {
        if (!_baseInteractable)
            _baseInteractable = GetComponentInParent<XRBaseInteractable>();
    }

    private new void Start()
    {
        OnValidate();
        _baseInteractable.hoverEntered.AddListener(x => enabled = true);
        _baseInteractable.hoverExited.AddListener(x => enabled = false);
        _baseInteractable.selectEntered.AddListener(x => enabled = false);
        _baseInteractable.selectExited.AddListener(x =>
        {
            if (_baseInteractable.isHovered) enabled = true;
        });
        enabled = false;
    }
}