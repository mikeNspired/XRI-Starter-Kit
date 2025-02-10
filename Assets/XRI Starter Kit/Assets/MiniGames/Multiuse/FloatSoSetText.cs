using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class FloatSoSetText : MonoBehaviour
    {
        public FloatSO FloatSO;

        public TextMeshProUGUI text;
        public int decimalCount = 2;

        private string decimalString = "f";

        // Start is called before the first frame update
        void Awake()
        {
            FloatSO.OnValueChanged += ChangedValue;
            decimalString += (decimalCount.ToString());
        }

        private void ChangedValue()
        {
            text.text = FloatSO.GetValue().ToString(decimalString);
        }
    }
}