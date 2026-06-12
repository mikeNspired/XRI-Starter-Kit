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
        // Optional solver-only fingertip probes (thumb=0 … pinky=4, entries may be null), supplied by a
        // HandDynamicPoses component on the hand. Used as the outermost contact point on skeletons whose
        // last finger bone is the distal knuckle. Null when the hand has no dynamic-poses component.
        public Transform[] tipProbes;
        public int stepCount = 15;
        public float probeRadius = 0.01f;
        // How many joints from the fingertip inward to sphere-test each step. 1 = tip only;
        // 2+ catches a finger wrapping the object even when the tip slips past it.
        public int samplesPerFinger = 2;
        // Contact samples along each tested bone segment (progressive solver). 2 = midpoint + endpoint
        // (the original behavior); higher catches thin geometry slipping between samples, so an
        // edge-wrap reads as a drape instead of a claw. Bounded small — this multiplies query count.
        public int samplesPerSegment = 2;
        // Optional per-finger calibration: enable mask, probe-radius scale, max-curl clamp, press bias.
        // Null = all fingers solve with defaults (see PerFingerSolveSettings.Get).
        public PerFingerSolveSettings fingerSettings;
        // Curl for a finger that contacts nothing (progressive solver). Like a human grabbing, the
        // non-gripping fingers settle into a relaxed fist rather than splaying open. 1 = full fist,
        // 0 = fully open; ~0.7 reads as a natural relaxed grip.
        public float noContactCurl = 0.7f;
        // After a finger has gripped upstream, how much each further-out joint keeps curling when it
        // finds nothing — a gentle wrap continuing the finger's spiral — instead of snapping straight
        // to a full fist (the distal "claw"). 0 = stop dead at the last grip; ~0.33 reads natural.
        public float distalFollowCurl = 0.33f;
        // When true the solver records per-joint debug telemetry into its LastSolveDebug for the gizmo
        // drawer. Adds a little work, so leave off unless a HandPoseSolveDebugDrawer is showing it.
        public bool collectDebug;
    }
}
