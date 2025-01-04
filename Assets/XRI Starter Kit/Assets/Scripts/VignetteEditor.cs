using UnityEngine;


public class VignetteEditor : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController vignetteController;
    private void Awake() => vignetteController = GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController>();
    public void SetApertureSize (float value) => vignetteController.defaultParameters.apertureSize = value;
    public void SetFeatheringSize (float value) => vignetteController.defaultParameters.featheringEffect = value;
}
