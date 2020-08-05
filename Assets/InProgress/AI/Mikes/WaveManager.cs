using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MikeNspired.UnityXRHandPoser;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class WaveManager : MonoBehaviour
{
    public List<Wave> waveList;
    
    public int currentWaveCounter;
    public Wave currentWave;
    public bool ShowUnityEvent = false;
    public UnityEvent OnWaveStarted;
    public FloatSO waveNumber;
    public FloatSO waveScore;
    public FloatSO waveCombo;
    public float comboTracker;

    public float scoreAddWhenWaveComplete = 1000;

    public AudioRandomize waveCompleteAudioSound;
    public AudioRandomize gameStartAudio;
    private void Awake()
    {
        foreach (var wave in waveList)
        {
            wave.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        waveNumber.SetValue(0);
        waveScore.SetValue(0);
        waveCombo.SetValue(1);
        StartNextWave();
        gameStartAudio.PlaySound();
    }

    private void OnValidate()
    {
        waveList = GetComponentsInChildren<Wave>(true).ToList();
    }

    void StartNextWave()
    {
        if (currentWaveCounter >= waveList.Count) return;

        waveList[currentWaveCounter].gameObject.SetActive(true);
        waveList[currentWaveCounter].Initialize();
        currentWave = waveList[currentWaveCounter];
        currentWave.OnEnemyKilled += AddCombo;
        OnWaveStarted.Invoke();
        currentWave.OnWaveComplete.AddListener(WaveCompleted);

        waveNumber.SetValue(currentWaveCounter + 1);
        if (currentWaveCounter > 0)
            waveScore.SetValue(waveScore.GetValue() + scoreAddWhenWaveComplete * comboTracker);
    }


    private void AddCombo(Actor enemyKilled, GameObject whatKilledEnemy)
    {
        if (whatKilledEnemy.GetComponent<Bullet>())
        {
            waveScore.SetValue(waveScore.GetValue() + 5 * comboTracker);
            comboTracker++;
            waveCombo.SetValue(comboTracker);
            return;
        }
        waveScore.SetValue(waveScore.GetValue() + 5);
    }

    private void WaveCompleted()
    {
        waveCompleteAudioSound.PlaySound();
        currentWave.gameObject.SetActive(false);
        currentWaveCounter++;

        StartNextWave();
    }
}