using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemySpawnerOverTime : MonoBehaviour
{
    public Action<Actor, GameObject> OnEnemyKilled = delegate { };
    public Action<Actor> OnEnemySpawned = delegate { };

    public GameObject prefab;
    public GameObject[] enemyList;
    public PatrolPath enemyPath;
    public int maxEnemyCount = 10;
    public float timeBetweenSpawn = 2, delayBeforeStartingSpawn;
    public bool endless, burstSpawn;

    private int currentEnemyCount;
    private float timer;
    private Transform enemyParent;
    public bool spawnAtRandomPathPoint;
    public bool randomizeSpawnArea, canSpawn = false;

    private void Awake()
    {
        if (enemyList.Length != 0)
            maxEnemyCount = enemyList.Length;
    }

    public void Initialize()
    {
        Invoke(nameof(AllowSpawning), delayBeforeStartingSpawn);
    }

    private void AllowSpawning()
    {
        canSpawn = true;
        if (burstSpawn)
            BurstSpawn(prefab);
    }

    private void Update()
    {
        if (!canSpawn) return;
        if (burstSpawn) return;

        if (timer >= timeBetweenSpawn)
        {
            SpawnEnemy();
            timer = 0;
        }

        timer += Time.deltaTime;
    }

    private void BurstSpawn(GameObject enemyType)
    {
        int counter;
        for (counter = 0; counter < maxEnemyCount; counter++)
            InstantiateEnemy(enemyType, new Vector3(this.transform.position.x + currentEnemyCount, transform.position.y, transform.position.z));
    }


    private void SpawnEnemy()
    {
        if (enemyList.Length == 0)
        {
            SpawnEnemy(prefab);
            return;
        }

        SpawnEnemy(enemyList[Mathf.Clamp(currentEnemyCount, 0, enemyList.Length - 1)]);
    }


    private void SpawnEnemy(GameObject enemyType)
    {
        if (endless)
        {
            InstantiateEnemy(enemyType, enemyPath.pathNodes[0].transform.position);
            return;
        }

        if (currentEnemyCount < maxEnemyCount)
            InstantiateEnemy(enemyType, enemyPath.pathNodes[0].transform.position);
    }

    private void InstantiateEnemy(GameObject enemyType, Vector3 pos)
    {
        currentEnemyCount++;
        var enemy = Instantiate(enemyType);
        enemy.gameObject.SetActive(false);

        enemy.transform.SetPositionAndRotation(GetRandomLocation(pos), Quaternion.identity);

        if (enemy.GetComponent<A_EnemyController>())
            enemy.GetComponent<A_EnemyController>().SetNewPath(enemyPath);

        if (spawnAtRandomPathPoint)
            enemy.transform.position = enemyPath.pathNodes[UnityEngine.Random.Range(0, enemyPath.pathNodes.Count)].transform.position;


        enemy.transform.parent = transform;
        enemy.gameObject.SetActive(true);
        OnEnemySpawned(enemy.GetComponent<Actor>());

        enemy.GetComponent<A_EnemyController>().onDeath += EnemyKilled;
    }

    private void EnemyKilled(Actor enemyKilled, GameObject whatKilledEnemy)
    {
        OnEnemyKilled(enemyKilled, whatKilledEnemy);
    }

    private Vector3 GetRandomLocation(Vector3 pos)
    {
        var randomPosition = Random.insideUnitSphere * 1 + pos;
        NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, 2, 1);
        return hit.position;
    }

    public void Reset()
    {
        timer = 0;
        currentEnemyCount = 0;
    }

    private void OnValidate()
    {
        // enemyParent = GameObject.Find("===== ENEMIES =====").transform;
    }
}