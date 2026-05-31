using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Run this in the OLD project first to collect old Pose references,
    /// then update the project so HandPoser uses PoseScriptableObject,
    /// and finally open the same scene again to match and apply references.
    ///
    /// - 'collectedData' stores which HandPoser object had which old pose names.
    /// - 'matchedPoses' is a user-editable list mapping each old pose name to a new PoseScriptableObject.
    /// - "Create Matches" will attempt to auto-match any known PoseScriptableObjects by name,
    ///   leaving unmatched or uncertain entries with newPoseSO = null for the user to fix manually.
    /// - "Apply Matches" will assign the matched PoseScriptableObject references to the updated HandPoser.
    /// </summary>
    public class HandPoserMigrationTool : MonoBehaviour
    {
        [Serializable]
        public class HandPoserData
        {
            public string gameObjectPath; // Path to the GameObject in the scene hierarchy
            public string leftPoseName;
            public string rightPoseName;
            public string leftAnimPoseName;
            public string rightAnimPoseName;
        }

        [Serializable]
        public struct PoseMatch
        {
            public string oldPoseName; // Name of the old Pose
            public PoseScriptableObject newPoseSO; // The matched PoseScriptableObject (editable by user)
        }

        // Collected from old HandPosers (scene-based). Saved as strings so they survive the update.
        public List<HandPoserData> collectedData = new List<HandPoserData>();

        // A user-editable list of all unique old pose names (auto-collected), mapped to new PoseScriptableObjects.
        public List<PoseMatch> matchedPoses = new List<PoseMatch>();

        public List<HandPoser> missingPosePosers = new List<HandPoser>();


#if UNITY_EDITOR
        [CustomEditor(typeof(HandPoserMigrationTool))]
        public class HandPoserMigrationToolEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                var tool = (HandPoserMigrationTool)target;

                EditorGUILayout.HelpBox(
                    "1) In OLD project: Click [Collect Old References].\n" +
                    "2) Update HandPoser to use PoseScriptableObject.\n" +
                    "3) In NEW project: Click [Create Matches] to auto-match.\n" +
                    "   Then manually fix unmatched pairs in 'matchedPoses'.\n" +
                    "4) Click [Apply Matches].",
                    MessageType.Info
                );

                EditorGUILayout.Space();

                // Disable Collect button if we already have data
                bool hasExistingData = tool.collectedData.Count > 0;
                if (hasExistingData)
                {
                    EditorGUILayout.HelpBox(
                        "Old data is already collected.\n" +
                        "Clear 'collectedData' if you wish to collect again.",
                        MessageType.Warning
                    );
                }

                EditorGUI.BeginDisabledGroup(hasExistingData);
                if (GUILayout.Button("Collect Old References", GUILayout.Height(25)))
                {
                    tool.CollectOldReferences();
                }

                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("Create Matches", GUILayout.Height(25)))
                {
                    tool.CreateMatches();
                }

                if (GUILayout.Button("Apply Matches", GUILayout.Height(25)))
                {
                    tool.ApplyMatches();
                }
                
                if (GUILayout.Button("Find Missing Default Poses", GUILayout.Height(25)))
                {
                    tool.FindMissingDefaultPoses();
                }

                EditorGUILayout.Space();

                // Show collected data (read-only details, but we'll just expose it for debugging)
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("collectedData"),
                    new GUIContent("Collected Data (Old)"),
                    true
                );

                // Show matched poses (editable by user)
                SerializedProperty matchedPosesProperty = serializedObject.FindProperty("matchedPoses");

                // Force each list element to be expanded
                for (int i = 0; i < matchedPosesProperty.arraySize; i++)
                {
                    SerializedProperty element = matchedPosesProperty.GetArrayElementAtIndex(i);
                    element.isExpanded = true;
                }

                EditorGUILayout.PropertyField(
                    matchedPosesProperty,
                    new GUIContent("Matched Poses (Editable)"),
                    true
                );

                // Show the missingPosePosers list
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("missingPosePosers"),
                    new GUIContent("HandPosers Missing Default Poses"),
                    true
                );

                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Collects old Pose references from existing HandPoser components.
        /// Stores them as strings in 'collectedData', including the GameObject path.
        /// </summary>
        public void CollectOldReferences()
        {
            collectedData.Clear();

            // Find all old HandPoser components in the scene (the old script version).
            var oldPosers = FindObjectsByType<HandPoser>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var poser in oldPosers)
            {
                SerializedObject so = new SerializedObject(poser);

                // The old fields had type Pose
                var leftProp = so.FindProperty("leftHandPose");
                var rightProp = so.FindProperty("rightHandPose");
                var leftAnimProp = so.FindProperty("LeftHandAnimationPose");
                var rightAnimProp = so.FindProperty("RightHandAnimationPose");

                string leftName = leftProp?.objectReferenceValue ? leftProp.objectReferenceValue.name : "";
                string rightName = rightProp?.objectReferenceValue ? rightProp.objectReferenceValue.name : "";
                string leftAnimName = leftAnimProp?.objectReferenceValue ? leftAnimProp.objectReferenceValue.name : "";
                string rightAnimName =
                    rightAnimProp?.objectReferenceValue ? rightAnimProp.objectReferenceValue.name : "";

                string gameObjectPath = GetGameObjectPath(poser.gameObject);

                collectedData.Add(new HandPoserData
                {
                    gameObjectPath = gameObjectPath,
                    leftPoseName = leftName,
                    rightPoseName = rightName,
                    leftAnimPoseName = leftAnimName,
                    rightAnimPoseName = rightAnimName
                });
            }

            EditorUtility.SetDirty(this);
            Debug.Log($"Collected references from {collectedData.Count} HandPosers in the scene.");
        }

        /// <summary>
        /// Auto-creates or updates the 'matchedPoses' list using the collected old pose names.
        /// Attempts to find PoseScriptableObject assets by name for quick auto-matching.
        /// </summary>
        public void CreateMatches()
        {
            // 1. Gather all old pose names from collectedData
            var allOldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in collectedData)
            {
                if (!string.IsNullOrEmpty(item.leftPoseName)) allOldNames.Add(item.leftPoseName);
                if (!string.IsNullOrEmpty(item.rightPoseName)) allOldNames.Add(item.rightPoseName);
                if (!string.IsNullOrEmpty(item.leftAnimPoseName)) allOldNames.Add(item.leftAnimPoseName);
                if (!string.IsNullOrEmpty(item.rightAnimPoseName)) allOldNames.Add(item.rightAnimPoseName);
            }

            // 2. Gather all existing PoseScriptableObject assets in the project
            var allPoseSOs = AssetDatabase.FindAssets("t:PoseScriptableObject")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<PoseScriptableObject>(path))
                .Where(x => x != null)
                .ToList();

            // 3. For each old name, see if we already have a match in matchedPoses
            //    If not, try to auto-find a matching PoseScriptableObject
            //    If found, set newPoseSO; otherwise, leave it null for manual assignment
            foreach (var oldName in allOldNames)
            {
                var existing = matchedPoses.FirstOrDefault(m =>
                    m.oldPoseName.Equals(oldName, StringComparison.OrdinalIgnoreCase));

                // If it's already in matchedPoses, skip
                if (!string.IsNullOrEmpty(existing.oldPoseName))
                    continue;

                // Otherwise, add a new entry
                var poseMatch = new PoseMatch { oldPoseName = oldName, newPoseSO = null };

                // Try exact match
                var exact = allPoseSOs.FirstOrDefault(so =>
                    so.name.Equals(oldName, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    poseMatch.newPoseSO = exact;
                }
                else
                {
                    // Try partial match
                    poseMatch.newPoseSO = allPoseSOs.FirstOrDefault(so =>
                        so.name.IndexOf(oldName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        oldName.IndexOf(so.name, StringComparison.OrdinalIgnoreCase) >= 0
                    );
                }

                matchedPoses.Add(poseMatch);
            }

            // Optionally remove stale entries in matchedPoses that are not in allOldNames
            matchedPoses.RemoveAll(m => !allOldNames.Contains(m.oldPoseName, StringComparer.OrdinalIgnoreCase));

            EditorUtility.SetDirty(this);
            Debug.Log($"CreateMatches() updated. Found {matchedPoses.Count} old pose names total.");
        }

        /// <summary>
        /// Assigns the matched PoseScriptableObjects to each HandPoser in the scene (new script).
        /// </summary>
        public void ApplyMatches()
        {
            int updatedCount = 0;

            // For each collected GameObject in the scene, set the new PoseScriptableObject references
            foreach (var item in collectedData)
            {
                var go = FindGameObjectByPath(item.gameObjectPath);
                if (!go)
                {
                    Debug.LogWarning($"Could not find GameObject at path: {item.gameObjectPath}");
                    continue;
                }

                var poser = go.GetComponent<HandPoser>();
                if (!poser)
                {
                    Debug.LogWarning($"No HandPoser on '{item.gameObjectPath}'. Skipping.");
                    continue;
                }

                var soPoser = new SerializedObject(poser);
                var leftProp = soPoser.FindProperty("leftHandPose");
                var rightProp = soPoser.FindProperty("rightHandPose");
                var leftAnimProp = soPoser.FindProperty("LeftHandAnimationPose");
                var rightAnimProp = soPoser.FindProperty("RightHandAnimationPose");

                leftProp.objectReferenceValue = FindMatch(item.leftPoseName);
                rightProp.objectReferenceValue = FindMatch(item.rightPoseName);
                leftAnimProp.objectReferenceValue = FindMatch(item.leftAnimPoseName);
                rightAnimProp.objectReferenceValue = FindMatch(item.rightAnimPoseName);

                soPoser.ApplyModifiedProperties();
                updatedCount++;
            }

            EditorUtility.SetDirty(this);
            Debug.Log($"ApplyMatches() completed. Updated {updatedCount} HandPosers.");

            PoseScriptableObject FindMatch(string oldName)
            {
                if (string.IsNullOrEmpty(oldName)) return null;
                var match = matchedPoses.FirstOrDefault(m =>
                    m.oldPoseName.Equals(oldName, StringComparison.OrdinalIgnoreCase)
                );
                return match.newPoseSO;
            }
        }

        // -------------------------------------------------------------------------
        // Utility: Returns the path of a GameObject in the scene hierarchy
        // -------------------------------------------------------------------------
        private string GetGameObjectPath(GameObject obj)
        {
            if (!obj) return "";
            string path = obj.name;
            var parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        // -------------------------------------------------------------------------
        // Utility: Finds a GameObject by its full path in the scene
        // -------------------------------------------------------------------------
        private GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var split = path.Split('/');
            GameObject current = null;
            foreach (var part in split)
            {
                if (current == null)
                {
                    current = GameObject.Find(part);
                }
                else
                {
                    var child = current.transform.Find(part);
                    current = child ? child.gameObject : null;
                }

                if (!current) return null;
            }

            return current;
        }

        public void FindMissingDefaultPoses()
        {
            missingPosePosers.Clear();

            // Find all updated HandPoser components (which now have PoseScriptableObject fields)
            var allPosers = FindObjectsByType<HandPoser>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var poser in allPosers)
            {
                bool isMissing = false || (poser.leftHandPose == null || poser.rightHandPose == null);

                // Check for missing default (required) poses

                // If hasAnimationPose == true, check animation poses
                // (Assuming the field is named exactly "hasAnimationPose" in HandPoser)
                // If it’s a property getter, adapt accordingly.
                var serializedPoser = new SerializedObject(poser);
                var animPoseProp = serializedPoser.FindProperty("hasAnimationPose");
                bool hasAnimPose = animPoseProp != null && animPoseProp.boolValue;

                if (hasAnimPose)
                {
                    if (poser.LeftHandAnimationPose == null || poser.RightHandAnimationPose == null)
                    {
                        isMissing = true;
                    }
                }

                if (isMissing)
                {
                    missingPosePosers.Add(poser);
                }
            }

            Debug.Log($"Found {missingPosePosers.Count} HandPoser(s) missing default poses.");
        }
#endif
    }
}
