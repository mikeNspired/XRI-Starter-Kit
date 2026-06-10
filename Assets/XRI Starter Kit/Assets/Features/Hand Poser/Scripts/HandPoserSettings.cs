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
                string assetPath = System.IO.Path.Combine("Assets", "HandPoserSettings.asset");
                if (Directory.Exists(assetPath) == false)
                    Directory.CreateDirectory(assetPath);
                UnityEditor.AssetDatabase.CreateAsset(_instance, assetPath);
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
        [Tooltip("Curl-sweep resolution: number of t steps per finger.")]
        public int dynamicStepCount = 15;
        [Tooltip("Fingertip probe sphere radius (m) used to detect contact during the sweep.")]
        public float dynamicProbeRadius = 0.02f;
        [Tooltip("Joints from the fingertip inward to sphere-test each step. 1 = tip only; " +
                 "2+ catches a finger wrapping the object even when the tip slips past it.")]
        [Range(1, 4)] public int dynamicSamplesPerFinger = 2;
        [Tooltip("Use the per-joint progressive-curl solver (fingertips wrap onto surfaces even when " +
                 "the knuckle rests on the object). Off uses the simpler single-t-per-finger curl sweep.")]
        public bool useProgressiveSolver = true;
        [Tooltip("Curl for a finger that touches nothing (progressive solver). Like a human grabbing, " +
                 "non-gripping fingers settle into a relaxed fist instead of splaying open. " +
                 "1 = full fist, 0 = fully open; ~0.7 reads as a natural relaxed grip.")]
        [Range(0, 1)] public float dynamicNoContactCurl = 0.7f;

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