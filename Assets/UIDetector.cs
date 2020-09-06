using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class UIDetector : MonoBehaviour
{
    [SerializeField] private XRRayInteractor xrRayInteractable;

    // Start is called before the first frame update
    void Start()
    {
        xrRayInteractable.onHoverEnter.AddListener(OnUIDetected);
        xrRayInteractable.onHoverExit.AddListener(OnNoUi);
    }

    private void OnNoUi(XRBaseInteractable arg0)
    {
        throw new System.NotImplementedException();
    }

    private void OnUIDetected(XRBaseInteractable arg0)
    {
        throw new System.NotImplementedException();
    }


}
