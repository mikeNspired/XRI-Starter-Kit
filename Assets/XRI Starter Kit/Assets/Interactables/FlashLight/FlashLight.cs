using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class FlashLight : MonoBehaviour
    {
        public Color colorOne = Color.white;
        public Color colorTwo = Color.yellow;
        public Color colorThree = Color.blue;
        public Color colorFour = Color.green;
        public Color colorFive = Color.red;
        public Color colorSix = Color.cyan;
        public Renderer rend;
        public Light flashLight;
        public bool isEnabled = true;
        public float minBrightness = .5f, maxBrightness = 5;
        private void Start()
        {
            flashLight.enabled = isEnabled;
            rend.enabled = isEnabled;
        }

        public void SwitchState()
        {
            isEnabled = !isEnabled;
            flashLight.enabled = isEnabled;
            rend.enabled = isEnabled;
        }

        public void SetBrightness(float dialPercentage)
        {
            var dialValuueZeroToOne = Remap(dialPercentage, 0f, 1f, minBrightness, maxBrightness);
            flashLight.intensity = dialValuueZeroToOne;
        }
        
        private float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
        
        public void SetColor(int color)
        {
            if (color == 0)
            {
                flashLight.color = colorOne;
                rend.material.SetColor("_EmissionColor", colorOne);
            }

            if (color == 1)
            {
                flashLight.color = colorTwo;
                rend.material.SetColor("_EmissionColor", colorTwo);
            }

            if (color == 2)
            {
                flashLight.color = colorThree;
                rend.material.SetColor("_EmissionColor", colorThree);
            }

            if (color == 3)
            {
                flashLight.color = colorFour;
                rend.material.SetColor("_EmissionColor", colorFour);
            }

            if (color == 4)
            {
                flashLight.color = colorFive;
                rend.material.SetColor("_EmissionColor", colorFive);
            }

            if (color == 5)
            {
                flashLight.color = colorSix;
                rend.material.SetColor("_EmissionColor", colorSix);
            }
        }
    }
}