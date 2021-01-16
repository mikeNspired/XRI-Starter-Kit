using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetNewPathAllControllers : MonoBehaviour
{
    public PatrolPath path;

    private void Start()
    {
        var enemies = FindObjectsOfType<A_EnemyNavController>();
        foreach (var enemy in enemies)
        {
            enemy.SetNewPath(path);
            enemy.SetPathDestinationToClosestNode();
            enemy.SetNavDestination(path.GetPositionOfPathNode(0));
            enemy.getRandomPathIndex = false;
        }
    }
}