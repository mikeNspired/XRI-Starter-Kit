using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class ScoreSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI mainScore = null, firstScore = null, secondScore = null;

        public void SetFirstScore(string score)
        {
            firstScore.text = score.ToString();
        }

        public void SetSecondScore(string score)
        {
            secondScore.text = score.ToString();
        }

        public void SetFinalScore(string score)
        {
            mainScore.text = score.ToString();
        }

        public void Reset()
        {
            mainScore.text = string.Empty;
            firstScore.text = string.Empty;
            secondScore.text = string.Empty;
        }
    }
}