using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Draws the dynamic solver's per-finger telemetry as Scene-view gizmos so you can tell, at a glance,
    /// why each finger ended up straight or curled. Put this on the hand (alongside <see cref="HandAnimator"/>).
    ///
    /// Telemetry is only produced when the global "Draw Solve Debug" switch on the HandPoserSettings asset is
    /// on; with it off this component draws nothing and adds no cost. Works for both real grabs (XRHandPoser)
    /// and the free-hand touch driver — whoever solves publishes the data to <see cref="HandAnimator.LastSolveDebug"/>.
    ///
    /// Per finger it draws a sphere at every sampled joint, coloured by how the finger resolved:
    /// green = contacted, red = no contact, yellow = relaxed-fist fallback. Contact normals are drawn in cyan,
    /// and (in the editor) each finger is labelled with its chosen closed pose and per-joint locked t.
    /// </summary>
    public class HandPoseSolveDebugDrawer : MonoBehaviour
    {
        [Tooltip("The hand whose last solve to visualize. Auto-filled from this GameObject if left empty.")]
        [SerializeField] private HandAnimator handAnimator;

        [Tooltip("Local switch for this hand's gizmos. The global 'Draw Solve Debug' on HandPoserSettings " +
                 "must also be on for any telemetry to exist.")]
        [SerializeField] private bool draw = true;

        [Tooltip("Length (m) of the cyan contact-normal lines.")]
        [SerializeField] private float normalLength = 0.02f;

        [Tooltip("Fallback sphere radius (m) used if a finger reports no probe radius.")]
        [SerializeField] private float fallbackRadius = 0.008f;

        private void Reset() => handAnimator = GetComponent<HandAnimator>();

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
                float radius = fd.probeRadius > 0f ? fd.probeRadius : fallbackRadius;

                for (int p = 0; p < fd.probeCount && p < fd.probes.Length; p++)
                {
                    var probe = fd.probes[p];

                    Gizmos.color = fingerColor;
                    Gizmos.DrawWireSphere(probe.position, radius);

                    if (probe.contacted && probe.contactNormal != Vector3.zero)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(probe.contactPoint, probe.contactPoint + probe.contactNormal * normalLength);
                    }
                }

#if UNITY_EDITOR
                DrawFingerLabel(f, fd);
#endif
            }
        }

        private static Color StateColor(FingerSolveState state) => state switch
        {
            FingerSolveState.Contacted   => Color.green,
            FingerSolveState.NoContact   => Color.red,
            FingerSolveState.RelaxedFist => Color.yellow,
            _                            => Color.gray,
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

            UnityEditor.Handles.color = StateColor(fd.state);
            UnityEditor.Handles.Label(anchor + Vector3.up * 0.01f, sb.ToString());
        }
#endif
    }
}
