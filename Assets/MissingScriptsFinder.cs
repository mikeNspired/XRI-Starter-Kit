// using UnityEngine;
// using System.Collections.Generic;
// #if UNITY_EDITOR
// using UnityEditor;
//
//
// namespace MikeNspired.XRIStarterKit
// {
// // Make sure the script executes in Edit mode.
//     [ExecuteInEditMode]
//     public class MissingScriptsFinder : MonoBehaviour
//     {
//         // This public list will show the GameObjects with missing scripts in the Inspector.
//         [TableList] public List<GameObject> objectsWithMissingScripts = new List<GameObject>();
//
//         // Button to scan the scene for missing scripts.
//         [Button("Find Missing Scripts in Scene")]
//         public void FindMissingScripts()
//         {
//             objectsWithMissingScripts.Clear();
//
//             // Find all GameObjects (active and inactive) in the scene.
//             GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
//             int count = 0;
//
//             foreach (GameObject go in allGameObjects)
//             {
//                 // Skip assets or prefabs (only work with scene instances).
//                 if (EditorUtility.IsPersistent(go))
//                     continue;
//
//                 // Optionally skip objects that are hidden or not in a scene.
//                 if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
//                     continue;
//
//                 if (go.scene == null || string.IsNullOrEmpty(go.scene.name))
//                     continue;
//
//                 // Check if this GameObject has any missing script.
//                 if (HasMissingScripts(go))
//                 {
//                     objectsWithMissingScripts.Add(go);
//                     count++;
//                 }
//             }
//
//             Debug.Log($"Found {count} GameObject(s) with missing scripts in the scene.");
//         }
//
//         // Checks for missing scripts by iterating through the serialized 'm_Component' list.
//         private bool HasMissingScripts(GameObject go)
//         {
//             // Use a SerializedObject to access internal properties.
//             SerializedObject so = new SerializedObject(go);
//             SerializedProperty componentsProperty = so.FindProperty("m_Component");
//
//             // Iterate over all components attached to this GameObject.
//             for (int i = 0; i < componentsProperty.arraySize; i++)
//             {
//                 SerializedProperty componentProperty = componentsProperty.GetArrayElementAtIndex(i);
//                 // If the component reference is null, then a script is missing.
//                 if (componentProperty.objectReferenceValue == null)
//                 {
//                     return true;
//                 }
//             }
//
//             return false;
//         }
//
//         // Button to remove missing script references from the found GameObjects.
//         [Button("Remove Missing Scripts")]
//         public void RemoveMissingScripts()
//         {
//             int totalRemoved = 0;
//             foreach (GameObject go in objectsWithMissingScripts)
//             {
//                 if (go != null)
//                 {
//                     // Removes missing MonoBehaviour references.
//                     totalRemoved += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
//                 }
//             }
//
//             Debug.Log($"Removed {totalRemoved} missing script reference(s).");
//             // Refresh the list after cleaning.
//             FindMissingScripts();
//         }
//     }
// }
// #endif
