// Author MikeNspired.
using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [CustomEditor(typeof(HandPhysicsColliders))]
    public class HandPhysicsCollidersEditor : UnityEditor.Editor
    {
        private SerializedProperty configProp, colliderLayerProp, drawGizmosProp;
        private UnityEditor.Editor configEditor;
        private HandColliderConfig cachedConfig;

        void OnEnable()
        {
            configProp = serializedObject.FindProperty("config");
            colliderLayerProp = serializedObject.FindProperty("colliderLayer");
            drawGizmosProp = serializedObject.FindProperty("drawGizmos");
        }

        void OnDisable()
        {
            if (configEditor) DestroyImmediate(configEditor);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Builds physical-presence colliders on the hand bones. Keep this component on the hand: " +
                "at runtime it disables the colliders while grabbing and re-enables them on release. " +
                "This is a separate track from the Hand Poser / Dynamic Posing — it is not wired into them.",
                MessageType.None);

            serializedObject.Update();
            EditorGUILayout.PropertyField(drawGizmosProp, new GUIContent("Draw Gizmos",
                "Show the built colliders in the Scene view while the hand is selected. Turn off to " +
                "stop them cluttering the view once they're dialed in."));
            EditorGUILayout.PropertyField(configProp, new GUIContent("Config"));
            colliderLayerProp.intValue = EditorGUILayout.LayerField("Collider Layer", colliderLayerProp.intValue);

            if (colliderLayerProp.intValue == 0)
                EditorGUILayout.HelpBox(
                    "Collider Layer is 'Default'. Put the finger colliders on a dedicated layer and " +
                    "exclude it from your interactor / distance-grabber masks, or the fingers will block grabs.",
                    MessageType.Warning);

            serializedObject.ApplyModifiedProperties();

            var poser = (HandPhysicsColliders)target;
            var config = configProp.objectReferenceValue as HandColliderConfig;

            if (config == null)
            {
                EditorGUILayout.HelpBox("Assign a Hand Collider Config to tune and build colliders.", MessageType.Info);
                return;
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

            // (Re)create the embedded config editor when the assigned asset changes.
            if (configEditor == null || cachedConfig != config)
            {
                if (configEditor) DestroyImmediate(configEditor);
                configEditor = CreateEditor(config);
                cachedConfig = config;
            }

            EditorGUILayout.Space(8);
            var line = EditorGUILayout.GetControlRect(GUILayout.Height(1f));
            EditorGUI.DrawRect(line, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.HelpBox(
                $"Settings from '{config.name}'. Edits here rebuild the colliders instantly in edit mode.",
                MessageType.None);

            // Inline config edits rebuild colliders live in edit mode.
            HandColliderConfigEditor.JointNamesContext = GetJointNames(poser);
            EditorGUI.BeginChangeCheck();
            configEditor.OnInspectorGUI();
            HandColliderConfigEditor.JointNamesContext = null;
            if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
            {
                poser.BuildColliders();
                EditorUtility.SetDirty(poser);
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
