// Author MikeNspired

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MikeNspired.XRIStarterKit
{
    public class InGameConsoleLogger : MonoBehaviour
    {
        [SerializeField] private Transform textPrefab, textParent;
        [SerializeField] private bool logTime, logStackTrace;
        [SerializeField] private int textCount = 20;
        [SerializeField] private Color logColor = Color.white, warningColor = Color.yellow, errorColor = Color.red, backgroundColor1, backGroundColor2;

        private List<Transform> textList = new List<Transform>();
        private int counter;

        private void OnEnable() => Application.logMessageReceived += LogMessages;

        private void OnDisable() => Application.logMessageReceived -= LogMessages;

        private void LogMessages(string condition, string stackTrace, LogType type)
        {
            string message = null;

            if (logTime)
            {
                message = "[" + Time.time.ToString("f3") + "] ";
            }

            if (logStackTrace)
                message += stackTrace.Trim();
            else
                message += condition.Trim();

            var entryColor = type switch
            {
                LogType.Error => errorColor,
                LogType.Warning => warningColor,
                LogType.Log => logColor,
                _ => errorColor
            };

            Transform newText;
            if (textList.Count >= textCount)
            {
                newText = textList[counter % textCount];
                newText.SetParent(transform);
            }
            else
            {
                newText = Instantiate(textPrefab, textParent);
                newText.name = "Text: " + counter;
                textList.Add(newText);
            }

            newText.SetParent(textParent);

            var newTextMesh = newText.GetComponentInChildren<TextMeshProUGUI>();
            var textImage = newText.GetComponentInChildren<Image>();

            newTextMesh.text = message;
            newTextMesh.color = entryColor;
            textImage.color = counter % 2 == 1 ? backgroundColor1 : backGroundColor2;

            counter++;
            LayoutRebuilder.ForceRebuildLayoutImmediate(textParent as RectTransform);
        }

        public void Clear()
        {
            foreach (var text in textList) text.GetComponentInChildren<TextMeshProUGUI>().text = "";
        }
    }
}