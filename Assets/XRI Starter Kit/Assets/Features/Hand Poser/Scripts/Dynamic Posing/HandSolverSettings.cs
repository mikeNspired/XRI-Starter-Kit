// Author MikeNspired.
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// The shared block of solver tuning, defined ONCE and reused everywhere a dynamic solve is
    /// configured: <see cref="HandPoserSettings.dynamicSolver"/> holds the global defaults (the single
    /// source of truth), <see cref="XRHandPoser"/> embeds one behind its Override Global Settings toggle
    /// for per-object deviations, and <see cref="HandGraspProbe"/> embeds one behind its Override Solver
    /// Settings toggle for the per-frame cost budget. Add new solver knobs HERE and they appear in all
    /// three places.
    /// </summary>
    [System.Serializable]
    public class HandSolverSettings
    {
        [Tooltip("Use the per-joint progressive-curl solver (fingertips wrap onto surfaces even when " +
                 "the knuckle rests on the object). Off uses the simpler single-t-per-finger curl sweep.")]
        public bool useProgressiveSolver = true;

        [Tooltip("Curl-sweep resolution: number of t steps per finger. Higher = finer contact, more " +
                 "queries. The grab path can afford ~15; a per-frame probe wants less (~8).")]
        [Range(4, 30)] public int stepCount = 15;

        [Tooltip("Contact probe sphere radius (m) used to detect the surface during the sweep.")]
        public float probeRadius = 0.02f;

        [Tooltip("Curl-sweep solver only. Joints from the fingertip inward to sphere-test each step. " +
                 "1 = tip only; 2+ catches a finger wrapping the object even when the tip slips past it.")]
        [Range(1, 4)] public int samplesPerFinger = 2;

        [Tooltip("Progressive solver only. Contact samples along each tested bone segment. 2 = midpoint " +
                 "+ endpoint; higher catches thin edges so wraps read as a drape. Multiplies query cost.")]
        [Range(1, 5)] public int samplesPerSegment = 2;

        [Tooltip("Progressive solver only. Curl for a finger that touches nothing — non-gripping fingers " +
                 "settle into a relaxed fist instead of opening. 1 = full fist; ~0.7 reads natural. " +
                 "Superseded per hand by a Relaxed Pose on HandDynamicPoses when one is assigned.")]
        [Range(0, 1)] public float noContactCurl = 0.7f;

        [Tooltip("Progressive solver only. After a finger grips, how much each further-out joint keeps " +
                 "curling when it finds nothing (gentle wrap) instead of fisting. ~0.33 reads natural.")]
        [Range(0, 1)] public float distalFollowCurl = 0.33f;

        [Tooltip("Per-finger calibration: enable mask (a disabled finger keeps the authored pose), " +
                 "probe-radius scale, max-curl anti-overshoot clamp, and press bias.")]
        public PerFingerSolveSettings fingerSettings = new PerFingerSolveSettings();

        /// Copies this block onto a solve context — the one place the field list is consumed.
        public void ApplyTo(HandSolveContext _ctx)
        {
            _ctx.stepCount         = stepCount;
            _ctx.probeRadius       = probeRadius;
            _ctx.samplesPerFinger  = samplesPerFinger;
            _ctx.samplesPerSegment = samplesPerSegment;
            _ctx.noContactCurl     = noContactCurl;
            _ctx.distalFollowCurl  = distalFollowCurl;
            _ctx.fingerSettings    = fingerSettings;
        }
    }
}
