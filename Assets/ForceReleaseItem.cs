using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ForceReleaseItem : MonoBehaviour
{
    [SerializeField] private XRInteractionManager xrManager;
    [SerializeField] private XRDirectInteractor directInteractor;

    void Start()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (!xrManager)
            xrManager = FindObjectOfType<XRInteractionManager>();
        if (!directInteractor)
            directInteractor = GetComponent<XRDirectInteractor>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReleaseItem();
        }
    }

    public void ReleaseItem()
    {
        if (directInteractor.selectTarget)
            xrManager.SelectExit_public(directInteractor, directInteractor.selectTarget);
    }
}