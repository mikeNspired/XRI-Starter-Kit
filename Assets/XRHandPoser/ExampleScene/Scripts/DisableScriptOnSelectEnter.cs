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
        xrDirectInteractor.onSelectEnter.AddListener((x) => script.enabled = false);
        xrDirectInteractor.onSelectExit.AddListener((x) => script.enabled = true);
    }

}
