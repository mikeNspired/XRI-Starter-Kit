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

        [Header("Dynamic Pose Gate")]
        [SerializeField] private float positionThreshold = 0.05f;
        [SerializeField] private float rotationThreshold = 30f;

        [Header("Dynamic Pose Solver")]
        [SerializeField] private int   dynamicStepCount   = 15;
        [SerializeField] private float dynamicProbeRadius = 0.01f;

        private IHandPoseSolver poseSolver;
        private Coroutine dynamicSolveRoutine;

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

            if (GrabIsDynamic(handRef.Hand))
                BeginDynamicPose(handRef.Hand);
            else
                BeginNewHandPoses(handRef.Hand);
        }

        // Returns true when the grab should use the dynamic solver; logs the decision either way.
        private bool GrabIsDynamic(HandAnimator hand)
        {
            if (!CheckIfPoseExistForHand(hand))
            {
                Debug.Log($"[XRHandPoser] {gameObject.name} | DYNAMIC — no authored pose for {hand.handType} hand");
                return true;
            }

            var authoredAttach = hand.handType == LeftRight.Left ? leftHandAttach : rightHandAttach;
            if (!authoredAttach)
            {
                Debug.Log($"[XRHandPoser] {gameObject.name} | AUTHORED — no attach transform found, treating as on-axis");
                return false;
            }

            // Measure from the HAND ROOT, not the controller: SaveAttachPoints defines the
            // authored attach as hand.transform's pose, so hand-vs-attach cancels the
            // hand-model grip offset and a clean on-axis grab reads ~0. Comparing the
            // controller instead bakes in that offset and trips DYNAMIC on every grab.
            float offsetPos   = Vector3.Distance(hand.transform.position, authoredAttach.position);
            float offsetAngle = Quaternion.Angle(hand.transform.rotation, authoredAttach.rotation);

            bool isDynamic = offsetPos > positionThreshold || offsetAngle > rotationThreshold;

            if (isDynamic)
                Debug.Log($"[XRHandPoser] {gameObject.name} | DYNAMIC — pos={offsetPos:F3}m (threshold {positionThreshold}m), angle={offsetAngle:F1}° (threshold {rotationThreshold}°)");
            else
                Debug.Log($"[XRHandPoser] {gameObject.name} | AUTHORED — pos={offsetPos:F3}m, angle={offsetAngle:F1}°");

            return isDynamic;
        }

        // ─── Dynamic pose path ────────────────────────────────────────────────────

        private void BeginDynamicPose(HandAnimator hand)
        {
            RegisterGrabbingHand(hand);   // so Release() can return the hand on un-grab
            hand.isGrabbingObject = true; // gate the grip-hold animation so it can't overwrite the solved pose (authored path does this via BeginNewPoses)
            if (dynamicSolveRoutine != null) StopCoroutine(dynamicSolveRoutine);
            dynamicSolveRoutine = StartCoroutine(SolveDynamicPoseRoutine(hand));
        }

        private IEnumerator SolveDynamicPoseRoutine(HandAnimator hand)
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

            var ctx = new HandSolveContext
            {
                hand            = hand,
                fingerMap       = hand.fingerMap,
                openPose        = hand.DefaultPose,
                closedPose      = hand.ClosedPose,
                targetColliders = colliders,
                targetMask      = mask,
                stepCount       = dynamicStepCount,
                probeRadius     = dynamicProbeRadius,
            };

            poseSolver ??= new CurlSweepSolver();
            var result = poseSolver.Solve(ctx);

            if (result != null && result.Length > 0)
                hand.SetJointsDirect(result, hand.animationTimeToNewPose);
            else
                Debug.LogWarning($"[XRHandPoser] {gameObject.name} — dynamic solve returned no joint data.");
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
