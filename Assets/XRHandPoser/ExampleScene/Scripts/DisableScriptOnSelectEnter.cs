using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DisableScriptOnSelectEnter : MonoBehaviour
{
    [SerializeField] private XRDirectInteractor xrDirectInteractor = null;

    [SerializeField] private MonoBehaviour script = null;

    private void Start()
    {
        xrDirectInteractor.onSelectEntered.AddListener((x) => script.enabled = false);
        xrDirectInteractor.onSelectExited.AddListener((x) => script.enabled = true);
    }

}
