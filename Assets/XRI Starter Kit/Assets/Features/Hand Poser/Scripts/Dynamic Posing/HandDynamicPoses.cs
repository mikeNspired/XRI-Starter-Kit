using System;
using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Everything the DYNAMIC posing system needs for one hand, in a single opt-in component: the solver's
    /// open/closed reference poses, optional extra closed candidates, and the fingertip probes. Add this to a
    /// hand (next to <see cref="HandAnimator"/>) ONLY when you want procedural grabbing — without it,
    /// HandAnimator does authored posing alone and none of these fields clutter its inspector.
    ///
    /// Solver poses: the per-finger solve sweeps each finger from the Open pose (t=0) to the Closed fist
    /// (t=1) and stops where the finger contacts the object. Closed Candidates are extra closed shapes
    /// (e.g. a pinch) the solver also tries per finger, keeping whichever seats that finger closest.
    ///
    /// Fingertip probes: on skeletons whose last finger bone is the distal knuckle (no tip joint), the
    /// fingertip pad has no transform to contact-test. This owns one probe transform per finger, just past
    /// the last joint. Probe names are "&lt;finger&gt;_TipProbe_Ignore": the "Ignore" suffix keeps the whole
    /// pose system off them, and the "TipProbe" marker distinguishes them from the physical-presence
    /// feature's "*_DistalCollider_Ignore" capsules (also "*Ignore" children of the last joints). A probe is
    /// a bare transform — anything carrying a Collider is rejected.
    /// </summary>
    public class HandDynamicPoses : MonoBehaviour
    {
        [Tooltip("Hand these dynamic poses belong to. Auto-filled from this GameObject if empty.")]
        [SerializeField] private HandAnimator handAnimator;

        [Header("Solver Poses")]
        [Tooltip("Fully-open/splayed pose — the t=0 (open) end of the per-finger curl sweep. Kept distinct " +
                 "from the HandAnimator DefaultPose so the solver has full finger range. Falls back to " +
                 "DefaultPose when empty.")]
        [SerializeField] private PoseScriptableObject openPose;

        [Tooltip("Fist/grip pose — the t=1 (closed) end of the per-finger curl sweep. Required for dynamic " +
                 "posing; without it this hand can only use authored poses.")]
        [SerializeField] private PoseScriptableObject closedPose;

        [Tooltip("Optional extra closed shapes the solver also tries per finger (e.g. a pinch alongside the " +
                 "fist), keeping whichever seats that finger's tip closest to the object. The Closed Pose " +
                 "above is always tried first. Leave empty for fist-only — zero cost.")]
        [SerializeField] private List<PoseScriptableObject> closedCandidates = new List<PoseScriptableObject>();

        [Tooltip("Optional rest shape for a finger that touches NOTHING during a dynamic solve. When set, " +
                 "that finger takes this pose exactly (author a natural relaxed grip); when empty it curls " +
                 "toward the Closed pose by the No-Contact Curl amount, as before.")]
        [SerializeField] private PoseScriptableObject relaxedPose;

        [Header("Palm")]
        [Tooltip("Approximate palm-center point, used by object seating (a dynamic grab settles the object " +
                 "toward this before the solve). Leave empty to fall back to the centroid of the finger " +
                 "base joints — assign a transform on the palm surface for best results.")]
        [SerializeField] private Transform palmAnchor;

        [Header("Fingertip Probes")]
        [Tooltip("Per-finger probe transforms at the fingertip pad, just past each last joint. Leave empty " +
                 "to auto-generate at runtime, or click 'Create / Refresh Probes' below and nudge them onto " +
                 "the pads. Each must be a bare transform named *_Ignore (never a physics collider).")]
        public Transform thumbTip;
        public Transform indexTip;
        public Transform middleTip;
        public Transform ringTip;
        public Transform pinkyTip;

        [Tooltip("Draw magenta spheres at the fingertip probes while this hand is selected, so you can " +
                 "check their placement. Turn off once they're positioned — purely a setup aid.")]
        [SerializeField] private bool drawTipGizmos = true;

        /// Marker substring identifying a solver tip probe, so detection can never confuse one with the
        /// physical-presence distal colliders (also "*Ignore" children of the same joints).
        public const string TipProbeMarker = "TipProbe";

        // Resolved per-finger probes (thumb=0 … pinky=4); entries may be null (chain too short / no hand).
        private readonly Transform[] tips = new Transform[5];

        // ─── Solver-pose accessors (read by the grab path, HandGraspProbe, the tester) ───
        public PoseScriptableObject OpenPose  => openPose;
        public PoseScriptableObject ClosedPose => closedPose;
        public List<PoseScriptableObject> ClosedCandidates => closedCandidates;
        public PoseScriptableObject RelaxedPose => relaxedPose;

        /// World-space palm point for object seating: the authored anchor when assigned, else the
        /// centroid of the finger base joints (a fair palm approximation on most skeletons).
        public Vector3 PalmPoint
        {
            get
            {
                if (palmAnchor) return palmAnchor.position;
                Vector3 sum = Vector3.zero;
                int count = 0;
                for (int i = 0; i < 5; i++)
                {
                    var chain = Hand ? Hand.fingerMap.Finger(i) : null;
                    if (chain == null || chain.Count == 0 || !chain[0]) continue;
                    sum += chain[0].position;
                    count++;
                }
                return count > 0 ? sum / count : transform.position;
            }
        }

        /// Open pose for the sweep, falling back to the hand's DefaultPose when none is assigned.
        public PoseScriptableObject OpenOrDefault => openPose ? openPose : (Hand ? Hand.DefaultPose : null);

        /// True when this hand has the minimum poses the solver needs (a closed fist + an open/default).
        public bool HasRequiredPoses => closedPose && OpenOrDefault;

        /// Resolved probe for a finger (thumb=0 … pinky=4), or null. Read by the solve drivers.
        public Transform Tip(int i) => (i >= 0 && i < 5) ? tips[i] : null;

        /// The resolved per-finger array handed to <see cref="HandSolveContext.tipProbes"/>.
        public Transform[] Tips => tips;

        private HandAnimator Hand => handAnimator ? handAnimator : (handAnimator = GetComponent<HandAnimator>());

        private void Reset() => handAnimator = GetComponent<HandAnimator>();

        private void Awake()
        {
            if (!handAnimator) TryGetComponent(out handAnimator);
            Resolve(createMissing: true);
        }

        /// <summary>
        /// Fills the per-finger probe array: a valid assigned field, else an existing "TipProbe" child of
        /// the finger's last joint, else (when <paramref name="createMissing"/>) a freshly extrapolated
        /// probe. <paramref name="onCreated"/> lets the editor button register Undo for created objects.
        /// </summary>
        public void Resolve(bool createMissing, Action<GameObject> onCreated = null)
        {
            if (!Hand)
            {
                Debug.LogWarning($"[HandDynamicPoses] {name} — no HandAnimator found; probes disabled.", this);
                return;
            }

            // Awake order between this and HandAnimator is undefined; make sure chains exist.
            if (handAnimator.fingerMap.thumb.Count == 0 && handAnimator.RootBone)
                handAnimator.SetBones();

            thumbTip  = tips[0] = ResolveOne(thumbTip,  handAnimator.fingerMap.thumb,  "Thumb",  createMissing, onCreated);
            indexTip  = tips[1] = ResolveOne(indexTip,  handAnimator.fingerMap.index,  "Index",  createMissing, onCreated);
            middleTip = tips[2] = ResolveOne(middleTip, handAnimator.fingerMap.middle, "Middle", createMissing, onCreated);
            ringTip   = tips[3] = ResolveOne(ringTip,   handAnimator.fingerMap.ring,   "Ring",   createMissing, onCreated);
            pinkyTip  = tips[4] = ResolveOne(pinkyTip,  handAnimator.fingerMap.pinky,  "Pinky",  createMissing, onCreated);
        }

        private Transform ResolveOne(Transform assigned, List<Transform> chain, string fingerName,
                                     bool createMissing, Action<GameObject> onCreated)
        {
            // A real probe is a bare transform. Reject colliders outright — this is exactly how the
            // physical-presence distal capsules ended up mistaken for probes once before.
            if (assigned)
            {
                if (!assigned.GetComponent<Collider>()) return assigned;
                Debug.LogWarning($"[HandDynamicPoses] {name} — '{assigned.name}' assigned as the {fingerName} " +
                                 "tip probe has a Collider; that is a physics collider, not a probe. Ignoring it.", this);
            }

            if (chain == null || chain.Count == 0) return null;
            var lastJoint = chain[chain.Count - 1];

            // Reuse an existing probe child (matched by the TipProbe marker, never by the Ignore suffix
            // alone) so repeated calls are idempotent.
            for (int i = 0; i < lastJoint.childCount; i++)
            {
                var child = lastJoint.GetChild(i);
                if (child.name.Contains(TipProbeMarker) && !child.GetComponent<Collider>()) return child;
            }

            if (!createMissing) return null;

            var probe = CreateProbe(chain, fingerName);
            if (probe) onCreated?.Invoke(probe.gameObject);
            return probe;
        }

        // Creates "<finger>_TipProbe_Ignore" just past the last joint by extending the last bone segment
        // (0.8× its length). Needs ≥2 joints to define a direction.
        private static Transform CreateProbe(List<Transform> chain, string fingerName)
        {
            if (chain.Count < 2) return null;
            var last = chain[chain.Count - 1];
            var prev = chain[chain.Count - 2];
            Vector3 dir = last.position - prev.position;
            float len = dir.magnitude;
            if (len < 1e-5f) return null;

            var probe = new GameObject($"{fingerName}_{TipProbeMarker}_Ignore").transform;
            probe.SetParent(last, false);
            probe.position = last.position + dir / len * (len * 0.8f);
            probe.localRotation = Quaternion.identity;
            return probe;
        }

#if UNITY_EDITOR
        // Magenta spheres mark the solver-only fingertip probes (the contact points the curl solver
        // tests against), so you can verify they sit on the finger pads. Setup aid only — toggleable.
        private void OnDrawGizmosSelected()
        {
            if (!drawTipGizmos) return;
            Gizmos.color = Color.magenta;
            foreach (var tip in tips)
                if (tip) Gizmos.DrawWireSphere(tip.position, 0.005f);
        }
#endif
    }
}
