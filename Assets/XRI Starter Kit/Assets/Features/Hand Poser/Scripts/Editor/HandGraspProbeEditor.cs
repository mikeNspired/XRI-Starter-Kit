using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit.Editor
{
    /// <summary>
    /// Conditional inspector for <see cref="HandGraspProbe"/>: shows only the fields that actually do
    /// something in the current configuration. The GripHoldPose sections hide entirely in AutoGraspTest;
    /// sub-options hide behind their toggles (stick fields, grasp-validity fields, the solver override
    /// block); and World Query / Feel collapse into foldouts. Presentation only — no behavior change.
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

        private bool worldQueryFoldout = true;
        private bool feelFoldout;

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

            // The Grip / Surface Stick / Grasp Validity groups are GripHoldPose-only — hide them whole in
            // AutoGraspTest (the editor dial-in tool, which uses none of them).
            bool gripHold = (GraspProbeMode)mode.enumValueIndex == GraspProbeMode.GripHoldPose;
            if (gripHold)
            {
                Header("Grip (GripHoldPose mode)");
                EditorGUILayout.PropertyField(controllerButtons);
                EditorGUILayout.PropertyField(gripThreshold);
                // Surface Stick overrides the latch, so the latch toggle only matters when stick is off.
                if (!stickHandToSurface.boolValue)
                    EditorGUILayout.PropertyField(latchGripHold);

                Header("Surface Stick (GripHoldPose)");
                EditorGUILayout.PropertyField(stickHandToSurface);
                if (stickHandToSurface.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(leashWeight);
                    EditorGUILayout.PropertyField(breakDistance);
                    EditorGUILayout.PropertyField(onHandSnapBack);
                    EditorGUI.indentLevel--;
                }

                Header("Grasp Validity (GripHoldPose)");
                EditorGUILayout.PropertyField(requireRealGrasp);
                if (requireRealGrasp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(minGraspFingers);
                    EditorGUILayout.PropertyField(minWrapAngle);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(4);
            worldQueryFoldout = EditorGUILayout.Foldout(worldQueryFoldout, "World Query", true, EditorStyles.foldoutHeader);
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

            EditorGUILayout.Space(4);
            feelFoldout = EditorGUILayout.Foldout(feelFoldout, "Feel", true, EditorStyles.foldoutHeader);
            if (feelFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(proximityEaseIn);
                if (proximityEaseIn.boolValue) // Full Pose Distance is proximity-ease-in only
                    EditorGUILayout.PropertyField(fullPoseDistance);
                EditorGUILayout.PropertyField(rotationDeadband);
                EditorGUILayout.PropertyField(positionDeadband);
                EditorGUILayout.PropertyField(smoothing);
                EditorGUILayout.PropertyField(candidateStickiness);
                EditorGUILayout.PropertyField(releaseFadeTime);
                EditorGUILayout.PropertyField(returnToIdleWhenClear);
                EditorGUI.indentLevel--;
            }

            Header("Solver");
            EditorGUILayout.PropertyField(overrideSolverSettings);
            if (overrideSolverSettings.boolValue) // the block is dead (and big) when using the global asset
                EditorGUILayout.PropertyField(solverSettings, true);

            serializedObject.ApplyModifiedProperties();
        }

        private static void Header(string title)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }
}
