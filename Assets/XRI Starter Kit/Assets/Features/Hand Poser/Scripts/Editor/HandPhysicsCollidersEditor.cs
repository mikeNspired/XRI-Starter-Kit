using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [CustomEditor(typeof(HandPhysicsColliders))]
    public class HandPhysicsCollidersEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Rebuild Colliders", GUILayout.Height(30)))
            {
                var poser = (HandPhysicsColliders)target;
                poser.BuildColliders();
                EditorUtility.SetDirty(poser);
            }
        }
    }
}
