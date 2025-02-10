using TMPro;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class LeverExample : MonoBehaviour
    {
        public float CurrentSpeed;
        public TextMeshProUGUI text;

        [SerializeField] private float maxSpeed = 1, minSpeed = -1;

        public Vector2 CurrentVector;

        public void SetSpeed(float percentage)
        {
            var remappedValue = Remap(percentage, 0f, 1f, minSpeed, maxSpeed);
            CurrentSpeed = remappedValue;
            text.text = CurrentSpeed.ToString();
        }

        public void SetSpeedNegativeOneToOne(float percentage)
        {
            var remappedValue = Remap(percentage, -1f, 1f, minSpeed, maxSpeed);
            CurrentSpeed = remappedValue;
            text.text = CurrentSpeed.ToString();
        }

        public void SetVector(Vector2 value)
        {
            var remappedValuex = Remap(value.x, -1f, 1f, minSpeed, maxSpeed);
            var remappedValuez = Remap(value.y, -1f, 1f, minSpeed, maxSpeed);
            CurrentVector = new Vector2(remappedValuex, remappedValuez);
            text.text = CurrentVector.ToString();
        }

        private float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}