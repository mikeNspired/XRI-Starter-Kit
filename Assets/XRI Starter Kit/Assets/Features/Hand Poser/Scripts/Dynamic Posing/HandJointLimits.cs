// Author MikeNspired.
using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Optional per-joint clamp on the dynamic solve's curl value t, for when the Closed pose alone is
    /// not a tight enough limit (e.g. a base knuckle that must never fold past halfway, or a thumb joint
    /// that should keep a minimum bend). Joints are matched by name, the same convention the pose system
    /// uses everywhere; joints not listed are unclamped. Assign on <see cref="HandDynamicPoses"/> —
    /// everything is behind a null check, so no asset means no change in behavior.
    ///
    /// The clamp applies to the final per-joint t the solvers emit AND to the poses they sweep through,
    /// so contact detection and the returned pose stay consistent (progressive solver; the curl-sweep
    /// fallback clamps only its output, see CurlSweepSolver).
    /// </summary>
    [CreateAssetMenu(fileName = "HandJointLimits", menuName = "XRI Starter Kit/Hand Joint Limits")]
    public class HandJointLimits : ScriptableObject
    {
        [System.Serializable]
        public struct JointLimit
        {
            [Tooltip("Joint transform name, exactly as in the hand skeleton / pose assets.")]
            public string jointName;
            [Tooltip("Lowest curl t this joint may take (0 = the Open pose).")]
            [Range(0f, 1f)] public float minCurl;
            [Tooltip("Highest curl t this joint may take (1 = the Closed pose).")]
            [Range(0f, 1f)] public float maxCurl;
        }

        public JointLimit[] limits = new JointLimit[0];

        private Dictionary<string, JointLimit> lookup;

        /// Clamps a joint's curl t into its authored [min, max]; unlisted joints pass through unchanged.
        public float Clamp(string _jointName, float _t)
        {
            if (lookup == null) BuildLookup();
            return lookup.TryGetValue(_jointName, out var limit)
                ? Mathf.Clamp(_t, limit.minCurl, Mathf.Max(limit.minCurl, limit.maxCurl))
                : _t;
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, JointLimit>(limits?.Length ?? 0);
            if (limits == null) return;
            foreach (var limit in limits)
                if (!string.IsNullOrEmpty(limit.jointName))
                    lookup[limit.jointName] = limit;
        }

        private void OnValidate() => lookup = null; // re-resolve after inspector edits
    }
}
