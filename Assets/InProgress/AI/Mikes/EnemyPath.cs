// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;
// using System.Linq.Expressions;
// using UnityEditor;
// using UnityEngine.Events;
// using UnityEngine.Serialization;
//
// public class EnemyPath : MonoBehaviour, I_DebugDraw
// {
//
//     [FormerlySerializedAs("list")] public List<Transform> pathTransforms;
//
//     public float sphereSize = .25f;
//
//     public void GetListFromChildren()
//     {
//         pathTransforms = GetComponentsInChildren<Transform>().Select(x => x).ToList();
//     }
//     
//     public void OnDrawGizmosSelected()
//     {
//         if (pathTransforms.Count <= 0 || pathTransforms[0] == null) return;
//         Gizmos.DrawSphere(pathTransforms[0].transform.position, sphereSize);
//
//         for (int i = 0; i < pathTransforms.Count - 1; i++)
//         {
//             Debug.DrawLine(pathTransforms[i].transform.position, pathTransforms[i + 1].transform.position, Color.green);
//             Gizmos.DrawSphere(pathTransforms[i + 1].transform.position, sphereSize);
//         }
//     }
// }
//
//
public interface I_DebugDraw
{
    void OnDrawGizmosSelected();
}