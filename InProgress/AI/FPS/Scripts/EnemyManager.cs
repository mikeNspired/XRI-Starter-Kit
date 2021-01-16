using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{

    public List<A_EnemyNavController> enemies { get; private set; }
    public int numberOfEnemiesTotal { get; private set; }
    public int numberOfEnemiesRemaining => enemies.Count;
    
    public UnityAction<A_EnemyNavController, int> onRemoveEnemy;

    private void Awake()
    {


        enemies = new List<A_EnemyNavController>();
    }

    public void RegisterEnemy(A_EnemyNavController enemyNav)
    {
        enemies.Add(enemyNav);

        numberOfEnemiesTotal++;
    }

    public void UnregisterEnemy(A_EnemyNavController enemyNavKilled)
    {
        int enemiesRemainingNotification = numberOfEnemiesRemaining - 1;

        if (onRemoveEnemy != null)
        {
            onRemoveEnemy.Invoke(enemyNavKilled, enemiesRemainingNotification);
        }

        // removes the enemy from the list, so that we can keep track of how many are left on the map
        enemies.Remove(enemyNavKilled);
    }
}
