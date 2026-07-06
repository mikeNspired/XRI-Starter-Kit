// Author MikeNspired.
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Calibration for ONE finger of the dynamic solve. Lives inside <see cref="PerFingerSolveSettings"/>;
    /// the solvers read it per finger from <see cref="HandSolveContext.fingerSettings"/>. All fields default
    /// to "no effect" so an untouched block behaves exactly like the pre-calibration solver.
    /// </summary>
    [System.Serializable]
    public class FingerSolveCalibration
    {
        [Tooltip("Solve this finger dynamically. Off = the solver skips it entirely and emits no joints " +
                 "for it, so it keeps the authored pose (grab path) or the current animation (probe).")]
        public bool solve = true;

        [Tooltip("Multiplier on the contact probe radius for this finger only. >1 makes the finger stop " +
                 "earlier (fixes a finger that curls through thin geometry); <1 lets it close snugger.")]
        [Range(0.25f, 3f)] public float probeRadiusScale = 1f;

        [Tooltip("Hard cap on this finger's curl t (1 = the full Closed pose). Anti-overshoot: lower it " +
                 "for a finger that curls past the surface into the air (e.g. ring/pinky hanging under " +
                 "an object). Also caps the relaxed/no-contact curl and the distal follow spiral.")]
        [Range(0f, 1f)] public float maxCurl = 1f;

        [Tooltip("Curl offset added AFTER a joint locks on contact, in t units. Positive presses the " +
                 "finger slightly into the surface (closes a visible hover gap); negative backs it off. " +
                 "Clamped to [0, Max Curl]. 0 = lock exactly where the sweep stopped.")]
        [Range(-0.2f, 0.2f)] public float pressBias = 0f;
    }

    /// <summary>
    /// Per-finger solve calibration (thumb=0 … pinky=4) with global defaults on the
    /// <see cref="HandPoserSettings"/> asset and an optional per-object override on
    /// <see cref="XRHandPoser"/>. Null-safe access via <see cref="Get"/> — a null settings object
    /// (or finger index out of range) yields the all-defaults calibration, so nothing in the solve
    /// pipeline ever needs a null check of its own.
    /// </summary>
    [System.Serializable]
    public class PerFingerSolveSettings
    {
        public FingerSolveCalibration thumb  = new FingerSolveCalibration();
        public FingerSolveCalibration index  = new FingerSolveCalibration();
        public FingerSolveCalibration middle = new FingerSolveCalibration();
        public FingerSolveCalibration ring   = new FingerSolveCalibration();
        public FingerSolveCalibration pinky  = new FingerSolveCalibration();

        public FingerSolveCalibration Finger(int i) => i switch
        {
            0 => thumb,
            1 => index,
            2 => middle,
            3 => ring,
            4 => pinky,
            _ => null,
        };

        private static readonly FingerSolveCalibration s_Default = new FingerSolveCalibration();

        /// Calibration for a finger, never null: missing settings or index fall back to all-defaults.
        public static FingerSolveCalibration Get(PerFingerSolveSettings settings, int fingerIndex)
        {
            var cal = settings?.Finger(fingerIndex);
            return cal ?? s_Default;
        }
    }
}
