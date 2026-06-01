using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [CustomEditor(typeof(HandPhysicsColliders))]
    public class HandPhysicsCollidersEditor : UnityEditor.Editor
    {
        private SerializedProperty configProp, interactLayersProp;
        private UnityEditor.Editor configEditor;
        private HandColliderConfig cachedConfig;

        void OnEnable()
        {
            configProp = serializedObject.FindProperty("config");
            interactLayersProp = serializedObject.FindProperty("interactLayers");
        }

        void OnDisable()
        {
            if (configEditor) DestroyImmediate(configEditor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(configProp);
            EditorGUILayout.PropertyField(interactLayersProp);
            serializedObject.ApplyModifiedProperties();

            var poser = (HandPhysicsColliders)target;
            var config = configProp.objectReferenceValue as HandColliderConfig;

            if (config == null)
            {
                EditorGUILayout.HelpBox("Assign a Hand Collider Config to tune and build colliders.", MessageType.Info);
                return;
            }

            // (Re)create the embedded config editor when the assigned asset changes.
            if (configEditor == null || cachedConfig != config)
            {
                if (configEditor) DestroyImmediate(configEditor);
                configEditor = CreateEditor(config);
                cachedConfig = config;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Collider Config (live)", EditorStyles.boldLabel);

            // Editing the config inline rebuilds colliders live in edit mode — no play mode,
            // no asset hopping. Shared config means both hands update together.
            HandColliderConfigEditor.JointNamesContext = GetJointNames(poser);
            EditorGUI.BeginChangeCheck();
            configEditor.OnInspectorGUI();
            HandColliderConfigEditor.JointNamesContext = null;
            if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
            {
                poser.BuildColliders();
                EditorUtility.SetDirty(poser);
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Colliders", GUILayout.Height(30)))
                {
                    poser.BuildColliders();
                    EditorUtility.SetDirty(poser);
                }
                if (GUILayout.Button("Clear Colliders", GUILayout.Height(30)))
                {
                    poser.ClearColliders();
                    EditorUtility.SetDirty(poser);
                }
            }
        }

        // Joint names for the override dropdown, gathered the same way the builder does.
        private static string[] GetJointNames(HandPhysicsColliders poser)
        {
            var anim = poser.GetComponent<HandAnimator>();
            if (anim == null) return null;
            if (anim.currentJoints == null || anim.currentJoints.Count == 0)
                anim.SetBones();

            var names = new System.Collections.Generic.List<string>();
            foreach (var j in anim.currentJoints)
                if (j) names.Add(j.name);
            return names.ToArray();
        }
    }
}
