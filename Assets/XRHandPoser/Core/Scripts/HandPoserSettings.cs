// Author MikeNspired. 
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
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
        public Pose DefaultPose;
        public List<Pose> ReferencePoses;
        public bool sortReferencePoses;

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