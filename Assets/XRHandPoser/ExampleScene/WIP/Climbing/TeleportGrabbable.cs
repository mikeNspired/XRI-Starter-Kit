using Unity.XR.CoreUtils;
using UnityEngine;

public class TeleportGrabbable : MoveToLocation
{
    public void Activate()
    {
        XROrigin rig = Camera.main.GetComponentInParent<XROrigin>();
        var hands = rig.GetComponentsInChildren<PlayerClimbingXR>();
        foreach (var h in hands)
        {
            h.CancelClimbing();
        }
        base.Activate();
    }
}