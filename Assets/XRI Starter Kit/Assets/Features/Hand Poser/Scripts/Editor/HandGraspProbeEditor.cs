using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit.Editor
{
    /// <summary>
    /// Inspector for <see cref="HandGraspProbe"/>. Groups the settings into collapsible foldouts and GREYS
    /// OUT (rather than hides) fields that don't apply in the current config, so toggling a bool never
    /// reflows the layout. The whole Grip Hold Pose group hides only on the deliberate Mode switch to
    /// AutoGraspTest. Presentation only — no behavior or serialization change. (Field [Header]s were removed
    /// from the component; all section titles are drawn here so they appear exactly once.)
    /// </summary>
    [CustomEditor(typeof(HandGraspProbe))]
    public class HandGraspProbeEditor : UnityEditor.Editor
    {
        private SerializedProperty handAnimator, enableProbe, mode;
        private SerializedProperty controllerButtons, gripThreshold, latchGripHold;
        private SerializedProperty stickHandToSurface, leashWeight, breakDistance, onHandSnapBack;
        private SerializedProperty requireRealGrasp, minGraspFingers, minWrapAngle;
        private SerializedProperty worldMask, probeCenter, reachRadius, maxColliders, ignoreColliderRoot;
        private SerializedProperty proximityEaseIn, fullPoseDistance, rotationDeadband, positionDeadband,
            smoothing, candidateStickiness, releaseFadeTime, returnToIdleWhenClear;
        private SerializedProperty overrideSolverSettings, solverSettings;
        private SerializedProperty onGraspAcquired;

        private bool gripHoldFoldout = true;
        private bool worldQueryFoldout = true;
        private bool feelFoldout;
        private bool solverFoldout;
        private bool eventsFoldout;

        private void OnEnable()
        {
            handAnimator       = serializedObject.FindProperty("handAnimator");
            enableProbe        = serializedObject.FindProperty("enableProbe");
            mode               = serializedObject.FindProperty("mode");
            controllerButtons  = serializedObject.FindProperty("controllerButtons");
            gripThreshold      = serializedObject.FindProperty("gripThreshold");
            latchGripHold      = serializedObject.FindProperty("latchGripHold");
            stickHandToSurface = serializedObject.FindProperty("stickHandToSurface");
            leashWeight        = serializedObject.FindProperty("leashWeight");
            breakDistance      = serializedObject.FindProperty("breakDistance");
            onHandSnapBack     = serializedObject.FindProperty("onHandSnapBack");
            requireRealGrasp   = serializedObject.FindProperty("requireRealGrasp");
            minGraspFingers    = serializedObject.FindProperty("minGraspFingers");
            minWrapAngle       = serializedObject.FindProperty("minWrapAngle");
            worldMask          = serializedObject.FindProperty("worldMask");
            probeCenter        = serializedObject.FindProperty("probeCenter");
            reachRadius        = serializedObject.FindProperty("reachRadius");
            maxColliders       = serializedObject.FindProperty("maxColliders");
            ignoreColliderRoot = serializedObject.FindProperty("ignoreColliderRoot");
            proximityEaseIn    = serializedObject.FindProperty("proximityEaseIn");
            fullPoseDistance   = serializedObject.FindProperty("fullPoseDistance");
            rotationDeadband   = serializedObject.FindProperty("rotationDeadband");
            positionDeadband   = serializedObject.FindProperty("positionDeadband");
            smoothing          = serializedObject.FindProperty("smoothing");
            candidateStickiness = serializedObject.FindProperty("candidateStickiness");
            releaseFadeTime    = serializedObject.FindProperty("releaseFadeTime");
            returnToIdleWhenClear = serializedObject.FindProperty("returnToIdleWhenClear");
            overrideSolverSettings = serializedObject.FindProperty("overrideSolverSettings");
            solverSettings     = serializedObject.FindProperty("solverSettings");
            onGraspAcquired    = serializedObject.FindProperty("onGraspAcquired");
        }

        public override void OnInspectorGUI()
        {
            DynamicPoseInfoBox.Draw(
                "Optional. While the hand is empty, curls the fingers onto nearby NON-grabbable world " +
                "geometry (e.g. press the open hand on a table). Off by default. AutoGraspTest = editor-only " +
                "dial-in tool; GripHoldPose = runtime, solves while the grip is held near a surface.");

            serializedObject.Update();

            EditorGUILayout.PropertyField(handAnimator);
            EditorGUILayout.PropertyField(enableProbe);
            EditorGUILayout.PropertyField(mode);

            // Grip Hold Pose: one dropdown for all of it. Hidden only on the deliberate switch to
            // AutoGraspTest (the dial-in tool uses none of these). Inside, dead sub-fields grey out so a
            // bool toggle never reflows the layout.
            if ((GraspProbeMode)mode.enumValueIndex == GraspProbeMode.GripHoldPose)
            {
                gripHoldFoldout = Foldout(gripHoldFoldout, "Grip Hold Pose");
                if (gripHoldFoldout)
                {
                    EditorGUI.indentLevel++;

                    SubHeader("Grip");
                    EditorGUILayout.PropertyField(controllerButtons);
                    EditorGUILayout.PropertyField(gripThreshold);
                    using (new EditorGUI.DisabledScope(stickHandToSurface.boolValue)) // stick overrides latch
                        EditorGUILayout.PropertyField(latchGripHold);

                    SubHeader("Surface Stick");
                    EditorGUILayout.PropertyField(stickHandToSurface);
                    using (new EditorGUI.DisabledScope(!stickHandToSurface.boolValue))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(leashWeight);
                        EditorGUILayout.PropertyField(breakDistance);
                        EditorGUILayout.PropertyField(onHandSnapBack);
                        EditorGUI.indentLevel--;
                    }

                    SubHeader("Grasp Validity");
                    EditorGUILayout.PropertyField(requireRealGrasp);
                    using (new EditorGUI.DisabledScope(!requireRealGrasp.boolValue))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(minGraspFingers);
                        EditorGUILayout.PropertyField(minWrapAngle);
                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }
            }

            worldQueryFoldout = Foldout(worldQueryFoldout, "World Query");
            if (worldQueryFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(worldMask);
                EditorGUILayout.PropertyField(probeCenter);
                EditorGUILayout.PropertyField(reachRadius);
                EditorGUILayout.PropertyField(maxColliders);
                EditorGUILayout.PropertyField(ignoreColliderRoot);
                EditorGUI.indentLevel--;
            }

            feelFoldout = Foldout(feelFoldout, "Feel");
            if (feelFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(proximityEaseIn);
                using (new EditorGUI.DisabledScope(!proximityEaseIn.boolValue)) // Full Pose Distance is ease-in only
                    EditorGUILayout.PropertyField(fullPoseDistance);
                EditorGUILayout.PropertyField(rotationDeadband);
                EditorGUILayout.PropertyField(positionDeadband);
                EditorGUILayout.PropertyField(smoothing);
                EditorGUILayout.PropertyField(candidateStickiness);
                EditorGUILayout.PropertyField(releaseFadeTime);
                EditorGUILayout.PropertyField(returnToIdleWhenClear);
                EditorGUI.indentLevel--;
            }

            solverFoldout = Foldout(solverFoldout, "Solver");
            if (solverFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(overrideSolverSettings);
                using (new EditorGUI.DisabledScope(!overrideSolverSettings.boolValue)) // dead when using the global asset
                    EditorGUILayout.PropertyField(solverSettings, true);
                EditorGUI.indentLevel--;
            }

            eventsFoldout = Foldout(eventsFoldout, "Events");
            if (eventsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(onGraspAcquired);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static bool Foldout(bool state, string title)
        {
            EditorGUILayout.Space(4);
            return EditorGUILayout.Foldout(state, title, true, EditorStyles.foldoutHeader);
        }

        private static void SubHeader(string title)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
        }
    }
}
