using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class HandSolveContext
    {
        public HandAnimator hand;
        public HandAnimator.HandFingerMap fingerMap;
        public PoseScriptableObject openPose;
        public PoseScriptableObject closedPose;
        // Optional candidate closed poses. When non-empty the solver picks, per finger, the one
        // whose fingertip ends closest to the target. Falls back to closedPose when null/empty.
        public List<PoseScriptableObject> closedPoses;
        public Collider[] targetColliders;
        public LayerMask targetMask;
        public int stepCount = 15;
        public float probeRadius = 0.01f;
        // How many joints from the fingertip inward to sphere-test each step. 1 = tip only;
        // 2+ catches a finger wrapping the object even when the tip slips past it.
        public int samplesPerFinger = 2;
        // Curl for a finger that contacts nothing (progressive solver). Like a human grabbing, the
        // non-gripping fingers settle into a relaxed fist rather than splaying open. 1 = full fist,
        // 0 = fully open; ~0.7 reads as a natural relaxed grip.
        public float noContactCurl = 0.7f;
        // When true the solver records per-joint debug telemetry into its LastSolveDebug for the gizmo
        // drawer. Adds a little work, so leave off unless a HandPoseSolveDebugDrawer is showing it.
        public bool collectDebug;
    }
}
