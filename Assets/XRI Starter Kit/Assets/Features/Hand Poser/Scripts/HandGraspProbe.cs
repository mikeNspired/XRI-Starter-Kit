using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// How <see cref="HandGraspProbe"/> decides when to solve a grasp onto nearby world geometry.
    /// </summary>
    public enum GraspProbeMode
    {
        /// Editor-only dial-in: solve continuously while the hand is empty and an object is in reach,
        /// so you can tune the grab settings live without an actual XRI grab. Never runs in a build.
        AutoGraspTest,

        /// Runtime: solve only while the grip button is held and a non-grabbable object is in reach,
        /// so the hand poses onto a surface (e.g. a table) the player cannot normally grab.
        GripHoldPose,
    }

    /// <summary>
    /// Drives the dynamic grab solver against nearby world geometry while the hand is NOT holding anything,
    /// and applies the result straight onto the joints (no blend coroutine). This is the *grasp* mechanic —
    /// fingers curl toward the surface — used here as a testing/utility tool, NOT the free-hand world
    /// reaction (that pushback feature is a separate, deferred script).
    ///
    /// Two modes (see <see cref="GraspProbeMode"/>): an editor-only continuous "auto grasp" for dialing in
    /// grab settings, and a runtime "grip-hold pose" that poses the hand onto a surface while the grip is held.
    ///
    /// Kinematic only: sphere queries + direct transform writes. No physics forces, ArticulationBodies, or IK.
    /// Pauses entirely while grabbing (the Phase 5 grab path owns the pose then). Off by default.
    ///
    /// Contact-owner rule: this and any persistent physical-presence finger colliders must not both own
    /// contact. While this runs, disable those colliders or set them to triggers. This driver does NOT wire
    /// that feature in — it runs its own queries and ignores the hand's own colliders (see Ignore Collider Root).
    /// </summary>
    public class HandGraspProbe : MonoBehaviour
    {
        #region Inspector

        [Tooltip("Hand to drive. Auto-filled from this GameObject if left empty.")]
        [SerializeField] private HandAnimator handAnimator;

        [Tooltip("Master toggle. Off = the hand poses normally and this component does nothing.")]
        [SerializeField] private bool enableProbe = false;

        [Tooltip("AutoGraspTest: editor-only, solves continuously to dial in grab settings. " +
                 "GripHoldPose: runtime, solves only while the grip button is held near a surface.")]
        [SerializeField] private GraspProbeMode mode = GraspProbeMode.GripHoldPose;

        [Header("Grip (GripHoldPose mode)")]
        [Tooltip("Grip input source. Auto-found in parents if empty. Falls back to the hand's grip animation " +
                 "value when none is assigned.")]
        [SerializeField] private XRControllerButtons controllerButtons;

        [Tooltip("Grip value (0..1) at/above which GripHoldPose mode is considered 'held'.")]
        [SerializeField, Range(0f, 1f)] private float gripThreshold = 0.5f;

        [Header("World Query")]
        [Tooltip("Only geometry on these layers is posed onto. Set to your non-grabbable environment layer(s) — " +
                 "never include the player body or the other hand. (Own colliders are excluded regardless.)")]
        [SerializeField] private LayerMask worldMask;

        [Tooltip("Center of the reach query. Defaults to this transform (the hand) if empty.")]
        [SerializeField] private Transform probeCenter;

        [Tooltip("Radius (m) around the hand to gather nearby world colliders each frame.")]
        [SerializeField] private float reachRadius = 0.15f;

        [Tooltip("Max world colliders considered per frame (reused buffer; no per-frame allocation here).")]
        [SerializeField] private int maxColliders = 32;

        [Tooltip("Colliders anywhere under this transform are ignored, so a broad World Mask can never make " +
                 "the hand collide with itself, the arm, or the other hand. Leave empty to auto-use this " +
                 "hand's hierarchy root (the XR rig) at runtime.")]
        [SerializeField] private Transform ignoreColliderRoot;

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

        [Tooltip("After a finger grips, how far each further-out joint keeps curling when it finds " +
                 "nothing (a gentle wrap) instead of fisting. ~0.33 reads natural.")]
        [SerializeField, Range(0, 1)] private float distalFollowCurl = 0.33f;

        [Tooltip("Per-frame smoothing time-constant (seconds). 0 = snap instantly to the solved pose; " +
                 "small values (e.g. 0.05) damp solver jitter so fingers settle instead of flicking.")]
        [SerializeField, Range(0f, 0.3f)] private float smoothing = 0.04f;

        [Tooltip("When the trigger condition ends (released / nothing in reach), snap once back to the idle " +
                 "pose so the hand doesn't freeze in the last solved shape.")]
        [SerializeField] private bool returnToIdleWhenClear = true;

        #endregion

        #region Private Fields

        private IHandPoseSolver solver;
        private bool solverIsProgressive;
        private HandSolveContext ctx;
        private Collider[] colliders;
        private Transform ignoreRoot;   // resolved hand/rig root whose colliders are skipped
        private bool ignoreRootResolved;
        private bool isPosing;          // wrote a solved pose last evaluated frame
        private bool warned;
        private bool warnedNoMask;

        // Smoothing state: the probe's own last-written pose, per joint name. Smoothing must blend
        // against THIS, not the live joints — the grip/trigger value animations also write the joints
        // every Update (before this LateUpdate), and lerping from their output would let them dilute
        // the solved pose forever (the hand would hang mostly fisted instead of settling on the surface).
        private readonly System.Collections.Generic.Dictionary<string, TransformStruct> displayedPose
            = new System.Collections.Generic.Dictionary<string, TransformStruct>();
        private PoseScriptableObject.JointData[] writeBuffer;
        private System.Collections.Generic.Dictionary<string, Transform> jointByName;

        #endregion

        #region Unity Lifecycle

        private void Reset() => handAnimator = GetComponent<HandAnimator>();

        private void Awake()
        {
            if (!handAnimator) TryGetComponent(out handAnimator);
            if (!controllerButtons) controllerButtons = GetComponentInParent<XRControllerButtons>();
            colliders = new Collider[Mathf.Max(1, maxColliders)];
        }

        // LateUpdate so this writes AFTER the trigger/grip value animations (which run in the normal
        // update phase). When this skips a frame, those animations naturally reassert the idle pose.
        private void LateUpdate()
        {
            if (!enableProbe || !handAnimator || handAnimator.isGrabbingObject || !ShouldSolveThisFrame())
            {
                ReleasePosing();
                return;
            }

            if (!Ready())
            {
                ReleasePosing();
                return;
            }

            Vector3 center = (probeCenter ? probeCenter : transform).position;
            int hits = Physics.OverlapSphereNonAlloc(center, reachRadius, colliders, worldMask,
                                                     QueryTriggerInteraction.Ignore);

            // Compact out the hand/player's own colliders so a broad mask can't self-collide, and null the
            // tail so stale references can't match. The solver's contact test only counts colliders that
            // remain in this buffer, so excluding them here fully prevents the hand grabbing itself.
            int count = FilterOwnColliders(hits);
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
                float lerp = smoothing <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime / smoothing);
                handAnimator.SetJointsImmediate(SmoothAgainstOwnOutput(result, lerp));
                isPosing = true;
            }
        }

        #endregion

        #region Private Methods

        // Mode-specific trigger gate.
        private bool ShouldSolveThisFrame() => mode switch
        {
            // Editor-only dial-in tool: never solve in a player build.
            GraspProbeMode.AutoGraspTest => Application.isEditor,
            GraspProbeMode.GripHoldPose  => GripHeld(),
            _                            => false,
        };

        private bool GripHeld()
        {
            if (controllerButtons)
                return controllerButtons.IsGripped || controllerButtons.gripValue >= gripThreshold;
            // Fallback: the hand's grip animation value is fed by the same input wiring.
            return handAnimator.gripAnimationValue >= gripThreshold;
        }

        private bool Ready()
        {
            if (worldMask == 0)
            {
                if (!warnedNoMask)
                {
                    Debug.LogWarning($"[HandGraspProbe] {name} — World Mask is set to Nothing, so the probe " +
                                     "can never find geometry. Set it to your environment layer(s).", this);
                    warnedNoMask = true;
                }
                return false;
            }

            if (handAnimator.ClosedPose && (handAnimator.OpenPose || handAnimator.DefaultPose))
                return true;

            if (!warned)
            {
                Debug.LogWarning($"[HandGraspProbe] {name} — assign ClosedPose and an OpenPose/DefaultPose " +
                                 "on the HandAnimator to enable the grasp probe.", this);
                warned = true;
            }
            return false;
        }

        private void BuildContext()
        {
            ctx ??= new HandSolveContext();
            // Re-created when the toggle changes so flipping it during play-mode dial-in takes effect.
            if (solver == null || solverIsProgressive != useProgressiveSolver)
            {
                solver = useProgressiveSolver ? (IHandPoseSolver)new ProgressiveCurlSolver() : new CurlSweepSolver();
                solverIsProgressive = useProgressiveSolver;
            }

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
            ctx.distalFollowCurl = distalFollowCurl;
            ctx.collectDebug     = handAnimator.requestSolveDebug ||
                                   (HandPoserSettings.Instance && HandPoserSettings.Instance.drawSolveDebug);
        }

        // Keeps only colliders NOT under the ignore root (hand / rig), compacting them to the front of the
        // buffer and nulling the rest. Returns the count of real world colliders to solve against.
        private int FilterOwnColliders(int _hits)
        {
            var root = IgnoreRoot();
            int w = 0;
            for (int i = 0; i < _hits; i++)
            {
                var col = colliders[i];
                if (!col) continue;
                if (root && col.transform.IsChildOf(root)) continue;
                colliders[w++] = col; // w <= i, so this never clobbers an unread entry
            }
            for (int i = w; i < colliders.Length; i++) colliders[i] = null;
            return w;
        }

        private Transform IgnoreRoot()
        {
            if (ignoreColliderRoot) return ignoreColliderRoot;
            if (!ignoreRootResolved)
            {
                ignoreRoot = handAnimator ? handAnimator.transform.root : null;
                ignoreRootResolved = true;
            }
            return ignoreRoot;
        }

        // Blends the fresh solve against the probe's own previous output (per joint, by name) and
        // returns a buffer to write authoritatively (snap). A joint seen for the first time seeds from
        // its live local pose so posing still eases in from wherever the hand currently is.
        private PoseScriptableObject.JointData[] SmoothAgainstOwnOutput(
            PoseScriptableObject.JointData[] _solved, float _lerp)
        {
            bool snap = _lerp >= 1f;
            if (writeBuffer == null || writeBuffer.Length != _solved.Length)
                writeBuffer = new PoseScriptableObject.JointData[_solved.Length];

            for (int i = 0; i < _solved.Length; i++)
            {
                var target = _solved[i];
                Vector3 pos = target.localPosition;
                Quaternion rot = target.localRotation;

                if (!snap)
                {
                    if (displayedPose.TryGetValue(target.jointName, out var prev))
                    {
                        pos = Vector3.Lerp(prev.position, target.localPosition, _lerp);
                        rot = Quaternion.Slerp(prev.rotation, target.localRotation, _lerp);
                    }
                    else if (JointByName().TryGetValue(target.jointName, out var joint) && joint)
                    {
                        pos = Vector3.Lerp(joint.localPosition, target.localPosition, _lerp);
                        rot = Quaternion.Slerp(joint.localRotation, target.localRotation, _lerp);
                    }
                }

                writeBuffer[i] = new PoseScriptableObject.JointData
                {
                    jointName = target.jointName, localPosition = pos, localRotation = rot
                };
                displayedPose[target.jointName] = new TransformStruct(pos, rot, Vector3.one);
            }
            return writeBuffer;
        }

        private System.Collections.Generic.Dictionary<string, Transform> JointByName()
        {
            if (jointByName != null) return jointByName;
            jointByName = new System.Collections.Generic.Dictionary<string, Transform>();
            foreach (var joint in handAnimator.currentJoints)
                if (joint && !jointByName.ContainsKey(joint.name)) jointByName[joint.name] = joint;
            return jointByName;
        }

        // Trigger ended (disabled / grabbing / grip released / nothing in reach): hand back to normal posing.
        // Writes DefaultPose directly (no coroutine stop) so a held trigger/grip animation keeps control and
        // simply overwrites this on its next frame; only matters when nothing else is driving the hand.
        private void ReleasePosing()
        {
            if (!isPosing) return;
            isPosing = false;
            displayedPose.Clear(); // next posing session eases in fresh from the live pose
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
            Gizmos.color = enableProbe ? Color.cyan : Color.gray;
            Vector3 center = (probeCenter ? probeCenter : transform).position;
            Gizmos.DrawWireSphere(center, reachRadius);
        }
#endif
    }
}
