using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Wave : MonoBehaviour
{
    public List<EnemySpawnerOverTime> enemySpawners;
    public List<Actor> enemies;
    
    public int TotalEnemiesToKill;
    public int currentEnemiesKilled;

    public UnityEvent OnWaveStart;
    public UnityEvent OnWaveComplete;
    public Action<Actor, GameObject> OnEnemyKilled = delegate { };


    public void Initialize()
    {
        OnWaveStart.Invoke();

        foreach (var spawner in enemySpawners)
        {
            TotalEnemiesToKill += spawner.maxEnemyCount;
            spawner.OnEnemyKilled += EnemyKilled;
            spawner.OnEnemySpawned += (enemy) => enemies.Add(enemy);
            spawner.Initialize();
        }
    }



    private void EnemyKilled(Actor killedEnemy, GameObject whatKilledEnemy)
    {
        enemies.Remove(killedEnemy);
        currentEnemiesKilled++;
        OnEnemyKilled(killedEnemy,whatKilledEnemy);
        if (currentEnemiesKilled >= TotalEnemiesToKill)
            WaveCompleted();
    }

    private void WaveCompleted()
    {
        Debug.Log("Wave: WaveCompleted ");

        OnWaveComplete.Invoke();
    }

    private void OnValidate()
    {
        enemySpawners = GetComponentsInChildren<EnemySpawnerOverTime>().ToList();
    }
}