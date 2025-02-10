using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;

namespace MikeNspired.XRIStarterKit
{
    public class VignetteEditor : MonoBehaviour
    {
        private TunnelingVignetteController vignetteController;
        private void Awake() => vignetteController = GetComponent<TunnelingVignetteController>();
        public void SetApertureSize(float value) => vignetteController.defaultParameters.apertureSize = value;
        public void SetFeatheringSize(float value) => vignetteController.defaultParameters.featheringEffect = value;
    }
}