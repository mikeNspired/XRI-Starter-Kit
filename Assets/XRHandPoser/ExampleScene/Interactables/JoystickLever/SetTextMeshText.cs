using TMPro;
using UnityEngine;

public class SetTextMeshText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMeshPro;
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    [SerializeField] private int decimalPointCount = 3;
    public void SetText(string text)
    {
        if (textMeshPro)
            textMeshPro.text = text;
        if (textMeshProUGUI)
            textMeshProUGUI.text = text;
    }

    public void SetText(float value) => SetText(value.ToString("f" + decimalPointCount));
}