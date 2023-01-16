using Unity.XR.CoreUtils;
using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class MoveToLocation : MonoBehaviour
    {
        public Transform location;
        private XROrigin rig;
        private PlayerClimbingXR[] climbingHands;

        private void Awake()
        {
            rig = Camera.main.GetComponentInParent<XROrigin>();
            climbingHands = rig.GetComponentsInChildren<PlayerClimbingXR>(true);
        }

        public void Activate()
        {
            CancelClimbing();
            Vector3 heightAdjustment = rig.transform.up * rig.CameraInOriginSpaceHeight;
            Vector3 cameraDestination = location.position + heightAdjustment;
            rig.MoveCameraToWorldLocation(cameraDestination);
        }

        private void CancelClimbing()
        {
            foreach (var h in climbingHands) h.CancelClimbing();
        }
    }
}