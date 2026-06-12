using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Draws the dynamic solver's per-finger telemetry as Scene-view gizmos so you can tell, at a glance,
    /// why each finger ended up straight or curled. Put this on the hand (alongside <see cref="HandAnimator"/>).
    ///
    /// Telemetry is produced while this component's 'draw' toggle is on (it sets the hand's
    /// requestSolveDebug flag each frame) or while the global "Draw Solve Debug" switch on the
    /// HandPoserSettings asset is on; with both off, nothing is recorded or drawn and there is no cost.
    /// Works for both real grabs (XRHandPoser) and the grasp probe — whoever solves publishes the data
    /// to <see cref="HandAnimator.LastSolveDebug"/>.
    ///
    /// Per finger it draws a sphere at every sampled joint, coloured PER JOINT so you can see which
    /// segment actually stopped the finger: contacted joints in the Contacted colour, non-contacted
    /// joints on a gripping finger in the muted Idle Joint colour, whole-finger no-contact / relaxed-rest
    /// in their own colours. Every colour (including its transparency), the labels, the normals, and the
    /// spheres can be toggled and tuned below so the overlay never has to bury the hand.
    /// </summary>
    public class HandPoseSolveDebugDrawer : MonoBehaviour
    {
        [Tooltip("The hand whose last solve to visualize. Auto-filled from this GameObject if left empty.")]
        [SerializeField] private HandAnimator handAnimator;

        [Tooltip("Master switch for this hand's solve debug. When on, the next dynamic solve (a grab or a " +
                 "free-hand touch) records telemetry and it is drawn here. Nothing draws until the hand " +
                 "actually solves against something — an open hand touching nothing has nothing to show.")]
        [SerializeField] private bool draw = true;

        [Header("What To Draw")]
        [Tooltip("Wire sphere at every sampled joint, at its locked curl position.")]
        [SerializeField] private bool drawSpheres = true;

        [Tooltip("Surface-normal line at each contacted joint's nearest surface point.")]
        [SerializeField] private bool drawContactNormals = true;

        [Tooltip("Per-finger text label: state, chosen closed pose, and the per-joint locked t values.")]
        [SerializeField] private bool drawLabels = true;

        [Tooltip("Only draw spheres/normals for joints that actually CONTACTED — hides the idle clutter " +
                 "so the gripping segments stand out.")]
        [SerializeField] private bool contactedJointsOnly = false;

        [Header("Colors (alpha = see-through)")]
        [Tooltip("Joint whose segment stopped on the target.")]
        [SerializeField] private Color contactedColor = new Color(0f, 1f, 0f, 0.55f);

        [Tooltip("Non-contacted joint on a finger that gripped elsewhere — kept faint so contacts pop.")]
        [SerializeField] private Color idleJointColor = new Color(0.65f, 0.65f, 0.65f, 0.3f);

        [Tooltip("Every joint of a finger that found nothing and fully closed (curl-sweep solver).")]
        [SerializeField] private Color noContactColor = new Color(1f, 0f, 0f, 0.55f);

        [Tooltip("Every joint of a finger that found nothing and settled into the relaxed rest.")]
        [SerializeField] private Color relaxedColor = new Color(1f, 0.92f, 0.016f, 0.55f);

        [Tooltip("Contact-normal lines.")]
        [SerializeField] private Color normalColor = new Color(0f, 1f, 1f, 0.8f);

        [Header("Labels")]
        [Tooltip("Tint each finger's label by its state colour. Off = the single Label Color below " +
                 "(easier to read against a busy scene).")]
        [SerializeField] private bool tintLabelsByState = true;

        [Tooltip("Label colour when Tint Labels By State is off.")]
        [SerializeField] private Color labelColor = Color.white;

        [Tooltip("World-space lift (m) of each label above its finger, to clear the geometry.")]
        [SerializeField] private float labelOffset = 0.01f;

        [Header("Sizes")]
        [Tooltip("Length (m) of the contact-normal lines.")]
        [SerializeField] private float normalLength = 0.02f;

        [Tooltip("Fallback sphere radius (m) used if a finger reports no probe radius.")]
        [SerializeField] private float fallbackRadius = 0.008f;

        [Tooltip("Multiplier on the reported probe radius — shrink the spheres without changing the solve.")]
        [SerializeField, Range(0.1f, 2f)] private float sphereScale = 1f;

        private void Reset() => handAnimator = GetComponent<HandAnimator>();

        // Ask the solver to record telemetry while this drawer wants to show it. Runs every frame so the
        // 'draw' toggle is the single switch — flip it and the next solve starts/stops collecting.
        private void Update()
        {
            if (!handAnimator) TryGetComponent(out handAnimator);
            if (handAnimator) handAnimator.requestSolveDebug = draw;
        }

        private void OnDisable()
        {
            if (handAnimator) handAnimator.requestSolveDebug = false;
        }

        private void OnDrawGizmos()
        {
            if (!draw) return;
            if (!handAnimator && !TryGetComponent(out handAnimator)) return;

            var dbg = handAnimator.LastSolveDebug;
            if (dbg == null || !dbg.hasData) return;

            for (int f = 0; f < dbg.fingers.Length; f++)
            {
                var fd = dbg.fingers[f];
                if (fd == null || fd.state == FingerSolveState.NoData || fd.probeCount == 0) continue;

                Color fingerColor = StateColor(fd.state);
                float radius = (fd.probeRadius > 0f ? fd.probeRadius : fallbackRadius) * sphereScale;

                for (int p = 0; p < fd.probeCount && p < fd.probes.Length; p++)
                {
                    var probe = fd.probes[p];
                    if (contactedJointsOnly && !probe.contacted) continue;

                    if (drawSpheres)
                    {
                        // Per-joint colour: a contacted joint always pops (this is the segment that
                        // stopped the finger); a non-contacted joint on a gripping finger stays faint;
                        // on a no-contact finger every joint takes the finger colour.
                        if (probe.contacted)
                            Gizmos.color = contactedColor;
                        else
                            Gizmos.color = fd.state == FingerSolveState.Contacted ? idleJointColor : fingerColor;
                        Gizmos.DrawWireSphere(probe.position, radius);
                    }

                    if (drawContactNormals && probe.contacted && probe.contactNormal != Vector3.zero)
                    {
                        Gizmos.color = normalColor;
                        Gizmos.DrawLine(probe.contactPoint, probe.contactPoint + probe.contactNormal * normalLength);
                    }
                }

#if UNITY_EDITOR
                if (drawLabels) DrawFingerLabel(f, fd);
#endif
            }
        }

        private Color StateColor(FingerSolveState state) => state switch
        {
            FingerSolveState.Contacted   => contactedColor,
            FingerSolveState.NoContact   => noContactColor,
            FingerSolveState.RelaxedFist => relaxedColor,
            _                            => idleJointColor,
        };

#if UNITY_EDITOR
        private static readonly string[] s_FingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

        private void DrawFingerLabel(int fingerIdx, FingerSolveDebug fd)
        {
            if (fd.probeCount == 0) return;

            // Anchor the label at the finger's most-distal sampled joint.
            Vector3 anchor = fd.probes[fd.probeCount - 1].position;

            var sb = new System.Text.StringBuilder();
            sb.Append(s_FingerNames[fingerIdx]).Append(": ").Append(fd.state);
            if (fd.chosenClosed) sb.Append(" [").Append(fd.chosenClosed.name).Append(']');
            sb.Append("\n t:");
            for (int p = 0; p < fd.probeCount; p++)
                sb.Append(' ').Append(fd.probes[p].lockedT.ToString("0.00"));

            // Labels stay fully opaque regardless of the sphere alphas — translucent text is unreadable.
            Color c = tintLabelsByState ? StateColor(fd.state) : labelColor;
            c.a = 1f;
            UnityEditor.Handles.color = c;
            UnityEditor.Handles.Label(anchor + Vector3.up * labelOffset, sb.ToString());
        }
#endif
    }
}
