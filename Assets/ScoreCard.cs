using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ScoreCard : MonoBehaviour
{
    public int totalScore, currentSlot, currentScoreToAdd, currentRoll, currentPinsHit;
    public bool isSpare;
    public bool isStrike;
    public int extraRollCounter;

    public ScoreSlot[] ScoreSlots;

    public UnityEvent FrameReset;

    // Start is called before the first frame update
    void Start()
    {
    }

    private void OnValidate()
    {
        ScoreSlots = GetComponentsInChildren<ScoreSlot>();
        foreach (var slot in ScoreSlots)
        {
            slot.Reset();
        }
    }

    public void ResetFrame()
    {
        isSpare = false;
        isStrike = false;
        currentPinsHit = 0;
        extraRollCounter = 0;
        currentRoll = 0;
        currentScoreToAdd = 0;
        currentSlot++;
        FrameReset.Invoke();
    }

    public void PinsHit(int pinsHit)
    {
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
            Debug.Log(currentRoll + "SetNormalScore first");

        }
        else if (currentRoll > 1)
        {
            Debug.Log(currentRoll + "SetNormalScore second");

            ScoreSlots[currentSlot].SetSecondScore(pinsHit.ToString());
            totalScore += currentScoreToAdd;
            ScoreSlots[currentSlot].SetFinalScore(totalScore.ToString());
            ResetFrame();
        }
    }

    private void SetSpareScore(int pinsHit)
    {
        Debug.Log(currentRoll + "Set Spare Score");

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
            Debug.Log(currentRoll + "Set Strike Score");

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
        Debug.Log("Strike");
        currentPinsHit = 0;

        isStrike = true;
        ScoreSlots[currentSlot].SetSecondScore("X");
        currentRoll = 0;
        currentSlot++;

    }

    private void Spare()
    {
        Debug.Log("Spare");
        currentPinsHit = 0;
        isSpare = true;
        ScoreSlots[currentSlot].SetSecondScore("/");
        currentSlot++;

    }

    // Update is called once per frame
    void Update()
    {
    }
}