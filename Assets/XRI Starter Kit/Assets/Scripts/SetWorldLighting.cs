using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class SetWorldLighting : MonoBehaviour
    {
        [SerializeField] private Color color1 = Color.black;
        [SerializeField] private Color color2 = Color.black;

        [SerializeField] private Light mixedLight;

        private Color startingColor;
        private float startingIntensity;
        private LightmapData[] startingLightMaps;

        private void Start()
        {
            startingColor = RenderSettings.ambientLight;
            startingLightMaps = LightmapSettings.lightmaps;
            startingIntensity = mixedLight.intensity;
        }

        public void SetToColor1()
        {
            RenderSettings.ambientLight = color1;
        }

        public void SetToColor2()
        {
            RenderSettings.ambientLight = color2;
        }

        public void BlackenWorld()
        {
            RenderSettings.ambientLight = Color.black;
            LightmapSettings.lightmaps = new LightmapData[] { };
            mixedLight.intensity = .1f;
        }
        public void DarkenWorld()
        {
            RenderSettings.ambientLight = new Color(.2f,.2f,.35f);
            LightmapSettings.lightmaps = new LightmapData[] { };
            mixedLight.intensity = .2f;
        }

        public void ReturnToStartingColor()
        {
            mixedLight.intensity = startingIntensity;
            RenderSettings.ambientLight = startingColor;
            LightmapSettings.lightmaps = startingLightMaps;
        }

        public void SetStateInt(int x)
        {
            if (x == 0)
                ReturnToStartingColor();
            else if (x == 1)
                SetToColor1();
            else
                SetToColor2();
        }
    }
}