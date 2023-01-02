using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MoveToLocation : MonoBehaviour
{
    public Transform location;

    public void Activate()
    {
        XROrigin rig = Camera.main.GetComponentInParent<XROrigin>();
        Vector3 heightAdjustment = rig.transform.up * rig.CameraInOriginSpaceHeight;

        Vector3 cameraDestination = location.position + heightAdjustment;
        rig.MoveCameraToWorldLocation(cameraDestination);
    }
}