using TMPro;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class SetTextMeshText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMeshPro;
        [SerializeField] private TextMeshProUGUI textMeshProUGUI;
        [SerializeField] private int decimalPointCount = 3;

        private void Awake() => OnValidate();

        private void OnValidate()
        {
            if (!textMeshPro) textMeshPro = GetComponent<TextMeshPro>();
            if (!textMeshProUGUI) textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        }

        public void SetText(string text)
        {
            if (textMeshPro)
                textMeshPro.text = text;
            if (textMeshProUGUI)
                textMeshProUGUI.text = text;
        }

        public void SetText(float value) => SetText(value.ToString("f" + decimalPointCount));
    }
}