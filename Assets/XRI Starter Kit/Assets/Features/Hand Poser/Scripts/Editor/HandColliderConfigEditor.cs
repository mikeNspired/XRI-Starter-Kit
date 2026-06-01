using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [CustomEditor(typeof(HandColliderConfig))]
    public class HandColliderConfigEditor : UnityEditor.Editor
    {
        // Optional list of the live hand's joint names, set by HandPhysicsCollidersEditor
        // before this editor is drawn inline. When present, the Per-Joint Overrides section
        // uses dropdowns instead of free-text. Null when editing the asset standalone.
        public static string[] JointNamesContext;

        private SerializedProperty globalRadius, globalHeight;
        private SerializedProperty ignoreNames, fingertipNames, distalLength;
        private SerializedProperty addPalm, palmShape, palmRadius, palmBoxSize, palmOffset;
        private SerializedProperty fingerConfigs, jointOverrides;

        private bool jointsFoldout = true;
        private bool palmFoldout = true;
        private bool fingerFoldout = true;
        private bool overrideFoldout = true;

        void OnEnable()
        {
            globalRadius   = serializedObject.FindProperty("globalRadiusMultiplier");
            globalHeight   = serializedObject.FindProperty("globalHeightMultiplier");
            ignoreNames    = serializedObject.FindProperty("ignoreJointNameContains");
            fingertipNames = serializedObject.FindProperty("fingertipMarkerNameContains");
            distalLength   = serializedObject.FindProperty("distalLengthMultiplier");
            addPalm        = serializedObject.FindProperty("addPalmCollider");
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
            jointsFoldout = EditorGUILayout.Foldout(jointsFoldout, "Joints", true, EditorStyles.foldoutHeader);
            if (jointsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(ignoreNames, new GUIContent("Ignore Joint Name Contains"), true);
                EditorGUILayout.PropertyField(fingertipNames, new GUIContent("Fingertip Marker Name Contains"), true);
                EditorGUILayout.PropertyField(distalLength, new GUIContent("Distal Length ×"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);
            palmFoldout = EditorGUILayout.Foldout(palmFoldout, "Palm", true, EditorStyles.foldoutHeader);
            if (palmFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(addPalm, new GUIContent("Add Palm Collider"));
                if (addPalm.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(palmShape, new GUIContent("Shape"));
                    // Metric values are tiny (meters); sliders with a hand-sized range are far
                    // easier to fine-tune than typing into raw Vector3/float fields.
                    if (palmShape.enumValueIndex == (int)PalmColliderShape.Box)
                        Vector3Sliders(palmBoxSize, "Box Size (m)", 0.005f, 0.15f);
                    else
                        palmRadius.floatValue = EditorGUILayout.Slider("Radius (m)", palmRadius.floatValue, 0.005f, 0.1f);
                    Vector3Sliders(palmOffset, "Local Offset (m)", -0.1f, 0.1f);
                    EditorGUI.indentLevel--;
                }
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
                DrawJointOverrides();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawJointOverrides()
        {
            var jointNames = JointNamesContext;
            bool hasJointList = jointNames != null && jointNames.Length > 0;

            for (int i = 0; i < jointOverrides.arraySize; i++)
            {
                var ov = jointOverrides.GetArrayElementAtIndex(i);
                var jointNameProp = ov.FindPropertyRelative("jointName");
                string jName = jointNameProp.stringValue;
                EditorGUILayout.LabelField(string.IsNullOrEmpty(jName) ? $"Override {i}" : jName, EditorStyles.miniBoldLabel);

                EditorGUI.indentLevel++;
                if (hasJointList)
                    DrawJointNamePopup(jointNameProp, jointNames);
                else
                    EditorGUILayout.PropertyField(jointNameProp, new GUIContent("Joint Name"));

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

            if (hasJointList)
            {
                // Dropdown of joints that don't already have an override.
                var taken = new HashSet<string>();
                for (int i = 0; i < jointOverrides.arraySize; i++)
                    taken.Add(jointOverrides.GetArrayElementAtIndex(i).FindPropertyRelative("jointName").stringValue);

                var addable = new List<string> { "Add joint override…" };
                foreach (var n in jointNames)
                    if (!taken.Contains(n)) addable.Add(n);

                int sel = EditorGUILayout.Popup("Add Override", 0, addable.ToArray());
                if (sel > 0)
                {
                    jointOverrides.InsertArrayElementAtIndex(jointOverrides.arraySize);
                    jointOverrides.GetArrayElementAtIndex(jointOverrides.arraySize - 1)
                        .FindPropertyRelative("jointName").stringValue = addable[sel];
                }
            }
            else if (GUILayout.Button("+ Add Joint Override"))
            {
                jointOverrides.InsertArrayElementAtIndex(jointOverrides.arraySize);
            }
        }

        private static void DrawJointNamePopup(SerializedProperty jointNameProp, string[] jointNames)
        {
            int current = System.Array.IndexOf(jointNames, jointNameProp.stringValue);
            // Show the stored name even if it isn't in the current hand's joint list.
            if (current < 0)
            {
                var withCurrent = new List<string>(jointNames) { jointNameProp.stringValue + " (missing)" };
                int picked = EditorGUILayout.Popup("Joint", withCurrent.Count - 1, withCurrent.ToArray());
                if (picked >= 0 && picked < jointNames.Length) jointNameProp.stringValue = jointNames[picked];
            }
            else
            {
                int picked = EditorGUILayout.Popup("Joint", current, jointNames);
                if (picked >= 0) jointNameProp.stringValue = jointNames[picked];
            }
        }

        private static void Vector3Sliders(SerializedProperty prop, string label, float min, float max)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            var v = prop.vector3Value;
            v.x = EditorGUILayout.Slider("X", v.x, min, max);
            v.y = EditorGUILayout.Slider("Y", v.y, min, max);
            v.z = EditorGUILayout.Slider("Z", v.z, min, max);
            prop.vector3Value = v;
            EditorGUI.indentLevel--;
        }
    }
}
