using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToFloorOnStart : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10))
        {
            transform.position = hit.point;
        }
    }


}
