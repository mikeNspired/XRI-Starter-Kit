using System;
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
    }
}
