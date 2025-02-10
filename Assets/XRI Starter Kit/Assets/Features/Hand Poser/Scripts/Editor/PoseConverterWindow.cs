using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit.Editor
{
    public class PoseConverterWindow : EditorWindow
    {
        [SerializeField] private string defaultRootJointName = "Hand_R_Jnt"; // Default root joint name
        [SerializeField] private string filePostfix = ""; // Optional postfix for the file name
        [SerializeField] private string filePrefix = ""; // Optional prefix for the file name
        [SerializeField] private List<GameObject> oldPosePrefabs = new(); // Drag-and-drop list for prefabs

        private Vector2 scrollPosition;

        [MenuItem("Window/HandPoser/Convert Old Pose Prefabs")]
        private static void OpenWindow()
        {
            var window = GetWindow<PoseConverterWindow>("Pose Converter");
            window.minSize = new Vector2(400, 500); // Minimum size
            window.Show();
        }


        private void OnEnable()
        {
            if (oldPosePrefabs == null) oldPosePrefabs = new List<GameObject>();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Convert Old Pose Prefabs to PoseScriptableObjects", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Instructions:\n" +
                "- The default root joint name is set to 'Hand_R_Jnt'.\n" +
                "- If your hand model uses a different root joint name, update the field below with the correct name.\n" +
                "- The first joint in the scriptable object will always be treated as the root joint.\n" +
                "- Use the Prefix/Postfix fields to customize the saved file names.\n" +
                "- Drag and drop multiple prefabs into the list for batch conversion.",
                MessageType.Info
            );

            defaultRootJointName = EditorGUILayout.TextField("Root Joint Name", defaultRootJointName);
            filePrefix = EditorGUILayout.TextField("File Name Prefix", filePrefix);
            filePostfix = EditorGUILayout.TextField("File Name Postfix", filePostfix);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Old Pose Prefabs (Drag and Drop)", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            SerializedObject serializedObject = new SerializedObject(this);
            SerializedProperty serializedProperty = serializedObject.FindProperty("oldPosePrefabs");
            EditorGUILayout.PropertyField(serializedProperty, true);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("Convert All", GUILayout.Height(30))) ConvertAllOldPosePrefabs();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Danger Zone", EditorStyles.boldLabel);
            if (GUILayout.Button("Delete Old Prefabs", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                        "Delete Old Prefabs",
                        "Are you sure you want to delete the old prefabs? This action cannot be undone. Please back up your work before proceeding.",
                        "Delete",
                        "Cancel"))
                {
                    DeleteOldPrefabs();
                }
            }
        }

        private void ConvertAllOldPosePrefabs()
        {
            if (oldPosePrefabs.Count == 0)
            {
                Debug.LogWarning("No old Pose prefabs selected.");
                return;
            }

            foreach (var prefab in oldPosePrefabs)
            {
                if (!prefab)
                {
                    Debug.LogWarning("Skipping null prefab in the list.");
                    continue;
                }

                ConvertOldPosePrefab(prefab);
            }

            Debug.Log("Conversion process complete!");
        }

        private void ConvertOldPosePrefab(GameObject oldPosePrefab)
        {
            // Instantiate temporarily to read the hierarchy
            var instance = PrefabUtility.InstantiatePrefab(oldPosePrefab) as GameObject;
            if (!instance)
            {
                Debug.LogError($"Failed to instantiate old pose prefab: {oldPosePrefab.name}");
                return;
            }

            var oldPoseMono = instance.GetComponent<Pose>();
            if (!oldPoseMono)
            {
                Debug.LogError($"No Pose (MonoBehaviour) found on prefab: {oldPosePrefab.name}");
                DestroyImmediate(instance);
                return;
            }

            // Collect joint data
            var jointList = new List<PoseScriptableObject.JointData>();
            GatherJointData(instance.transform, jointList);

            // Rename the root joint (first in hierarchy)
            if (jointList.Count > 0 && jointList[0].jointName != defaultRootJointName)
            {
                var rootJoint = jointList[0]; // Fix: Retrieve the struct
                rootJoint.jointName = defaultRootJointName; // Modify its property
                jointList[0] = rootJoint; // Reassign it back to the list
            }

            // Create a new scriptable object asset
            var newPose = CreateInstance<PoseScriptableObject>();
            newPose.joints = jointList.ToArray();

            // Save in the same directory as the old prefab, with prefix and postfix applied
            var prefabPath = AssetDatabase.GetAssetPath(oldPosePrefab);
            var directory = Path.GetDirectoryName(prefabPath);
            var fileName = $"{filePrefix}{oldPosePrefab.name}{filePostfix}.asset";
            var savePath = $"{directory}/{fileName}";

            AssetDatabase.CreateAsset(newPose, savePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Created PoseScriptableObject for '{oldPosePrefab.name}' at: {savePath}");
            DestroyImmediate(instance);
        }

        private void GatherJointData(Transform root, List<PoseScriptableObject.JointData> list)
        {
            var allTransforms = new List<Transform>();
            JointUtility.GatherTransformsForPose(root, allTransforms);

            foreach (var t in allTransforms)
            {
                // Build your JointData
                var data = new PoseScriptableObject.JointData
                {
                    jointName = t.name,
                    localPosition = t.localPosition,
                    localRotation = t.localRotation
                };
                list.Add(data);
            }
        }

        private void DeleteOldPrefabs()
        {
            foreach (var prefab in oldPosePrefabs)
            {
                if (!prefab)
                {
                    Debug.LogWarning("Skipping null prefab in the list.");
                    continue;
                }

                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    Debug.LogWarning($"Could not find path for prefab: {prefab.name}");
                    continue;
                }

                AssetDatabase.DeleteAsset(prefabPath);
            }

            AssetDatabase.SaveAssets();
            oldPosePrefabs.Clear();
        }
    }
}
