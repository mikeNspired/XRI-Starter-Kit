using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    public class ScoreCard : MonoBehaviour
    {
        private int totalScore, currentSlot, currentScoreToAdd, currentRoll, currentPinsHit;
        private bool isSpare, isStrike;

        [SerializeField] private ScoreSlot[] ScoreSlots;

        public UnityEvent FrameReset;

        private void OnValidate()
        {
            ScoreSlots = GetComponentsInChildren<ScoreSlot>();
        }

        private void ResetFrame()
        {
            isSpare = false;
            isStrike = false;
            currentPinsHit = 0;
            currentRoll = 0;
            currentScoreToAdd = 0;
            currentSlot++;
            FrameReset.Invoke();
        }

        public void PinsHit(int pinsHit)
        {
            if (currentSlot > 9) return;
            currentRoll++;
            currentPinsHit += pinsHit;
            currentScoreToAdd += pinsHit;
            
            if (pinsHit == 10)
            {
                Strike();
                return;
            }

            if (currentPinsHit == 10)
            {
                Spare();
                return;
            }

            if (!isSpare && !isStrike)
            {
                SetNormalScore(pinsHit);
                return;
            }

            if (isSpare)
                SetSpareScore(pinsHit);
            else if (isStrike)
                SetStrikeScore(pinsHit);
        }

        private void SetNormalScore(int pinsHit)
        {
            if (currentRoll == 1)
            {
                ScoreSlots[currentSlot].SetFirstScore(pinsHit.ToString());
            }
            else if (currentRoll > 1)
            {
                ScoreSlots[currentSlot].SetSecondScore(pinsHit.ToString());
                totalScore += currentScoreToAdd;
                ScoreSlots[currentSlot].SetFinalScore(totalScore.ToString());
                ResetFrame();
            }
        }

        private void SetSpareScore(int pinsHit)
        {
            ScoreSlots[currentSlot].SetFirstScore(pinsHit.ToString());
            totalScore += currentScoreToAdd;
            ScoreSlots[currentSlot - 1].SetFinalScore(totalScore.ToString());
            currentScoreToAdd -= 10;
            isSpare = false;
        }

        private void SetStrikeScore(int pinsHit)
        {
            if (currentRoll == 1)
                ScoreSlots[currentSlot].SetFirstScore(pinsHit.ToString());

            else if (currentRoll > 1)
            {
                ScoreSlots[currentSlot].SetSecondScore(pinsHit.ToString());

                //Set previous slot
                totalScore += currentScoreToAdd;
                ScoreSlots[currentSlot - 1].SetFinalScore(totalScore.ToString());

                //Set current slot
                totalScore += currentScoreToAdd - 10;
                ScoreSlots[currentSlot].SetFinalScore(totalScore.ToString());

                ResetFrame();
            }
        }

        private void Strike()
        {
            currentPinsHit = 0;

            isStrike = true;
            ScoreSlots[currentSlot].SetSecondScore("X");
            currentRoll = 0;
            currentSlot++;
        }

        private void Spare()
        {
            currentPinsHit = 0;
            isSpare = true;
            ScoreSlots[currentSlot].SetSecondScore("/");
            currentSlot++;
        }

        public void Reset()
        {
            ResetFrame();
            foreach (var slot in ScoreSlots)
            {
                slot.Reset();
            }

            currentSlot = 0;
        }
    }
}