// Author MikeNspired. 
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class HandPoserSettings : ScriptableObject
    {
        private static HandPoserSettings _instance;

        public static HandPoserSettings Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = Resources.Load<HandPoserSettings>("HandPoserSettings");
                if (_instance != null) return _instance;


                _instance = CreateInstance<HandPoserSettings>();

#if UNITY_EDITOR
                // Create inside a Resources folder so the Resources.Load above finds it next time.
                string resourcesDir = System.IO.Path.Combine("Assets", "Resources");
                if (Directory.Exists(resourcesDir) == false)
                    Directory.CreateDirectory(resourcesDir);
                UnityEditor.AssetDatabase.CreateAsset(_instance,
                    System.IO.Path.Combine(resourcesDir, "HandPoserSettings.asset"));
                UnityEditor.AssetDatabase.SaveAssets();
#endif
                ShowNotSetupWarning();
                return _instance;
            }
        }


        public HandAnimator LeftHand;
        public HandAnimator RightHand;
        public PoseScriptableObject DefaultPose;
        public List<PoseScriptableObject> ReferencePoses;
        public bool sortReferencePoses;

        [Header("Dynamic Pose — Global Defaults")]
        [Tooltip("How far (m) a grab can land from the authored grip before it counts as off-axis (DYNAMIC). Per-object XRHandPoser can override.")]
        public float dynamicPositionThreshold = 0.08f;
        [Tooltip("How far (deg) a grab can rotate from the authored grip before it counts as off-axis (DYNAMIC). Per-object XRHandPoser can override.")]
        public float dynamicRotationThreshold = 45f;

        [Tooltip("Solver tuning used by EVERY dynamic solve (grab path and grasp probe) unless overridden " +
                 "per object on XRHandPoser or per probe on HandGraspProbe — the single source of truth.")]
        public HandSolverSettings dynamicSolver = new HandSolverSettings();

        [Header("Dynamic Pose — Object Seating")]
        [Tooltip("On a DYNAMIC grab, kinematically settle the object a small capped distance toward the " +
                 "hand's palm point BEFORE the solve, closing the air gap so the grab reads as held rather " +
                 "than hovering. Pure position nudge, hard-capped — it can never reintroduce the " +
                 "authored-grip snap. Off = hold exactly where grabbed (previous behavior).")]
        public bool dynamicSeatInPalm = false;
        [Tooltip("Maximum distance (m) the seating settle may move the object toward the palm.")]
        public float dynamicSeatMaxDistance = 0.03f;
        [Tooltip("Air gap (m) seating keeps between the palm point and the object surface, so the " +
                 "object settles against the palm instead of into it.")]
        public float dynamicSeatClearance = 0.01f;

        [Header("Dynamic Pose — Debug")]
        [Tooltip("Master switch for the per-finger solve visualization. When on, dynamic solves record " +
                 "telemetry and a HandPoseSolveDebugDrawer on the hand draws, per finger: a sphere at each " +
                 "sampled joint (green = contacted, red = no contact, yellow = relaxed-fist), the locked t, " +
                 "the chosen closed pose, and the contact normal. Off = zero overhead.")]
        public bool drawSolveDebug = false;

        [Header("Dynamic Grasp — Global Defaults")]
        [Tooltip("Require the thumb to make contact for a dynamic grasp to hold.")]
        public bool graspRequireThumb = true;
        [Tooltip("Number of non-thumb fingers that must contact for a dynamic grasp to hold.")]
        [Range(0, 4)] public int graspRequiredFingers = 2;
        [Tooltip("What to do when a dynamic grasp fails its contact test.")]
        public FailedGraspResponse failedGraspResponse = FailedGraspResponse.Drop;

        private void OnValidate()
        {
            if (sortReferencePoses)
                ReferencePoses = ReferencePoses.OrderBy(x => x).ToList();
        }

        public static void ShowNotSetupWarning()
        {
            Debug.LogError("HandPoserSettings is not setup correctly");
            if (!_instance)
            {
                Debug.LogWarning("Please create HandPoserSettings");
                return;
            }

            if (!_instance.LeftHand)
                Debug.LogWarning("Assign Left hand in HandPoserSettings");
            if (!_instance.RightHand)
                Debug.LogWarning("Assign Right hand in HandPoserSettings");
            if (!_instance.DefaultPose)
                Debug.LogWarning("Assign DefaultPose in HandPoserSettings");
        }
    }
}