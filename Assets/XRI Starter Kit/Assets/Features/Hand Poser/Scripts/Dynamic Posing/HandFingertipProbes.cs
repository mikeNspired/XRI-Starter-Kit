using System;
using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Solver-only fingertip probes for the DYNAMIC posing system. On skeletons whose last finger bone is
    /// the distal knuckle (no tip joint), the actual fingertip pad has no transform to contact-test, so the
    /// dynamic solvers can't seat the tip on a surface. This component owns one probe transform per finger,
    /// placed just past the last joint.
    ///
    /// Deliberately separate from <see cref="HandAnimator"/>: authored poses never use these — they exist
    /// only for the dynamic grab/grasp solve. Put this on the hand (next to HandAnimator). Leave the fields
    /// empty to auto-generate probes at runtime, or use the inspector's "Create / Refresh Probes" button to
    /// create persistent ones you can nudge onto the fingertip pads and save into the prefab.
    ///
    /// Probe names are "&lt;finger&gt;_TipProbe_Ignore": the "Ignore" suffix keeps the entire pose system
    /// (SetBones, finger chains, pose saving) away from them, and the "TipProbe" marker distinguishes them
    /// from the physical-presence feature's "*_DistalCollider_Ignore" capsules, which also live under the
    /// last joints. A probe is a bare transform — anything with a Collider is rejected as a probe.
    /// </summary>
    public class HandFingertipProbes : MonoBehaviour
    {
        [Tooltip("Hand whose fingers these probes belong to. Auto-filled from this GameObject if empty.")]
        [SerializeField] private HandAnimator handAnimator;

        [Tooltip("Per-finger probe transform at the fingertip pad, just past the last joint. Leave empty to " +
                 "auto-generate at runtime, or use 'Create / Refresh Probes' and nudge into place. Must be a " +
                 "bare transform named *_Ignore (never a physics collider).")]
        public Transform thumbTip, indexTip, middleTip, ringTip, pinkyTip;

        /// Marker substring identifying a solver tip probe, so detection can never confuse one with the
        /// physical-presence distal colliders (also "*Ignore" children of the same joints).
        public const string TipProbeMarker = "TipProbe";

        // Resolved per-finger probes (thumb=0 … pinky=4); entries may be null (chain too short / no hand).
        private readonly Transform[] tips = new Transform[5];

        /// Resolved probe for a finger (thumb=0 … pinky=4), or null. Read by the solve drivers.
        public Transform Tip(int i) => (i >= 0 && i < 5) ? tips[i] : null;

        /// The resolved per-finger array handed to <see cref="HandSolveContext.tipProbes"/>.
        public Transform[] Tips => tips;

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
            if (!handAnimator && !TryGetComponent(out handAnimator))
            {
                Debug.LogWarning($"[HandFingertipProbes] {name} — no HandAnimator found; probes disabled.", this);
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
                Debug.LogWarning($"[HandFingertipProbes] {name} — '{assigned.name}' assigned as the {fingerName} " +
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
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            foreach (var tip in tips)
                if (tip) Gizmos.DrawWireSphere(tip.position, 0.005f);
        }
#endif
    }
}
