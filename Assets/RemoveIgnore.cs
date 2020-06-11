using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveIgnore : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnValidate()
    {
        DrawJoints(transform);
    }

    public void DrawJoints(Transform joint)
    {
        
        if (joint.name.EndsWith("Ignore"))
            joint.name = joint.name.Replace("Ignore", "aux").Trim();

        for (int i = 0; i < joint.childCount; ++i)
        {
            Transform child = joint.GetChild(i);
            if (child.name.EndsWith("Ignore"))
                child.name = child.name.Replace("Ignore", "aux").Trim();
            DrawJoints(child);
        }
    }
}
