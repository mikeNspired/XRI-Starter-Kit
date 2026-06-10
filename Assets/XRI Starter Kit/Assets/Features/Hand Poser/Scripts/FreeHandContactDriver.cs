using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Free (non-grabbing) hand world touch. While the hand is empty, runs the dynamic
    /// <see cref="IHandPoseSolver"/> every frame against a configurable world <see cref="LayerMask"/> and
    /// writes the result straight onto the joints (no blend coroutine), so fingers rest on surfaces and
    /// drape over edges. The progressive solver resolves each finger independently — fingers over a table
    /// edge curl down while fingers still on the surface stay flat.
    ///
    /// Kinematic only: sphere queries + direct transform writes. No physics forces, ArticulationBodies, or IK.
    /// Pauses entirely while grabbing (the Phase 5 grab path owns the pose then). Off by default.
    ///
    /// Contact-owner rule: this driver and any persistent physical-presence finger colliders must not both
    /// own contact. While Enable Free Hand Touch is on, disable those colliders or set them to triggers.
    /// This driver intentionally does NOT wire that feature in — it runs its own queries against the World Mask.
    /// </summary>
    public class FreeHandContactDriver : MonoBehaviour
    {
        #region Inspector

        [Tooltip("Hand to drive. Auto-filled from this GameObject if left empty.")]
        [SerializeField] private HandAnimator handAnimator;

        [Tooltip("Master toggle. Off = the hand poses normally and this component does nothing.")]
        [SerializeField] private bool enableFreeHandTouch = false;

        [Tooltip("Only geometry on these layers is touched. Set this to your world/environment layer(s) — " +
                 "never include the player body or the other hand.")]
        [SerializeField] private LayerMask worldMask;

        [Tooltip("Center of the reach query. Defaults to this transform (the hand) if empty.")]
        [SerializeField] private Transform probeCenter;

        [Tooltip("Radius (m) around the hand to gather nearby world colliders each frame.")]
        [SerializeField] private float reachRadius = 0.15f;

        [Tooltip("Max world colliders considered per frame (reused buffer; no per-frame allocation here).")]
        [SerializeField] private int maxColliders = 32;

        [Header("Solver (kept low for per-frame cost)")]
        [Tooltip("Use the per-joint progressive solver (independent fingers / edge drape). " +
                 "Off uses the simpler single-t-per-finger curl sweep.")]
        [SerializeField] private bool useProgressiveSolver = true;

        [Tooltip("Curl-sweep resolution per finger. Lower than the grab solver to bound per-frame cost.")]
        [SerializeField, Range(4, 20)] private int stepCount = 8;

        [Tooltip("Probe sphere radius (m) for contact detection.")]
        [SerializeField] private float probeRadius = 0.012f;

        [Tooltip("Joints from the fingertip inward to sphere-test each step.")]
        [SerializeField, Range(1, 4)] private int samplesPerFinger = 2;

        [Tooltip("Curl for a finger that touches nothing (progressive solver), so it drapes naturally " +
                 "instead of splaying open. 1 = fist, 0 = open.")]
        [SerializeField, Range(0, 1)] private float noContactCurl = 0.5f;

        [Tooltip("When the hand leaves all surfaces, snap once back to the idle pose so it doesn't freeze " +
                 "in the last touched shape.")]
        [SerializeField] private bool returnToIdleWhenClear = true;

        #endregion

        #region Private Fields

        private IHandPoseSolver solver;
        private HandSolveContext ctx;
        private Collider[] colliders;
        private bool isPosing;          // wrote a touched pose last evaluated frame
        private bool warned;

        #endregion

        #region Unity Lifecycle

        private void Reset() => handAnimator = GetComponent<HandAnimator>();

        private void Awake()
        {
            if (!handAnimator) TryGetComponent(out handAnimator);
            colliders = new Collider[Mathf.Max(1, maxColliders)];
        }

        // LateUpdate so this writes AFTER the trigger/grip value animations (which run in the normal
        // update phase). When this skips a frame, those animations naturally reassert the idle pose.
        private void LateUpdate()
        {
            if (!enableFreeHandTouch || !handAnimator || handAnimator.isGrabbingObject)
            {
                ReleasePosing();
                return;
            }

            if (!Ready()) return;

            Vector3 center = (probeCenter ? probeCenter : transform).position;
            int count = Physics.OverlapSphereNonAlloc(center, reachRadius, colliders, worldMask,
                                                      QueryTriggerInteraction.Ignore);
            // Drop stale references beyond the live hits so they can't match during the solve.
            for (int i = count; i < colliders.Length; i++) colliders[i] = null;

            if (count == 0)
            {
                ReleasePosing();
                return;
            }

            BuildContext();
            var result = solver.Solve(ctx);
            handAnimator.LastSolveDebug = solver.LastSolveDebug;

            if (result != null && result.Length > 0)
            {
                handAnimator.SetJointsImmediate(result);
                isPosing = true;
            }
        }

        #endregion

        #region Private Methods

        private bool Ready()
        {
            if (handAnimator.ClosedPose && (handAnimator.OpenPose || handAnimator.DefaultPose))
                return true;

            if (!warned)
            {
                Debug.LogWarning($"[FreeHandContactDriver] {name} — assign ClosedPose and an OpenPose/DefaultPose " +
                                 "on the HandAnimator to enable free-hand touch.", this);
                warned = true;
            }
            return false;
        }

        private void BuildContext()
        {
            ctx ??= new HandSolveContext();
            solver ??= useProgressiveSolver ? new ProgressiveCurlSolver() : (IHandPoseSolver)new CurlSweepSolver();

            ctx.hand             = handAnimator;
            ctx.fingerMap        = handAnimator.fingerMap;
            ctx.openPose         = handAnimator.OpenPose ? handAnimator.OpenPose : handAnimator.DefaultPose;
            ctx.closedPose       = handAnimator.ClosedPose;
            ctx.closedPoses      = handAnimator.ClosedPoses;
            ctx.targetColliders  = colliders;
            ctx.targetMask       = worldMask;
            ctx.stepCount        = stepCount;
            ctx.probeRadius      = probeRadius;
            ctx.samplesPerFinger = samplesPerFinger;
            ctx.noContactCurl    = noContactCurl;
            ctx.collectDebug     = HandPoserSettings.Instance && HandPoserSettings.Instance.drawSolveDebug;
        }

        // Hand has left all surfaces (or driver disabled / grabbing): hand back to normal posing.
        // Writes DefaultPose directly (no coroutine stop) so a held trigger/grip animation keeps control
        // and simply overwrites this on its next frame; only matters when nothing else is driving the hand.
        private void ReleasePosing()
        {
            if (!isPosing) return;
            isPosing = false;
            // Leave handAnimator.LastSolveDebug as-is: nulling it here would wipe a fresh grab's telemetry
            // (XRHandPoser sets it during the grab's Update, before this LateUpdate). The drawer simply
            // shows the last solve until something solves again.
            if (returnToIdleWhenClear && handAnimator && !handAnimator.isGrabbingObject && handAnimator.DefaultPose)
                handAnimator.SetJointsImmediate(handAnimator.DefaultPose.joints);
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = enableFreeHandTouch ? Color.cyan : Color.gray;
            Vector3 center = (probeCenter ? probeCenter : transform).position;
            Gizmos.DrawWireSphere(center, reachRadius);
        }
#endif
    }
}
