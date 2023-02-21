using Unity.XR.CoreUtils;
using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class TeleportGrabbable : MoveToLocation
    {
        public new void Activate()
        {
            XROrigin rig = Camera.main.GetComponentInParent<XROrigin>();
            var hands = rig.GetComponentsInChildren<PlayerClimbingXR>();
            foreach (var h in hands)
            {
                h.CancelClimbing();
            }

            base.TeleportWithFeetAtLocation();
        }
    }
}