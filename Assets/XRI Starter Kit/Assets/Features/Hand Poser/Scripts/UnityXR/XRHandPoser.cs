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
        [SerializeField] private bool  graspRequireThumb  = true;
        [SerializeField, Range(0, 4)] private int graspRequiredFingers = 2;
        [SerializeField] private FailedGraspResponse failedGraspResponse = FailedGraspResponse.Drop;

        private IHandPoseSolver poseSolver;
        private Coroutine dynamicSolveRoutine;

        // ─── Effective settings: per-object override when enabled, else global defaults ───
        private HandPoserSettings Settings => HandPoserSettings.Instance;
        private float PositionThreshold     => overrideGlobalSettings ? positionThreshold     : Settings.dynamicPositionThreshold;
        private float RotationThreshold     => overrideGlobalSettings ? rotationThreshold     : Settings.dynamicRotationThreshold;
        private int   DynamicStepCount      => overrideGlobalSettings ? dynamicStepCount      : Settings.dynamicStepCount;
        private float DynamicProbeRadius    => overrideGlobalSettings ? dynamicProbeRadius    : Settings.dynamicProbeRadius;
        private int   SamplesPerFinger      => overrideGlobalSettings ? dynamicSamplesPerFinger : Settings.dynamicSamplesPerFinger;
        private bool  GraspRequireThumb     => overrideGlobalSettings ? graspRequireThumb     : Settings.graspRequireThumb;
        private int   GraspRequiredFingers  => overrideGlobalSettings ? graspRequiredFingers  : Settings.graspRequiredFingers;
        private FailedGraspResponse FailedGraspResponse => overrideGlobalSettings ? failedGraspResponse : Settings.failedGraspResponse;

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
                case HandPosePolicy.DynamicOnly:  return true;
                default:                          return IsGrabDynamic(hand);
            }
        }

        // Returns true when the grab should use the dynamic solver. Public so the interactor-side
        // snap suppression (HandReference) can reuse the same gate without duplicating thresholds.
        // logDecision gates the console output so HandReference's query doesn't double-log.
        public bool IsGrabDynamic(HandAnimator hand, bool logDecision = false)
        {
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

            // Actively stop any in-flight grip/trigger animation and hold the open (sweep-start)
            // pose during the ease-in/solve wait. Gating isGrabbingObject alone can't stop a grip
            // clench that already started this frame (StartAnimationByButtonValue's guard only
            // blocks new starts). SetJointsDirect cancels the running coroutine, so the hand reads
            // as a natural open → curl instead of clench → open → pose.
            var openPose = hand.OpenPose ? hand.OpenPose : hand.DefaultPose;
            if (openPose && openPose.joints != null)
                hand.SetJointsDirect(openPose.joints, hand.animationTimeToNewPose);

            if (dynamicSolveRoutine != null) StopCoroutine(dynamicSolveRoutine);
            dynamicSolveRoutine = StartCoroutine(SolveDynamicPoseRoutine(hand, interactor));
        }

        private IEnumerator SolveDynamicPoseRoutine(HandAnimator hand, IXRSelectInteractor interactor)
        {
            // Wait for the object's attach ease-in so the hand is at the grab location
            // before we probe — otherwise fingers solve against thin air.
            float ease = GetEaseInTime();
            if (ease > 0f) yield return new WaitForSeconds(ease);
            yield return null;  // one extra frame for the grab to fully settle

            if (!hand || !hand.ClosedPose || !hand.DefaultPose)
            {
                Debug.LogWarning($"[XRHandPoser] {gameObject.name} — dynamic solve skipped: hand, ClosedPose, or DefaultPose is not assigned.");
                yield break;
            }

            var colliders = interactable.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0)
                Debug.LogWarning($"[XRHandPoser] {gameObject.name} — dynamic solve: no colliders on interactable; fingers will fully close.");

            int mask = 0;
            foreach (var c in colliders) mask |= 1 << c.gameObject.layer;

            // Sweep from the dedicated max-open pose so fingers have full range and start clear
            // of the target. Falls back to the relaxed DefaultPose on hands without an OpenPose
            // authored yet, preserving prior behavior.
            var openPose = hand.OpenPose ? hand.OpenPose : hand.DefaultPose;

            var ctx = new HandSolveContext
            {
                hand            = hand,
                fingerMap       = hand.fingerMap,
                openPose        = openPose,
                closedPose      = hand.ClosedPose,
                closedPoses     = hand.ClosedPoses,
                targetColliders = colliders,
                targetMask      = mask,
                stepCount       = DynamicStepCount,
                probeRadius     = DynamicProbeRadius,
                samplesPerFinger = SamplesPerFinger,
            };

            poseSolver ??= new CurlSweepSolver();
            var result = poseSolver.Solve(ctx);

            if (result == null || result.Length == 0)
            {
                Debug.LogWarning($"[XRHandPoser] {gameObject.name} — dynamic solve returned no joint data; treating as failed grasp.");
                HandleFailedGrasp(hand, interactor);
                yield break;
            }

            // Graspability gate: a sphere-cast grab can catch an object the hand only floated past.
            // Require the configured contact before committing, otherwise the object would hang in
            // mid-air. The solver records per-finger contact (thumb=0 … pinky=4).
            if (!IsGraspValid(poseSolver.LastSolveContacted))
            {
                Debug.Log($"[XRHandPoser] {gameObject.name} — dynamic grasp failed contact test ({DescribeContacts(poseSolver.LastSolveContacted)}); {FailedGraspResponse}.");
                HandleFailedGrasp(hand, interactor);
                yield break;
            }

            // Grasp holds: apply the solved pose. (Animation gating was set in BeginDynamicPose so
            // the hand doesn't fist up during the solve wait.)
            hand.SetJointsDirect(result, hand.animationTimeToNewPose);
        }

        // thumb = index 0; fingers 1..4 are index/middle/ring/pinky.
        private bool IsGraspValid(bool[] contacted)
        {
            if (contacted == null || contacted.Length < 5) return false;

            if (GraspRequireThumb && !contacted[0]) return false;

            int fingers = 0;
            for (int i = 1; i < 5; i++)
                if (contacted[i]) fingers++;

            return fingers >= GraspRequiredFingers;
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

            // Drop: force-release so the object can't hang in the air. The select-exit chain
            // (TryReleaseHand + HandReference.ResetAttachTransform) returns the hand and attach.
            if (interactor != null && interactable && interactable.interactionManager)
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
