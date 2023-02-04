using Unity.XR.CoreUtils;
using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class MoveToLocation : MonoBehaviour
    {
        public Transform location;
        protected XROrigin rig;
        private PlayerClimbingXR[] climbingHands;
        
        private void Awake()
        {
            rig = Camera.main.GetComponentInParent<XROrigin>();
            climbingHands = rig.GetComponentsInChildren<PlayerClimbingXR>(true);
        }

        public void TeleportWithFeetAtLocation()
        {
            CancelClimbing();
            Vector3 heightAdjustment = rig.transform.up * rig.CameraInOriginSpaceHeight;
            Vector3 cameraDestination = location.position + heightAdjustment;
            rig.MoveCameraToWorldLocation(cameraDestination);
        }
        public void TeleportWithHeadAtLocation()
        {
            CancelClimbing();
            rig.MoveCameraToWorldLocation(location.position);
        }

        public void TeleportWithHeadAtLocationAndRotate()
        {
            TeleportWithHeadAtLocation();
            rig.MatchOriginUpOriginForward(location.up, location.forward);
        }
        public void TeleportWithFeetAtLocationAndRotate()
        {
            TeleportWithFeetAtLocation();
            rig.MatchOriginUpOriginForward(location.up, location.forward);
        }

        private void CancelClimbing()
        {
            foreach (var h in climbingHands) h.CancelClimbing();
        }
    }
}