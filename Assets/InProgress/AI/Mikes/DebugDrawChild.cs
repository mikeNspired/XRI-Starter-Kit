using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DebugDrawChild : MonoBehaviour
{
    private I_DebugDraw debugDraw;
    private MeshRenderer mesh;
    private void OnValidate()
    {
        debugDraw = GetComponentInParent<I_DebugDraw>();
        GetComponentInParent<PatrolPath>()?.GetListFromChildren();
        mesh = GetComponent<MeshRenderer>();
    }


    public void OnDrawGizmosSelected()
    {
        if (debugDraw == null)
            debugDraw = GetComponentInParent<I_DebugDraw>();

        debugDraw?.OnDrawGizmosSelected();
        
    }

    public void OnDestroy()
    {
        GetComponentInParent<PatrolPath>()?.pathNodes.Remove(transform.GetComponent<Transform>());
    }
}