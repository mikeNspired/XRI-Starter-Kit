using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class HandSolveContext
    {
        public HandAnimator hand;
        public HandAnimator.HandFingerMap fingerMap;
        public PoseScriptableObject openPose;
        public PoseScriptableObject closedPose;
        public Collider[] targetColliders;
        public LayerMask targetMask;
        public int stepCount = 15;
        public float probeRadius = 0.01f;
    }
}
