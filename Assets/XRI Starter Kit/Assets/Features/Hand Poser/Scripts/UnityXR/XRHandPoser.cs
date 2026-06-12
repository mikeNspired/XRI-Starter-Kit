// Author MikeNspired.

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// The main script to setup the hand for animations.
    /// Its main purpose is to quickly setup hand poses for each item, and then assign those poses to the hand when the item is grabbed.
    /// This script is driven by the XRGrabInteractable to be used with UnityXR. It uses the onSelectEnter and onSelectExit to work.
    /// </summary>
    public class XRHandPoser : HandPoser
    {
        public XRBaseInteractable interactable;
        public bool MaintainHandOnObject = true;
        public bool WaitTillEaseInTimeToMaintainPosition = true;
        public bool overrideEaseTime = false;
        public float easeInTimeOverride = 0;

        [Header("Hand Pose Policy")]
        [Tooltip("Auto: authored near the grip, dynamic fallback off-axis. AuthoredOnly: never solve " +
                 "(e.g. a handgun). DynamicOnly: always solve (e.g. a cube). NoPosing: no hand posing.")]
        [SerializeField] private HandPosePolicy posePolicy = HandPosePolicy.Auto;

        [Tooltip("When off, this object uses the global defaults on the HandPoserSettings asset. " +
                 "Turn on to override the thresholds and grasp rule below for this object only.")]
        [SerializeField] private bool overrideGlobalSettings = false;

        [Header("Per-Object Overrides (used only when Override Global Settings is on)")]
        [SerializeField] private float positionThreshold = 0.08f;
        [SerializeField] private float rotationThreshold = 45f;
        [SerializeField] private int   dynamicStepCount   = 15;
        [SerializeField] private float dynamicProbeRadius = 0.02f;
        [SerializeField, Range(1, 4)] private int dynamicSamplesPerFinger = 2;
        [SerializeField, Range(0, 1)] private float dynamicNoContactCurl = 0.7f;
        [SerializeField, Range(0, 1)] private float dynamicDistalFollowCurl = 0.33f;
        [SerializeField, Range(1, 5)] private int dynamicSamplesPerSegment = 2;
        [SerializeField] private PerFingerSolveSettings fingerSettings = new PerFingerSolveSettings();
        [SerializeField] private bool  graspRequireThumb  = true;
        [SerializeField, Range(0, 4)] private int graspRequiredFingers = 2;
        [SerializeField] private FailedGraspResponse failedGraspResponse = FailedGraspResponse.Drop;
        [SerializeField] private bool  seatInPalm = false;
        [SerializeField] private float seatMaxDistance = 0.03f;
        [SerializeField] private float seatClearance = 0.01f;

        private IHandPoseSolver poseSolver;
        private Coroutine dynamicSolveRoutine;

        // ─── Effective settings: per-object override when enabled, else global defaults ───
        private HandPoserSettings Settings => HandPoserSettings.Instance;
        private float PositionThreshold     => overrideGlobalSettings ? positionThreshold     : Settings.dynamicPositionThreshold;
        private float RotationThreshold     => overrideGlobalSettings ? rotationThreshold     : Settings.dynamicRotationThreshold;
        private int   DynamicStepCount      => overrideGlobalSettings ? dynamicStepCount      : Settings.dynamicStepCount;
        private float DynamicProbeRadius    => overrideGlobalSettings ? dynamicProbeRadius    : Settings.dynamicProbeRadius;
        private int   SamplesPerFinger      => overrideGlobalSettings ? dynamicSamplesPerFinger : Settings.dynamicSamplesPerFinger;
        private float NoContactCurl         => overrideGlobalSettings ? dynamicNoContactCurl   : Settings.dynamicNoContactCurl;
        private float DistalFollowCurl      => overrideGlobalSettings ? dynamicDistalFollowCurl : Settings.dynamicDistalFollowCurl;
        private int   SamplesPerSegment     => overrideGlobalSettings ? dynamicSamplesPerSegment : Settings.dynamicSamplesPerSegment;
        private PerFingerSolveSettings FingerSettings => overrideGlobalSettings ? fingerSettings : Settings.dynamicFingerSettings;
        private bool  GraspRequireThumb     => overrideGlobalSettings ? graspRequireThumb     : Settings.graspRequireThumb;
        private int   GraspRequiredFingers  => overrideGlobalSettings ? graspRequiredFingers  : Settings.graspRequiredFingers;
        private FailedGraspResponse FailedGraspResponse => overrideGlobalSettings ? failedGraspResponse : Settings.failedGraspResponse;
        private bool  SeatInPalm            => overrideGlobalSettings ? seatInPalm      : Settings.dynamicSeatInPalm;
        private float SeatMaxDistance       => overrideGlobalSettings ? seatMaxDistance : Settings.dynamicSeatMaxDistance;
        private float SeatClearance         => overrideGlobalSettings ? seatClearance   : Settings.dynamicSeatClearance;

        protected override void Awake()
        {
            base.Awake();
            OnValidate();
            SubscribeToSelection();
        }

        private void SubscribeToSelection()
        {
            //Set hand animation on grab
            interactable.selectEntered.AddListener(TryStartPosing);

            //Set to default animations when item is released
            interactable.selectExited.AddListener(TryReleaseHand);
        }

        private void TryStartPosing(SelectEnterEventArgs x)
        {
            var handRef = x.interactorObject.transform.GetComponentInParent<HandReference>();
            if (!handRef) return;

            if (handRef.NearFarInteractor != null && handRef.NearFarInteractor.interactionAttachController.hasOffset)
            {
                Debug.Log("Hand poser skipped also");
                return; // Skip hand posing for far interactions
            }

            switch (posePolicy)
            {
                case HandPosePolicy.NoPosing:
                    return;
                case HandPosePolicy.AuthoredOnly:
                    BeginNewHandPoses(handRef.Hand);
                    return;
                case HandPosePolicy.DynamicOnly:
                    BeginDynamicPose(handRef.Hand, x.interactorObject);
                    return;
                default: // Auto
                    if (IsGrabDynamic(handRef.Hand, logDecision: true))
                        BeginDynamicPose(handRef.Hand, x.interactorObject);
                    else
                        BeginNewHandPoses(handRef.Hand);
                    return;
            }
        }

        // Effective dynamic-vs-authored decision including the per-object policy. HandReference
        // calls this so its snap suppression matches exactly what TryStartPosing will do.
        public bool ShouldUseDynamic(HandAnimator hand)
        {
            switch (posePolicy)
            {
                case HandPosePolicy.NoPosing:     return false;
                case HandPosePolicy.AuthoredOnly: return false;
                case HandPosePolicy.DynamicOnly:  return HasDynamicConfig(hand);
                default:                          return IsGrabDynamic(hand);
            }
        }

        // A hand can only pose dynamically if it carries a HandDynamicPoses component with the solver's
        // open/closed reference poses. Without it, every grab falls back to the authored path.
        private static bool HasDynamicConfig(HandAnimator hand)
        {
            if (!hand) return false;
            var dyn = hand.GetComponent<HandDynamicPoses>();
            return dyn && dyn.HasRequiredPoses;
        }

        // Returns true when the grab should use the dynamic solver. Public so the interactor-side
        // snap suppression (HandReference) can reuse the same gate without duplicating thresholds.
        // logDecision gates the console output so HandReference's query doesn't double-log.
        public bool IsGrabDynamic(HandAnimator hand, bool logDecision = false)
        {
            // No dynamic-pose component → never dynamic, regardless of attach offset.
            if (!HasDynamicConfig(hand))
            {
                if (logDecision)
                    Debug.Log($"[XRHandPoser] {gameObject.name} | AUTHORED — no HandDynamicPoses config on {hand.handType} hand");
                return false;
            }

            if (!CheckIfPoseExistForHand(hand))
            {
                if (logDecision)
                    Debug.Log($"[XRHandPoser] {gameObject.name} | DYNAMIC — no authored pose for {hand.handType} hand");
                return true;
            }

            var authoredAttach = hand.handType == LeftRight.Left ? leftHandAttach : rightHandAttach;
            if (!authoredAttach)
            {
                if (logDecision)
                    Debug.Log($"[XRHandPoser] {gameObject.name} | AUTHORED — no attach transform found, treating as on-axis");
                return false;
            }

            // Measure from the HAND ROOT, not the controller: SaveAttachPoints defines the
            // authored attach as hand.transform's pose, so hand-vs-attach cancels the
            // hand-model grip offset and a clean on-axis grab reads ~0. Comparing the
            // controller instead bakes in that offset and trips DYNAMIC on every grab.
            float offsetPos   = Vector3.Distance(hand.transform.position, authoredAttach.position);
            float offsetAngle = Quaternion.Angle(hand.transform.rotation, authoredAttach.rotation);

            bool isDynamic = offsetPos > PositionThreshold || offsetAngle > RotationThreshold;

            if (logDecision)
            {
                if (isDynamic)
                    Debug.Log($"[XRHandPoser] {gameObject.name} | DYNAMIC — pos={offsetPos:F3}m (threshold {PositionThreshold}m), angle={offsetAngle:F1}° (threshold {RotationThreshold}°)");
                else
                    Debug.Log($"[XRHandPoser] {gameObject.name} | AUTHORED — pos={offsetPos:F3}m, angle={offsetAngle:F1}°");
            }

            return isDynamic;
        }

        // ─── Dynamic pose path ────────────────────────────────────────────────────

        private void BeginDynamicPose(HandAnimator hand, IXRSelectInteractor interactor)
        {
            RegisterGrabbingHand(hand);    // so Release() can return the hand on un-grab
            hand.isGrabbingObject = true;  // gate the grip-hold animation from (re)starting
            hand.AnimationPose = null;     // gate the trigger animation; ReturnAnimationsToOriginal restores it on release
            hand.StopButtonValueAnimation(); // stop the grip squeeze in place so it can't keep closing the hand

            // Optional seating BEFORE the solve, so the fingers wrap the settled position.
            if (SeatInPalm) SeatObjectInPalm(hand, interactor);

            // Solve and apply on THIS frame, exactly like the authored path. Phase 5 holds the object
            // where it was grabbed (it does not ease toward the hand), so the geometry is already in
            // place — there is nothing to wait for. Applying synchronously means one blend from the
            // current grab shape straight to the solved pose; waiting a frame first let the grip
            // squeeze close the hand to a fist before the pose landed (the "closes then poses" glitch).
            SolveAndApplyDynamicPose(hand, interactor);
        }

        // Kinematic object seating: settle the grabbed object a small, hard-capped distance toward the
        // hand's palm point, closing the air gap so a dynamic grab reads as held rather than hovering.
        // HandReference (interactor selectEntered, which XRI fires before this interactable event) has
        // already aligned the interactor attach to the object's current pose, so moving the object AND
        // that attach target by the same delta keeps XRI's grab target equal to the object's pose — a
        // settle, never a snap-back toward the authored grip.
        private void SeatObjectInPalm(HandAnimator hand, IXRSelectInteractor interactor)
        {
            var dyn = hand ? hand.GetComponent<HandDynamicPoses>() : null;
            if (!dyn) return;

            var colliders = GatherSolidColliders();
            if (colliders.Length == 0) return;

            Vector3 palm = dyn.PalmPoint;
            float gap = float.PositiveInfinity;
            Vector3 nearest = palm;
            foreach (var col in colliders)
            {
                // ClosestPoint only supports primitives + convex meshes; bounds is a fine fallback
                // for a settle distance on arbitrary environment-style meshes.
                Vector3 cp = SupportsClosestPoint(col) ? col.ClosestPoint(palm) : col.ClosestPointOnBounds(palm);
                float d = Vector3.Distance(palm, cp);
                if (d < gap) { gap = d; nearest = cp; }
            }
            // gap 0 = palm already inside the object; nothing sensible to settle.
            if (float.IsPositiveInfinity(gap) || gap <= 1e-5f) return;

            float move = Mathf.Min(SeatMaxDistance, gap - SeatClearance);
            if (move <= 0f) return;

            // Move the nearest surface point toward the palm: the shortest way to close the gap.
            Vector3 delta = (palm - nearest) / gap * move;

            interactable.transform.position += delta;
            if (interactable.TryGetComponent(out Rigidbody body)) body.position = interactable.transform.position;

            var attach = interactor?.GetAttachTransform(interactable);
            if (attach) attach.position += delta;

            // The solve sphere-tests this frame; make the physics world see the moved colliders now.
            Physics.SyncTransforms();
        }

        private static bool SupportsClosestPoint(Collider col) =>
            col is BoxCollider || col is SphereCollider || col is CapsuleCollider ||
            (col is MeshCollider mc && mc.convex);

        // Solid colliders only: trigger volumes (hover zones, sound triggers) can never stop a finger
        // (the sweep queries ignore triggers) but they WOULD pollute the fingertip-gap candidate
        // selection and the layer mask. Disabled colliders likewise aren't in the physics world.
        private Collider[] GatherSolidColliders() =>
            System.Array.FindAll(
                interactable.GetComponentsInChildren<Collider>(),
                c => c.enabled && !c.isTrigger);

        private void SolveAndApplyDynamicPose(HandAnimator hand, IXRSelectInteractor interactor)
        {
            var dyn = hand ? hand.GetComponent<HandDynamicPoses>() : null;
            if (!hand || !dyn || !dyn.HasRequiredPoses)
            {
                Debug.LogWarning($"[XRHandPoser] {gameObject.name} — dynamic solve skipped: the hand needs a " +
                                 "HandDynamicPoses component with a Closed pose (and an Open/Default).");
                return;
            }

            var colliders = GatherSolidColliders();
            if (colliders.Length == 0)
                Debug.LogWarning($"[XRHandPoser] {gameObject.name} — dynamic solve: no solid colliders on interactable; fingers will fully close.");

            int mask = 0;
            foreach (var c in colliders) mask |= 1 << c.gameObject.layer;

            var ctx = new HandSolveContext
            {
                hand            = hand,
                fingerMap       = hand.fingerMap,
                tipProbes       = dyn.Tips,
                openPose        = dyn.OpenOrDefault,
                closedPose      = dyn.ClosedPose,
                closedPoses     = dyn.ClosedCandidates,
                relaxedPose     = dyn.RelaxedPose,
                jointLimits     = dyn.JointLimits,
                targetColliders = colliders,
                targetMask      = mask,
                stepCount       = DynamicStepCount,
                probeRadius     = DynamicProbeRadius,
                samplesPerFinger = SamplesPerFinger,
                samplesPerSegment = SamplesPerSegment,
                fingerSettings   = FingerSettings,
                noContactCurl    = NoContactCurl,
                distalFollowCurl = DistalFollowCurl,
                collectDebug     = Settings.drawSolveDebug || hand.requestSolveDebug,
            };

            // Re-created when the settings toggle changes so flipping useProgressiveSolver during
            // play-mode dial-in takes effect on the next grab (??= alone would pin the first choice).
            bool wantProgressive = Settings.useProgressiveSolver;
            if (poseSolver == null || (poseSolver is ProgressiveCurlSolver) != wantProgressive)
                poseSolver = wantProgressive ? (IHandPoseSolver)new ProgressiveCurlSolver() : new CurlSweepSolver();
            var result = poseSolver.Solve(ctx);

            // Publish solve telemetry to the hand so HandPoseSolveDebugDrawer can visualize this grab.
            hand.LastSolveDebug = poseSolver.LastSolveDebug;

            if (result == null || result.Length == 0)
            {
                Debug.LogWarning($"[XRHandPoser] {gameObject.name} — dynamic solve returned no joint data; treating as failed grasp.");
                HandleFailedGrasp(hand, interactor);
                return;
            }

            // Graspability gate: a sphere-cast grab can catch an object the hand only floated past.
            // Require the configured contact before committing, otherwise the object would hang in
            // mid-air. The solver records per-finger contact (thumb=0 … pinky=4).
            if (!IsGraspValid(poseSolver.LastSolveContacted))
            {
                Debug.Log($"[XRHandPoser] {gameObject.name} — dynamic grasp failed contact test ({DescribeContacts(poseSolver.LastSolveContacted)}); {FailedGraspResponse}.");
                HandleFailedGrasp(hand, interactor);
                return;
            }

            // Grasp holds: apply the solved pose — a single blend from the current grab shape.
            // Fingers disabled in the per-finger calibration take the authored pose when one exists.
            hand.SetJointsDirect(AppendAuthoredJointsForDisabledFingers(hand, result), hand.animationTimeToNewPose);
        }

        // A finger disabled in the per-finger calibration emits no solved joints. When this object has
        // an authored pose for the hand, pose that finger from it so "disabled" reads as "stay authored
        // while the rest solve". With no authored pose the finger simply keeps its current shape.
        private PoseScriptableObject.JointData[] AppendAuthoredJointsForDisabledFingers(
            HandAnimator hand, PoseScriptableObject.JointData[] result)
        {
            var settings = FingerSettings;
            var authored = hand.handType == LeftRight.Left ? leftHandPose : rightHandPose;
            if (!authored) return result;

            System.Collections.Generic.List<PoseScriptableObject.JointData> extra = null;
            for (int f = 0; f < 5; f++)
            {
                if (PerFingerSolveSettings.Get(settings, f).solve) continue;
                var chain = hand.fingerMap.Finger(f);
                if (chain == null) continue;
                foreach (var joint in chain)
                {
                    if (!joint) continue;
                    foreach (var jd in authored.joints)
                    {
                        if (jd.jointName != joint.name) continue;
                        extra ??= new System.Collections.Generic.List<PoseScriptableObject.JointData>();
                        extra.Add(jd);
                        break;
                    }
                }
            }
            if (extra == null) return result;

            // Solved joints first: SetJointsDirect matches by first name hit, so on a bone shared
            // between an enabled and a disabled finger the solve wins.
            var merged = new PoseScriptableObject.JointData[result.Length + extra.Count];
            result.CopyTo(merged, 0);
            extra.CopyTo(merged, result.Length);
            return merged;
        }

        // thumb = index 0; fingers 1..4 are index/middle/ring/pinky. Fingers disabled in the
        // per-finger calibration can never contact, so they are exempt from the requirement and
        // the required count caps at the number of enabled fingers.
        private bool IsGraspValid(bool[] contacted)
        {
            if (contacted == null || contacted.Length < 5) return false;
            var settings = FingerSettings;

            if (GraspRequireThumb && PerFingerSolveSettings.Get(settings, 0).solve && !contacted[0])
                return false;

            int fingers = 0, enabledFingers = 0;
            for (int i = 1; i < 5; i++)
            {
                if (!PerFingerSolveSettings.Get(settings, i).solve) continue;
                enabledFingers++;
                if (contacted[i]) fingers++;
            }

            return fingers >= Mathf.Min(GraspRequiredFingers, enabledFingers);
        }

        private static string DescribeContacts(bool[] contacted)
        {
            if (contacted == null) return "no data";
            return $"thumb={contacted[0]}, fingers={(contacted[1] ? 1 : 0) + (contacted[2] ? 1 : 0) + (contacted[3] ? 1 : 0) + (contacted[4] ? 1 : 0)}";
        }

        private void HandleFailedGrasp(HandAnimator hand, IXRSelectInteractor interactor)
        {
            // FallbackToAuthored: if an authored pose exists, apply it. The object is already held
            // in place (HandReference suppressed its snap), so BeginNewHandPoses moves the hand onto
            // the authored grip and poses it — no object jump. With no authored pose, fall through to drop.
            if (FailedGraspResponse == FailedGraspResponse.FallbackToAuthored && CheckIfPoseExistForHand(hand))
            {
                BeginNewHandPoses(hand);
                return;
            }

            // Drop: force-release so the object can't hang in the air. Deferred one frame — we are
            // inside the selectEntered callback and XRI does not allow a re-entrant SelectExit. The
            // select-exit chain (TryReleaseHand + HandReference.ResetAttachTransform) returns the
            // hand and attach.
            if (interactor != null && interactable && interactable.interactionManager)
                dynamicSolveRoutine = StartCoroutine(DropNextFrame(interactor));
        }

        private IEnumerator DropNextFrame(IXRSelectInteractor interactor)
        {
            yield return null;
            if (interactor != null && interactable && interactable.interactionManager && interactable.isSelected)
                interactable.interactionManager.SelectExit(interactor, (IXRSelectInteractable)interactable);
        }

        // ─── Shared / authored path (unchanged) ──────────────────────────────────

        private void TryReleaseHand(SelectExitEventArgs x)
        {
            if (!x.interactorObject.transform.GetComponentInParent<HandReference>()) return;

            // Cancel any pending dynamic solve so a stale solve can't land on the hand
            // after release (or during the next grab).
            if (dynamicSolveRoutine != null)
            {
                StopCoroutine(dynamicSolveRoutine);
                dynamicSolveRoutine = null;
            }

            Release();
        }

        private void MoveHandToPoseTransforms(HandAnimator hand)
        {
            //Determines if the left or right hand is grabbed, and then sends over the proper attachment point to be assigned to the XRGrabInteractable.
            var attachPoint = hand.handType == LeftRight.Left ? leftHandAttach : rightHandAttach;
            hand.MoveHandToTarget(attachPoint, GetEaseInTime(), WaitTillEaseInTimeToMaintainPosition);
        }

        protected override void BeginNewHandPoses(HandAnimator hand)
        {
            if (!hand || !CheckIfPoseExistForHand(hand)) return;

            base.BeginNewHandPoses(hand);

            if (MaintainHandOnObject) MoveHandToPoseTransforms(hand);
        }

        private bool CheckIfPoseExistForHand(HandAnimator hand)
        {
            if (leftHandPose && hand.handType == LeftRight.Left)
                return true;
            if (rightHandPose && hand.handType == LeftRight.Right)
                return true;
            return false;
        }

        private float GetEaseInTime()
        {
            float time = 0;
            interactable.TryGetComponent(out XRGrabInteractable xrGrabInteractable);
            if (xrGrabInteractable)
                time = xrGrabInteractable.attachEaseInTime;
            if (overrideEaseTime)
                time = easeInTimeOverride;

            return time;
        }

        private void OnValidate()
        {
            if (!interactable)
                interactable = GetComponent<XRBaseInteractable>();
            if (!interactable)
                interactable = GetComponentInParent<XRBaseInteractable>();
            if (!interactable)
                Debug.LogWarning(gameObject + " XRGrabPoser does not have an XRGrabInteractable assigned." + "  (Parent name) " + transform.parent);
        }
    }
}
