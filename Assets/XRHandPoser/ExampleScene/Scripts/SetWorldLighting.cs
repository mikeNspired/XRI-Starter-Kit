using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class SetWorldLighting : MonoBehaviour
    {
        [SerializeField] private Color color1 = Color.black;
        [SerializeField] private Color color2 = Color.black;
        

        private Color startingColor;
        private LightmapData[] startingLightMaps;

        private void Start()
        {
            startingColor = RenderSettings.ambientLight;
             startingLightMaps = LightmapSettings.lightmaps;

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
        }

        public void ReturnToStartingColor()
        {
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