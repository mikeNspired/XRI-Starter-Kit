using System.Collections.Generic;
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
    /// Feel: the pose eases in by how close the nearest surface is (no spider-claw the instant something
    /// enters reach), a deadband suppresses micro solver jitter (no crawling), and leaving reach fades back
    /// to idle over a short time (no snap). Kinematic only: sphere queries + direct transform writes. No
    /// physics forces, ArticulationBodies, or IK. Pauses entirely while grabbing. Off by default.
    ///
    /// Coexisting with physical-presence colliders (HandPhysicsColliders): the two are complementary —
    /// the colliders push world objects (a door), this wraps the fingers so it looks held. The only
    /// friction is the solver chasing a dynamic surface those colliders are pushing; the GripHoldPose
    /// "latch" (Latch Grip Hold) fixes that by acquiring the grasp once and holding the captured shape
    /// until release, so the fingers stop re-conforming to the moving object. This driver still does NOT
    /// wire the collider feature in — it runs its own queries and ignores the hand's own colliders
    /// (see Ignore Collider Root); the latch is purely a behavior of THIS component.
    ///
    /// Surface stick (GripHoldPose, opt-in via Stick Hand To Surface): the HAND ROOT also anchors to the
    /// gripped surface — it stays planted where you gripped (with a tunable leash 'give') and snaps back to
    /// the controller past a break distance, while the fingers keep re-conforming. Reuses the hand's
    /// MoveHandToTarget / ReturnHandToPlayer tracking; it is the spatial companion to the finger latch and
    /// takes precedence over it. The palm motion it produces is also the input a future fingertip-pinning
    /// IK solver would consume.
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

        [Tooltip("GripHoldPose only. On = solve ONCE when the grip engages near a surface, then HOLD that " +
                 "finger shape until release (re-acquiring if the first press caught nothing). This stops " +
                 "the fingers chasing a surface that the hand's physical colliders are pushing (e.g. a door " +
                 "the hand shoves while 'holding' it), and reads like a real hand pressing on something. " +
                 "Off = re-solve every frame, so the fingers re-conform as you slide along a surface " +
                 "(drape over an edge) at the cost of fighting a dynamic object the colliders move.")]
        [SerializeField] private bool latchGripHold = true;

        [Header("Surface Stick (GripHoldPose)")]
        [Tooltip("On = while gripping a surface the HAND ITSELF anchors to it (not just the fingers): it " +
                 "stays planted where you gripped as you move the controller, then snaps back when you pull " +
                 "too far. Fingers stay active and re-conform, so this OVERRIDES Latch Grip Hold. " +
                 "Off = the hand follows the controller as usual and only the fingers pose.")]
        [SerializeField] private bool stickHandToSurface = false;

        [Tooltip("How much the anchored hand follows the controller. 0 = planted rigidly on the surface; " +
                 "1 = no stick. A small value keeps it mostly planted but lets it 'give' toward the " +
                 "controller as you pull, so the detach reads as straining rather than a frozen hand.")]
        [SerializeField, Range(0f, 1f)] private float leashWeight = 0.15f;

        [Tooltip("Controller-to-anchor distance (m) at which the hand lets go and snaps back to the " +
                 "controller. Keep tight (~0.1–0.15) so the snap is small and reads as the grip slipping.")]
        [SerializeField] private float breakDistance = 0.12f;

        [Tooltip("Invoked when the hand snaps back because you pulled past Break Distance (NOT on a normal " +
                 "grip release). Hook a sound or haptic here.")]
        [SerializeField] private UnityEngine.Events.UnityEvent onHandSnapBack;

        /// 0 when not anchored, rising to 1 at the break distance. A future "hand shakes as it nears the
        /// snap" effect can read this; also handy for haptics / UI.
        public float StrainNormalized => sticking ? strain : 0f;

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

        [Header("Feel")]
        [Tooltip("Ease the grasp in by how close the surface is, so the hand doesn't snap to a full grasp " +
                 "(the 'spider claw') the instant something enters reach. OFF = commit fully to the solved " +
                 "pose whenever anything is in reach. Turn OFF first if the fingers fall short of the " +
                 "object — that is almost always this easing diluting the pose, not the solver.")]
        [SerializeField] private bool proximityEaseIn = true;

        [Tooltip("Proximity ease-in only. Distance (m) from the HAND (probe center — NOT the fingertips) " +
                 "to the nearest surface at which the grasp fully commits; farther than this it blends " +
                 "toward the idle pose. Because it is measured from the hand center, the realistic range is " +
                 "larger than it looks (~0.06–0.10) and it MUST stay below Reach Radius — at or above it the " +
                 "weight degenerates and the grasp never forms (auto-clamped to keep it valid).")]
        [SerializeField] private float fullPoseDistance = 0.07f;

        [Tooltip("Ignore solved-joint rotation changes smaller than this (deg) so micro solver noise doesn't " +
                 "make the fingers crawl. Deliberate motion still passes through at full speed.")]
        [SerializeField] private float rotationDeadband = 1.5f;

        [Tooltip("Ignore solved-joint position changes smaller than this (m). Companion to Rotation Deadband.")]
        [SerializeField] private float positionDeadband = 0.0005f;

        [Tooltip("Per-frame smoothing time-constant (seconds). 0 = snap instantly to the solved pose; " +
                 "small values (e.g. 0.05) damp residual jitter. With the deadband doing the heavy lifting " +
                 "this can stay small, avoiding the slow-motion feel of high smoothing.")]
        [SerializeField, Range(0f, 0.3f)] private float smoothing = 0.04f;

        [Tooltip("Stops a finger flickering between Closed candidate poses as the hand/object moves: it " +
                 "keeps its current candidate unless another fits closer by more than this margin (m). " +
                 "0 = pick the best fit every frame (old behaviour, jitter-prone with multiple candidates); " +
                 "~0.003–0.008 stops the jitter while still switching on a clearly better fit; very high ≈ " +
                 "lock the pose once chosen. Continuous modes only (AutoGraspTest / unlatched " +
                 "GripHoldPose); the one-shot grab is unaffected.")]
        [SerializeField] private float candidateStickiness = 0.005f;

        [Tooltip("When the trigger ends (left reach / grip released), ease back to idle over this many " +
                 "seconds instead of snapping. Keep short so it doesn't feel like slow motion. " +
                 "0 (or Return To Idle off) = stop immediately.")]
        [SerializeField] private float releaseFadeTime = 0.2f;

        [Tooltip("Ease back to the idle pose when the trigger ends. Off = freeze wherever the solve left off " +
                 "(the normal trigger/grip animations then reassert control).")]
        [SerializeField] private bool returnToIdleWhenClear = true;

        [Header("Solver")]
        [Tooltip("Off = solve with the global solver settings from the HandPoserSettings asset (single " +
                 "source of truth — the probe tracks whatever you dial in there). On = use the " +
                 "probe-local block below, typically cheaper values since this can solve every frame.")]
        [SerializeField] private bool overrideSolverSettings = false;

        [Tooltip("Probe-local solver tuning, used only when Override Solver Settings is on. Defaults are " +
                 "deliberately cheaper than the grab path (fewer steps, smaller radius, softer rest curl) " +
                 "to bound per-frame cost.")]
        [SerializeField] private HandSolverSettings solverSettings = new HandSolverSettings
        {
            stepCount = 8,
            probeRadius = 0.012f,
            noContactCurl = 0.5f,
        };

        // Effective solver block: probe-local override, else the global asset (which may be absent
        // in a stripped setup — then the local block is still a safe fallback).
        private HandSolverSettings Solver =>
            overrideSolverSettings || !HandPoserSettings.Instance ? solverSettings : HandPoserSettings.Instance.dynamicSolver;

        #endregion

        #region Private Fields

        private IHandPoseSolver solver;
        private bool solverIsProgressive;
        private HandDynamicPoses dyn;          // solver poses + fingertip probes, resolved once from the hand
        private bool dynResolved;
        private HandSolveContext ctx;
        private Collider[] colliders;
        private Transform ignoreRoot;   // resolved hand/rig root whose colliders are skipped
        private bool ignoreRootResolved;
        private bool isPosing;          // wrote a solved pose last evaluated frame
        private bool warned;
        private bool warnedNoMask;

        // Latch state (GripHoldPose + latchGripHold): once a grasp is acquired we hold latchedResult
        // and stop re-solving until the grip releases. lastResult is the most recent solver output
        // (the solver returns a fresh, caller-owned array each call, so capturing it is safe).
        private bool latched;
        private PoseScriptableObject.JointData[] lastResult;
        private PoseScriptableObject.JointData[] latchedResult;

        // The probe's own last-written ("displayed") pose, per joint name, and the deadbanded target it is
        // smoothing toward. Both must be the probe's OWN state, not the live joints — the grip/trigger value
        // animations also write the joints every Update (before this LateUpdate), so smoothing against the
        // live pose would let them dilute the solve every frame.
        private readonly Dictionary<string, TransformStruct> displayedPose = new Dictionary<string, TransformStruct>();
        private readonly Dictionary<string, TransformStruct> targetPose    = new Dictionary<string, TransformStruct>();
        private readonly Dictionary<string, TransformStruct> fadeStart     = new Dictionary<string, TransformStruct>();
        private Dictionary<string, PoseScriptableObject.JointData> idleByName; // DefaultPose lookup for the blend
        private Dictionary<string, Transform> jointByName;
        private PoseScriptableObject.JointData[] writeBuffer;

        private bool fading;
        private float fadeElapsed;

        // Surface-stick anchor state (GripHoldPose + stickHandToSurface).
        private Transform stickAnchor;     // child of the gripped collider, frozen at the grip-time hand pose
        private Transform leashTarget;     // transform the hand tracks each frame (anchor ↔ controller blend)
        private Transform controllerMount; // hand's parent captured at acquire (where the hand "should" be)
        private Vector3 handLocalPos;
        private Quaternion handLocalRot;
        private bool sticking;
        private float strain;

        #endregion

        #region Unity Lifecycle

        private void Reset() => handAnimator = GetComponent<HandAnimator>();

        private void Awake()
        {
            if (!handAnimator) TryGetComponent(out handAnimator);
            if (!controllerButtons) controllerButtons = GetComponentInParent<XRControllerButtons>();
            colliders = new Collider[Mathf.Max(1, maxColliders)];
        }

        private void OnDisable()
        {
            ReleaseStick(false); // re-parent the hand before we stop, so it never stays detached
            HardStop();
        }

        private void OnValidate()
        {
            // Full-pose distance must stay strictly below reach radius: ProximityWeight does
            // InverseLerp(reachRadius, fullPoseDistance, …), which degenerates to a never-commit 0 when
            // the two are equal (the "0.15 ruins the pose" case). Keep a margin so it always works.
            if (reachRadius < 0.01f) reachRadius = 0.01f;
            fullPoseDistance = Mathf.Clamp(fullPoseDistance, 0f, reachRadius * 0.95f);
            if (breakDistance < 0.01f) breakDistance = 0.01f;
        }

        // LateUpdate so this writes AFTER the trigger/grip value animations (which run in the normal update
        // phase). When this isn't posing, those animations naturally reassert the idle pose.
        private void LateUpdate()
        {
            if (!enableProbe || !handAnimator) { ReleaseStick(false); HardStop(); return; }
            // Never fight the grab blend — the Phase 5 grab path owns the pose while holding something.
            if (handAnimator.isGrabbingObject) { ReleaseStick(false); HardStop(); return; }

            // Surface stick (GripHoldPose): the hand root anchors to the gripped surface and the fingers
            // re-conform continuously. Takes precedence over the latch.
            if (UseStick()) { StickUpdate(); return; }
            if (sticking) ReleaseStick(false); // mode/toggle changed while anchored — clean up

            // Latch path (GripHoldPose): acquire a grasp once, then HOLD it without re-solving so the
            // fingers don't chase a surface the hand's physical colliders are simultaneously pushing.
            if (UseLatch() && ShouldSolveThisFrame())
            {
                if (latched) { ApplySolved(latchedResult, 1f); return; }   // hold the captured shape
                if (Ready() && TrySolve()) { latched = true; latchedResult = lastResult; return; }
                FadeToIdle();   // grip held but nothing in reach yet — keep trying to acquire
                return;
            }

            // Continuous path (AutoGraspTest, or GripHoldPose with latch off): re-solve every frame.
            latched = false;
            bool posed = ShouldSolveThisFrame() && Ready() && TrySolve();
            if (!posed) FadeToIdle();
        }

        // True while a held grasp should hold its captured pose instead of re-solving.
        private bool UseLatch() => mode == GraspProbeMode.GripHoldPose && latchGripHold;

        // True when the hand-root surface anchor is active (overrides the finger latch).
        private bool UseStick() => mode == GraspProbeMode.GripHoldPose && stickHandToSurface;

        #endregion

        #region Solve + apply

        // Gathers nearby world colliders, solves, and applies the proximity-weighted, deadbanded, smoothed
        // pose. Returns false (→ fade to idle) when nothing solvable is in reach.
        private bool TrySolve()
        {
            Vector3 center = (probeCenter ? probeCenter : transform).position;
            int hits = Physics.OverlapSphereNonAlloc(center, reachRadius, colliders, worldMask,
                                                     QueryTriggerInteraction.Ignore);

            // Compact out the hand/player's own colliders so a broad mask can't self-collide, and null the
            // tail so stale references can't match. The solver's contact test only counts colliders that
            // remain in this buffer, so excluding them here fully prevents the hand grabbing itself.
            int count = FilterOwnColliders(hits);
            if (count == 0) return false;

            BuildContext();
            var result = solver.Solve(ctx);
            handAnimator.LastSolveDebug = solver.LastSolveDebug;
            if (result == null || result.Length == 0) return false;
            lastResult = result; // caller-owned; safe to capture for the latch hold

            float weight = ProximityWeight(center, count);
            ApplySolved(result, weight);
            isPosing = true;
            fading = false;
            return true;
        }

        // 0 at the edge of reach (barely deviate from idle) → 1 at/under fullPoseDistance (full solve),
        // smoothstepped. Drives the ease-in so the hand doesn't claw the instant something enters reach.
        private float ProximityWeight(Vector3 _center, int _count)
        {
            if (!proximityEaseIn) return 1f; // commit fully whenever something is in reach

            float nearest = float.PositiveInfinity;
            for (int i = 0; i < _count; i++)
            {
                var col = colliders[i];
                if (!col) continue;
                // ClosestPoint only supports primitives + convex meshes; ClosestPointOnBounds (AABB) is a
                // fine, error-free approximation for a fade weight on arbitrary environment meshes.
                Vector3 cp = SupportsClosestPoint(col) ? col.ClosestPoint(_center) : col.ClosestPointOnBounds(_center);
                float d = Vector3.Distance(_center, cp);
                if (d < nearest) nearest = d;
            }
            if (float.IsPositiveInfinity(nearest)) return 1f;

            float w = Mathf.InverseLerp(reachRadius, Mathf.Max(fullPoseDistance, 0f), nearest);
            return Mathf.SmoothStep(0f, 1f, w);
        }

        // Blend solved → idle by proximity weight, freeze targets that barely moved (deadband), then smooth
        // the displayed pose toward the target and write it.
        private void ApplySolved(PoseScriptableObject.JointData[] _solved, float _weight)
        {
            float smoothLerp = smoothing <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime / smoothing);
            if (writeBuffer == null || writeBuffer.Length != _solved.Length)
                writeBuffer = new PoseScriptableObject.JointData[_solved.Length];

            for (int i = 0; i < _solved.Length; i++)
            {
                var s = _solved[i];
                string nm = s.jointName;

                // Proximity blend: ease from idle (weight 0, reach edge) to full solve (weight 1, contact).
                Vector3 tgtPos = s.localPosition;
                Quaternion tgtRot = s.localRotation;
                if (_weight < 1f && Idle(nm, out var idle))
                {
                    tgtPos = Vector3.Lerp(idle.localPosition, s.localPosition, _weight);
                    tgtRot = Quaternion.Slerp(idle.localRotation, s.localRotation, _weight);
                }

                // Deadband: keep the previous target unless this one moved enough (kills micro jitter).
                if (targetPose.TryGetValue(nm, out var prevTgt))
                {
                    bool moved = Quaternion.Angle(prevTgt.rotation, tgtRot) > rotationDeadband ||
                                 (prevTgt.position - tgtPos).sqrMagnitude > positionDeadband * positionDeadband;
                    if (!moved) { tgtPos = prevTgt.position; tgtRot = prevTgt.rotation; }
                }
                targetPose[nm] = new TransformStruct(tgtPos, tgtRot, Vector3.one);

                // Smooth the displayed pose toward the (deadbanded) target. Seed from the live joint the
                // first time so posing eases in from wherever the hand currently is.
                Vector3 pos; Quaternion rot;
                if (smoothLerp >= 1f) { pos = tgtPos; rot = tgtRot; }
                else if (displayedPose.TryGetValue(nm, out var disp))
                {
                    pos = Vector3.Lerp(disp.position, tgtPos, smoothLerp);
                    rot = Quaternion.Slerp(disp.rotation, tgtRot, smoothLerp);
                }
                else if (JointByName().TryGetValue(nm, out var joint) && joint)
                {
                    pos = Vector3.Lerp(joint.localPosition, tgtPos, smoothLerp);
                    rot = Quaternion.Slerp(joint.localRotation, tgtRot, smoothLerp);
                }
                else { pos = tgtPos; rot = tgtRot; }

                writeBuffer[i] = new PoseScriptableObject.JointData { jointName = nm, localPosition = pos, localRotation = rot };
                displayedPose[nm] = new TransformStruct(pos, rot, Vector3.one);
            }

            handAnimator.SetJointsImmediate(writeBuffer);
        }

        #endregion

        #region Release / fade

        // Not posing this frame. If we were, ease the displayed pose back to idle over releaseFadeTime so the
        // hand doesn't snap; once faded (or if fade is disabled) finalize and hand control back to normal posing.
        private void FadeToIdle()
        {
            if (!isPosing) return; // nothing of ours on the hand — normal animations drive it

            if (!returnToIdleWhenClear || !handAnimator.DefaultPose || releaseFadeTime <= 0f)
            {
                FinalizeIdle();
                return;
            }

            if (!fading)
            {
                fading = true;
                fadeElapsed = 0f;
                fadeStart.Clear();
                foreach (var kv in displayedPose) fadeStart[kv.Key] = kv.Value;
            }

            fadeElapsed += Time.deltaTime;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fadeElapsed / releaseFadeTime));

            var idleJoints = handAnimator.DefaultPose.joints;
            if (writeBuffer == null || writeBuffer.Length != idleJoints.Length)
                writeBuffer = new PoseScriptableObject.JointData[idleJoints.Length];

            for (int i = 0; i < idleJoints.Length; i++)
            {
                var idle = idleJoints[i];
                Vector3 pos = idle.localPosition; Quaternion rot = idle.localRotation;
                if (fadeStart.TryGetValue(idle.jointName, out var start))
                {
                    pos = Vector3.Lerp(start.position, idle.localPosition, e);
                    rot = Quaternion.Slerp(start.rotation, idle.localRotation, e);
                }
                writeBuffer[i] = new PoseScriptableObject.JointData { jointName = idle.jointName, localPosition = pos, localRotation = rot };
                displayedPose[idle.jointName] = new TransformStruct(pos, rot, Vector3.one);
            }
            handAnimator.SetJointsImmediate(writeBuffer);

            if (fadeElapsed >= releaseFadeTime) FinalizeIdle();
        }

        // Write the idle pose once (if asked) and drop all probe state so normal posing takes over.
        private void FinalizeIdle()
        {
            if (isPosing && returnToIdleWhenClear && handAnimator && handAnimator.DefaultPose && !handAnimator.isGrabbingObject)
                handAnimator.SetJointsImmediate(handAnimator.DefaultPose.joints);
            // Stop the debug drawer leaving the last grasp's spheres/labels floating in the scene after
            // the hand has gone idle. Only clears here (the probe was the active poser); the next solve
            // re-sets hasData, so a fresh grab's telemetry is untouched.
            if (handAnimator && handAnimator.LastSolveDebug != null) handAnimator.LastSolveDebug.hasData = false;
            HardStop();
        }

        // Drop all probe state without writing. Used when disabled or while grabbing (the grab owns the pose).
        // Leaves handAnimator.LastSolveDebug as-is so a fresh grab's telemetry isn't wiped.
        private void HardStop()
        {
            isPosing = false;
            fading = false;
            latched = false;
            if (displayedPose.Count > 0) displayedPose.Clear();
            if (targetPose.Count > 0) targetPose.Clear();
            if (fadeStart.Count > 0) fadeStart.Clear();
        }

        #endregion

        #region Surface stick

        // GripHoldPose + stickHandToSurface: keep the fingers solving continuously while anchoring the
        // hand root to the gripped surface with a leash, snapping back past the break distance.
        private void StickUpdate()
        {
            if (!ShouldSolveThisFrame() || !Ready())   // grip released / not set up → let go
            {
                if (sticking) ReleaseStick(false);
                FadeToIdle();
                return;
            }

            bool solved = TrySolve(); // fingers re-conform to the surface each frame

            if (!sticking)
            {
                if (solved) AcquireStick();  // first valid grasp anchors the hand
                else FadeToIdle();           // grip held but nothing in reach yet
                return;
            }

            UpdateLeash(); // already anchored: leash toward the controller, break if pulled too far
        }

        // Anchor the hand at its current pose: freeze a child transform under the gripped collider, start
        // the hand tracking a leash transform, and remember where the controller would otherwise hold it.
        private void AcquireStick()
        {
            controllerMount = handAnimator.transform.parent;
            handLocalPos    = handAnimator.transform.localPosition;
            handLocalRot    = handAnimator.transform.localRotation;

            Vector3 pos = handAnimator.transform.position;
            Quaternion rot = handAnimator.transform.rotation;

            var surface = NearestCollider();
            stickAnchor = new GameObject("HandStickAnchor").transform;
            if (surface) stickAnchor.SetParent(surface.transform, true); // ride a moving surface
            stickAnchor.SetPositionAndRotation(pos, rot);

            leashTarget = new GameObject("HandLeashTarget").transform;
            leashTarget.SetPositionAndRotation(pos, rot);

            // Reuse the hand's existing attach-tracking: MoveHandToTarget un-parents the hand and matches
            // it to leashTarget every frame (OnBeforeRender). We move leashTarget; ReturnHandToPlayer undoes it.
            handAnimator.MoveHandToTarget(leashTarget, 0f, false);
            sticking = true;
            strain   = 0f;
        }

        // Move the leash target between the surface anchor and where the controller would hold the hand,
        // weighted by leashWeight; break (snap back) when the controller pulls past breakDistance.
        private void UpdateLeash()
        {
            if (!leashTarget || !stickAnchor || !controllerMount) { ReleaseStick(true); return; }

            Vector3 ctrlPos = controllerMount.TransformPoint(handLocalPos);
            Quaternion ctrlRot = controllerMount.rotation * handLocalRot;
            Vector3 stickPos = stickAnchor.position;

            float dist = Vector3.Distance(ctrlPos, stickPos);
            strain = Mathf.Clamp01(dist / breakDistance);
            if (dist > breakDistance) { ReleaseStick(true); return; }

            leashTarget.SetPositionAndRotation(
                Vector3.Lerp(stickPos, ctrlPos, leashWeight),
                Quaternion.Slerp(stickAnchor.rotation, ctrlRot, leashWeight));
        }

        // Re-attach the hand to the controller (snap), tear down the anchor objects, and (on a distance
        // break only) fire the snap-back event. Idempotent — safe to call when not sticking.
        private void ReleaseStick(bool broke)
        {
            if (!sticking) return;
            sticking = false;
            strain   = 0f;

            if (handAnimator) handAnimator.ReturnHandToPlayer();
            if (stickAnchor) Destroy(stickAnchor.gameObject);
            if (leashTarget) Destroy(leashTarget.gameObject);
            stickAnchor     = null;
            leashTarget     = null;
            controllerMount = null;

            if (broke) onHandSnapBack?.Invoke();
        }

        // Nearest gathered world collider to the probe center (buffer is compacted: nulls only at the tail).
        private Collider NearestCollider()
        {
            Vector3 center = (probeCenter ? probeCenter : transform).position;
            Collider best = null;
            float min = float.PositiveInfinity;
            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (!col) break;
                Vector3 cp = SupportsClosestPoint(col) ? col.ClosestPoint(center) : col.ClosestPointOnBounds(center);
                float d = (cp - center).sqrMagnitude;
                if (d < min) { min = d; best = col; }
            }
            return best;
        }

        #endregion

        #region Gating / context

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

            if (Dyn() && dyn.HasRequiredPoses)
                return true;

            if (!warned)
            {
                Debug.LogWarning($"[HandGraspProbe] {name} — add a HandDynamicPoses component with a Closed " +
                                 "pose (and an Open/Default) to the hand to enable the grasp probe.", this);
                warned = true;
            }
            return false;
        }

        // Resolves the hand's HandDynamicPoses once (solver poses + fingertip probes). May be null.
        private HandDynamicPoses Dyn()
        {
            if (!dynResolved)
            {
                dyn = handAnimator ? handAnimator.GetComponent<HandDynamicPoses>() : null;
                dynResolved = true;
            }
            return dyn;
        }

        private void BuildContext()
        {
            ctx ??= new HandSolveContext();
            // Re-created when the toggle changes so flipping it during play-mode dial-in takes effect.
            bool wantProgressive = Solver.useProgressiveSolver;
            if (solver == null || solverIsProgressive != wantProgressive)
            {
                solver = wantProgressive ? (IHandPoseSolver)new ProgressiveCurlSolver() : new CurlSweepSolver();
                solverIsProgressive = wantProgressive;
            }

            ctx.hand             = handAnimator;
            ctx.fingerMap        = handAnimator.fingerMap;
            ctx.tipProbes        = dyn ? dyn.Tips : null;
            ctx.openPose         = dyn.OpenOrDefault;
            ctx.closedPose       = dyn.ClosedPose;
            ctx.closedPoses      = dyn.ClosedCandidates;
            ctx.relaxedPose      = dyn.RelaxedPose;
            ctx.targetColliders  = colliders;
            ctx.targetMask       = worldMask;
            ctx.collectDebug     = handAnimator.requestSolveDebug ||
                                   (HandPoserSettings.Instance && HandPoserSettings.Instance.drawSolveDebug);
            Solver.ApplyTo(ctx);

            // Candidate hysteresis is a continuous-resolve concern, so it lives on the probe (not the
            // shared solver block). Clear the solver's remembered choices on the first solve of a fresh
            // session (was idle last frame) so a new grasp doesn't inherit the previous one's bias.
            ctx.candidateStickiness   = candidateStickiness;
            ctx.resetCandidateHistory = !isPosing;
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

        private bool Idle(string _name, out PoseScriptableObject.JointData _idle)
        {
            if (idleByName == null)
            {
                idleByName = new Dictionary<string, PoseScriptableObject.JointData>();
                if (handAnimator.DefaultPose)
                    foreach (var jd in handAnimator.DefaultPose.joints) idleByName[jd.jointName] = jd;
            }
            return idleByName.TryGetValue(_name, out _idle);
        }

        private Dictionary<string, Transform> JointByName()
        {
            if (jointByName != null) return jointByName;
            jointByName = new Dictionary<string, Transform>();
            foreach (var joint in handAnimator.currentJoints)
                if (joint && !jointByName.ContainsKey(joint.name)) jointByName[joint.name] = joint;
            return jointByName;
        }

        // Collider.ClosestPoint only supports primitives and convex mesh colliders (else it logs an error
        // per call); the fade weight uses ClosestPointOnBounds for everything else.
        private static bool SupportsClosestPoint(Collider _col) =>
            _col is BoxCollider || _col is SphereCollider || _col is CapsuleCollider ||
            (_col is MeshCollider mc && mc.convex);

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = enableProbe ? Color.cyan : Color.gray;
            Vector3 center = (probeCenter ? probeCenter : transform).position;
            Gizmos.DrawWireSphere(center, reachRadius);

            // While anchored: the break sphere (green → red as strain rises) and the strain line to where
            // the controller is pulling the hand.
            if (sticking && stickAnchor)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, strain);
                Gizmos.DrawWireSphere(stickAnchor.position, breakDistance);
                if (controllerMount)
                    Gizmos.DrawLine(stickAnchor.position, controllerMount.TransformPoint(handLocalPos));
            }
        }
#endif
    }
}
