using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MoveToLocation : MonoBehaviour
{
    public Transform location;

    public void Activate()
    {
        XRRig rig = Camera.main.GetComponentInParent<XRRig>();
        Vector3 heightAdjustment = rig.transform.up * rig.cameraInRigSpaceHeight;

        Vector3 cameraDestination = location.position + heightAdjustment;
        rig.MoveCameraToWorldLocation(cameraDestination);
    }
}