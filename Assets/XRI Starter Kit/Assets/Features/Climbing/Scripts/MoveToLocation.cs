using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    public class MoveToLocation : MonoBehaviour
    {
        public Transform location;
        public UnityEvent OnTeleport;
        protected XROrigin rig;
        private PlayerClimbingXR[] climbingHands;

        protected virtual void Awake()
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
            OnTeleport.Invoke();
        }
        public void TeleportWithHeadAtLocation()
        {
            CancelClimbing();
            rig.MoveCameraToWorldLocation(location.position);
            OnTeleport.Invoke();
        }

        public void TeleportWithHeadAtLocationAndRotate()
        {
            TeleportWithHeadAtLocation();
            rig.MatchOriginUpOriginForward(location.up, location.forward);
            OnTeleport.Invoke();
        }
        public void TeleportWithFeetAtLocationAndRotate()
        {
            TeleportWithFeetAtLocation();
            rig.MatchOriginUpOriginForward(location.up, location.forward);
            OnTeleport.Invoke();
        }

        private void CancelClimbing()
        {
            foreach (var h in climbingHands) h.CancelClimbing();
        }
    }
}