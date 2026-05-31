using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [CustomEditor(typeof(HandColliderConfig))]
    public class HandColliderConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty globalRadius, globalHeight;
        private SerializedProperty addPalm, palmShape, palmRadius, palmBoxSize, palmOffset;
        private SerializedProperty fingerConfigs, jointOverrides;

        private bool fingerFoldout = true;
        private bool overrideFoldout = true;

        void OnEnable()
        {
            globalRadius  = serializedObject.FindProperty("globalRadiusMultiplier");
            globalHeight  = serializedObject.FindProperty("globalHeightMultiplier");
            addPalm       = serializedObject.FindProperty("addPalmCollider");
            palmShape     = serializedObject.FindProperty("palmShape");
            palmRadius    = serializedObject.FindProperty("palmRadius");
            palmBoxSize   = serializedObject.FindProperty("palmBoxSize");
            palmOffset    = serializedObject.FindProperty("palmOffset");
            fingerConfigs = serializedObject.FindProperty("fingerConfigs");
            jointOverrides = serializedObject.FindProperty("jointOverrides");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Global", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(globalRadius, new GUIContent("Radius Multiplier"));
            EditorGUILayout.PropertyField(globalHeight, new GUIContent("Height Multiplier"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Palm", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(addPalm, new GUIContent("Add Palm Collider"));
            if (addPalm.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(palmShape, new GUIContent("Shape"));
                if (palmShape.enumValueIndex == (int)PalmColliderShape.Box)
                    EditorGUILayout.PropertyField(palmBoxSize, new GUIContent("Box Size"));
                else
                    EditorGUILayout.PropertyField(palmRadius, new GUIContent("Radius"));
                EditorGUILayout.PropertyField(palmOffset, new GUIContent("Local Offset"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);
            fingerFoldout = EditorGUILayout.Foldout(fingerFoldout, "Per-Finger Multipliers", true, EditorStyles.foldoutHeader);
            if (fingerFoldout)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < fingerConfigs.arraySize; i++)
                {
                    var fc = fingerConfigs.GetArrayElementAtIndex(i);
                    var name = fc.FindPropertyRelative("fingerName").stringValue;
                    EditorGUILayout.LabelField(name, EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(fc.FindPropertyRelative("radiusMultiplier"), new GUIContent("Radius ×"));
                    EditorGUILayout.PropertyField(fc.FindPropertyRelative("heightMultiplier"), new GUIContent("Height ×"));
                    EditorGUI.indentLevel--;
                    if (i < fingerConfigs.arraySize - 1) EditorGUILayout.Space(2);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);
            overrideFoldout = EditorGUILayout.Foldout(overrideFoldout, "Per-Joint Overrides", true, EditorStyles.foldoutHeader);
            if (overrideFoldout)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < jointOverrides.arraySize; i++)
                {
                    var ov = jointOverrides.GetArrayElementAtIndex(i);
                    string jName = ov.FindPropertyRelative("jointName").stringValue;
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(jName) ? $"Override {i}" : jName, EditorStyles.miniBoldLabel);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(ov.FindPropertyRelative("jointName"), new GUIContent("Joint Name"));

                    var ovRadius = ov.FindPropertyRelative("overrideRadius");
                    EditorGUILayout.PropertyField(ovRadius, new GUIContent("Override Radius"));
                    if (ovRadius.boolValue)
                        EditorGUILayout.PropertyField(ov.FindPropertyRelative("radius"), new GUIContent("Radius"));

                    var ovHeight = ov.FindPropertyRelative("overrideHeight");
                    EditorGUILayout.PropertyField(ovHeight, new GUIContent("Override Height"));
                    if (ovHeight.boolValue)
                        EditorGUILayout.PropertyField(ov.FindPropertyRelative("height"), new GUIContent("Height"));

                    EditorGUILayout.PropertyField(ov.FindPropertyRelative("localOffset"), new GUIContent("Local Offset"));
                    EditorGUI.indentLevel--;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            jointOverrides.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                    EditorGUILayout.Space(4);
                }

                if (GUILayout.Button("+ Add Joint Override"))
                    jointOverrides.InsertArrayElementAtIndex(jointOverrides.arraySize);

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
