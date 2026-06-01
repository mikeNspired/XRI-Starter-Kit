using System;
using System.Linq;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public enum PalmColliderShape { Sphere, Box }

    [Serializable]
    public class FingerColliderConfig
    {
        public string fingerName = "finger";
        [Range(0.1f, 3f)] public float radiusMultiplier = 1f;
        [Range(0.5f, 2f)] public float heightMultiplier = 1f;
    }

    [Serializable]
    public class JointColliderOverride
    {
        public string jointName;
        public bool overrideRadius;
        [Min(0.001f)] public float radius = 0.01f;
        public bool overrideHeight;
        [Min(0.001f)] public float height = 0.02f;
        public Vector3 localOffset;
    }

    [CreateAssetMenu(fileName = "HandColliderConfig", menuName = "XRI Starter Kit/Hand Collider Config")]
    public class HandColliderConfig : ScriptableObject
    {
        [Header("Global")]
        [Range(0.05f, 1f)]  public float globalRadiusMultiplier = 0.35f;
        [Range(0.5f, 2f)]   public float globalHeightMultiplier = 1f;

        [Header("Joints")]
        [Tooltip("Joints whose name contains any of these fragments (case-insensitive) are treated as " +
                 "helper / non-rotating joints: they are skipped as collider owners but bridged through " +
                 "so the real bones on either side stay linked. e.g. \"aux\", \"twist\", \"helper\".")]
        public string[] ignoreJointNameContains = { "aux" };

        [Tooltip("Leaf joints whose name contains any of these fragments are non-rotating fingertip " +
                 "markers: the parent bone capsule already reaches them, so they get no collider. " +
                 "Leaves that do NOT match are treated as the last real joint (e.g. a DIP) and get an " +
                 "extended distal bone past them. Leave empty for rigs that have no tip-marker transform.")]
        public string[] fingertipMarkerNameContains = { };

        [Tooltip("Length of the extended distal bone as a fraction of its parent bone's length. " +
                 "Used only for last-real-joint leaves that have no child transform to define a tip.")]
        [Range(0.3f, 1.5f)] public float distalLengthMultiplier = 0.8f;

        [Header("Fixups & Diagnostics")]
        [Tooltip("OFF by default. When the last finger joint isn't oriented down the bone (model not " +
                 "posed straight), a single-axis CapsuleCollider tilts off the finger. Enable to place " +
                 "the distal capsule on an auto-created child GameObject oriented down the bone, fixing " +
                 "tilt and any off-axis center offset. A well-built model does not need this.")]
        public bool orientDistalTipWithChild = false;

        [Tooltip("Log a warning when a hand (or a parent) has negative/mirrored scale, which Unity " +
                 "colliders cannot represent correctly. Helps diagnose mirrored left hands.")]
        public bool warnOnMirroredScale = true;

        [Tooltip("Verbose: log each step of collider building (joint count, skips, per-joint outcomes). " +
                 "Turn on to diagnose 'Rebuild does nothing'.")]
        public bool verboseBuildLogging = false;

        [Header("Palm")]
        public bool addPalmCollider = true;
        public PalmColliderShape palmShape = PalmColliderShape.Box;
        [Range(0.01f, 0.1f)] public float palmRadius = 0.035f;          // used when palmShape == Sphere
        public Vector3 palmBoxSize = new Vector3(0.08f, 0.025f, 0.07f); // used when palmShape == Box
        public Vector3 palmOffset = Vector3.zero;                       // collider center, both shapes

        [Header("Per-Finger Multipliers")]
        public FingerColliderConfig[] fingerConfigs = new FingerColliderConfig[]
        {
            new() { fingerName = "Thumb",  radiusMultiplier = 1.2f },
            new() { fingerName = "Index",  radiusMultiplier = 1.0f },
            new() { fingerName = "Middle", radiusMultiplier = 1.0f },
            new() { fingerName = "Ring",   radiusMultiplier = 0.9f },
            new() { fingerName = "Pinky",  radiusMultiplier = 0.8f },
        };

        [Header("Per-Joint Overrides")]
        public JointColliderOverride[] jointOverrides = Array.Empty<JointColliderOverride>();

        public FingerColliderConfig GetFingerConfig(string jointName)
        {
            foreach (var fc in fingerConfigs)
                if (jointName.IndexOf(fc.fingerName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return fc;
            return null;
        }

        public JointColliderOverride GetJointOverride(string jointName)
        {
            foreach (var ov in jointOverrides)
                if (ov.jointName == jointName) return ov;
            return null;
        }

        public bool IsIgnoredJoint(string jointName) =>
            ignoreJointNameContains != null &&
            ignoreJointNameContains.Any(f => !string.IsNullOrEmpty(f) &&
                jointName.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);

        public bool IsFingertipMarker(string jointName) =>
            fingertipMarkerNameContains != null &&
            fingertipMarkerNameContains.Any(f => !string.IsNullOrEmpty(f) &&
                jointName.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
