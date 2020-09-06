using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class JoystickTut : MonoBehaviour
{
    public Transform hand;

    public Transform projectionPoint;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Update()
    {
        //Projection
        Vector3 positionToProject = hand.position;
        Vector3 v = positionToProject - transform.position;
        Vector3 projection = Vector3.ProjectOnPlane(v, transform.up);
        projectionPoint.position = projection;
        // Vector3 projectedPoint;
        // if (xAxis & yAxis)
        //     projectedPoint = transform.position + Vector3.ClampMagnitude(projection, 1);
        // else
        //     projectedPoint = transform.position + new Vector3(Mathf.Clamp(projection.x, -1, 1), 0, Mathf.Clamp(projection.z, -1, 1));
        //
        // var locRot = transform.InverseTransformPoint(projectedPoint);
        //
        // // locRot = Vector3.ClampMagnitude(locRot, shaftLength);
        //
        // SetPosition(locRot);
    }
}
