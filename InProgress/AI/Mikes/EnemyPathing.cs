using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class EnemyPathing : MonoBehaviour, I_DebugDraw
{
    public float distanceToChange = 5;
    public List<Transform> pathTransforms;
    public bool randomPositions;


    public float distanceToTarget;


    public int current;

    public Vector3 CurrentTargetPosition;

    public bool pauseTargetIncrement;
    public bool ShowUnityEvent = false;
    public UnityEvent OnGoalPositionReached;

    private void Start()
    {
        
    }

    public void InitializeListFromWaveManager(List<Transform> list)
    {
        this.pathTransforms = list;
        CurrentTargetPosition = list[0].position;
    }

    void Update()
    {
        if (pathTransforms.Count == 0) return;

        if (IsChangeDistance() && !pauseTargetIncrement)
        {
            if (randomPositions)
                GetRandomNextPath();
            else
            {
                IncrementCurrentPath();
                CurrentTargetPosition = GetRandomPositionNearGoalTarget();
            }

            OnGoalPositionReached.Invoke();
        }
        
    }


    public void IncrementCurrentPath()
    {
        current++;
        if (current > pathTransforms.Count - 1)
            current = 0;
    }

    private void OnEnemyCollided(Collision c)
    {
        IncrementCurrentPath();
        SetTargetBackToPath();
    }

    public bool IsChangeDistance() => (Vector3.Distance(transform.position, CurrentTargetPosition) < distanceToChange);


    public void SetTargetBackToPath()
    {
        CurrentTargetPosition = GetRandomPositionNearGoalTarget();
        pauseTargetIncrement = false;
    }


    public void SetTargetToRandomNearSelfGreaterThanDistanceChange()
    {
        CurrentTargetPosition = GetRandomPositionGreaterThanDistanceChange();
        pauseTargetIncrement = true;
    }

    public void GetRandomNextPath()
    {
        int oldCurrent = current;
        while (current == oldCurrent)
            current = Random.Range(0, pathTransforms.Count);

        CurrentTargetPosition = GetRandomPositionGreaterThanDistanceChange();
    }

    #region helperMethods

    public Vector3 GetRandomPositionGreaterThanDistanceChange(float distanceMultiplier = 2)
    {
        float distance = 0;
        Vector3 randomPosition = Vector3.zero;
        while (distance < distanceToChange)
        {
            randomPosition = Random.insideUnitSphere * distanceToChange * distanceMultiplier;
            distance = Vector3.Distance(randomPosition, transform.position);
        }

        return transform.position + randomPosition;
    }


    public Vector3 GetRandomPositionNearGoalTarget(float distanceMultiplier = .9f)
    {
        var randomPosition = Random.insideUnitSphere * distanceToChange * distanceMultiplier;
        return pathTransforms[current].position + randomPosition;
    }

    public Vector3 GetRandomPositionNearSelf(float distanceMultiplier = 1)
    {
        Vector3 randomPosition = Random.insideUnitSphere * distanceMultiplier;
        return transform.position + randomPosition;
    }

    public float debugDrawSphereSize = .25f;
    public bool drawDistanceSpheres = true;

    public void OnDrawGizmosSelected()
    {
        if (pathTransforms.Count <= 0 || pathTransforms[0] == null) return;
        Gizmos.DrawSphere(pathTransforms[0].transform.position, debugDrawSphereSize);

        for (int i = 0; i < pathTransforms.Count - 1; i++)
        {
            Debug.DrawLine(pathTransforms[i].transform.position, pathTransforms[i + 1].transform.position, Color.green);
            Gizmos.DrawSphere(pathTransforms[i + 1].transform.position, debugDrawSphereSize);
            if (drawDistanceSpheres)
                Gizmos.DrawWireSphere(pathTransforms[i + 1].transform.position, distanceToChange);
        }
    }

    #endregion
}