using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

using Pose = UnityEngine.Pose;

namespace MikeNspired.XRIStarterKit.Editor
{
    [CustomEditor(typeof(XRHandPoser))]
    public class XRHandPoserEditor : UnityEditor.Editor
    {
        private XRHandPoser handPoseScript;
        private SerializedProperty leftHandPose;
        private SerializedProperty rightHandPose;
        private SerializedProperty hasAnimationPose;
        private SerializedProperty LeftHandAnimationPose;
        private SerializedProperty RightHandAnimationPose;
        private SerializedProperty leftHandAttach;
        private SerializedProperty rightHandAttach;
        private SerializedProperty currentLeftHand;
        private SerializedProperty currentRightHand;
        private SerializedProperty interactable;

        // Dynamic posing config
        private SerializedProperty posePolicy;
        private SerializedProperty overrideGlobalSettings;
        private SerializedProperty positionThreshold;
        private SerializedProperty rotationThreshold;
        private SerializedProperty dynamicStepCount;
        private SerializedProperty dynamicProbeRadius;
        private SerializedProperty dynamicSamplesPerFinger;
        private SerializedProperty dynamicNoContactCurl;
        private SerializedProperty graspRequireThumb;
        private SerializedProperty graspRequiredFingers;
        private SerializedProperty failedGraspResponse;

        private bool hasLeftHand;
        private bool hasRightHand;
        private AnimBool customizeValues;
        private GUIStyle centerStyle;
        private GUIStyle poseTitleStyle;
        private float contextWidth;
        private int buttonWidth = 175;


        protected void OnEnable()
        {
            handPoseScript = (XRHandPoser) target;
            leftHandPose = serializedObject.FindProperty("leftHandPose");
            rightHandPose = serializedObject.FindProperty("rightHandPose");
            hasAnimationPose = serializedObject.FindProperty("hasAnimationPose");
            LeftHandAnimationPose = serializedObject.FindProperty("LeftHandAnimationPose");
            RightHandAnimationPose = serializedObject.FindProperty("RightHandAnimationPose");
            leftHandAttach = serializedObject.FindProperty("leftHandAttach");
            rightHandAttach = serializedObject.FindProperty("rightHandAttach");
            currentLeftHand = serializedObject.FindProperty("currentLeftHand");
            currentRightHand = serializedObject.FindProperty("currentRightHand");
            interactable = serializedObject.FindProperty("interactable");

            posePolicy = serializedObject.FindProperty("posePolicy");
            overrideGlobalSettings = serializedObject.FindProperty("overrideGlobalSettings");
            positionThreshold = serializedObject.FindProperty("positionThreshold");
            rotationThreshold = serializedObject.FindProperty("rotationThreshold");
            dynamicStepCount = serializedObject.FindProperty("dynamicStepCount");
            dynamicProbeRadius = serializedObject.FindProperty("dynamicProbeRadius");
            dynamicSamplesPerFinger = serializedObject.FindProperty("dynamicSamplesPerFinger");
            dynamicNoContactCurl = serializedObject.FindProperty("dynamicNoContactCurl");
            graspRequireThumb = serializedObject.FindProperty("graspRequireThumb");
            graspRequiredFingers = serializedObject.FindProperty("graspRequiredFingers");
            failedGraspResponse = serializedObject.FindProperty("failedGraspResponse");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            hasLeftHand = currentLeftHand.objectReferenceValue != null;
            hasRightHand = currentRightHand.objectReferenceValue != null;
            customizeValues = new AnimBool(true);
            customizeValues.valueChanged.AddListener(Repaint);

            centerStyle = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter};
            poseTitleStyle = new GUIStyle();
            poseTitleStyle.fontSize = 12;
            poseTitleStyle.fontStyle = FontStyle.Bold;
            poseTitleStyle.alignment = TextAnchor.MiddleCenter;

            contextWidth = (float) typeof(EditorGUIUtility).GetProperty("contextWidth", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);

            SaveAttachPointsCurrentPositions();
            CompareAttachPositions();

            GUILayout.Space(5f);
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            GUILayout.BeginVertical();
            DrawFields();
            GUILayout.EndVertical();


            GUILayout.Space(10);
            GUILayout.Label("Object Pose Setup", EditorStyles.boldLabel);
            Rect horizontalLine = EditorGUILayout.GetControlRect(GUILayout.Height(1f));
            horizontalLine.height = .5f;
            horizontalLine.y += 5;
            EditorGUI.DrawRect(horizontalLine, Color.gray);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            DrawPoseSection();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            DrawBottomButtons();
            DrawMessages();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawFields()
        {
            var labelToolTip = new GUIContent("XRBaseInteractable",
                "XRBaseInteractable script, Can be located anywhere. If null, will grab if one is on the object");
            interactable.objectReferenceValue = EditorGUILayout.ObjectField(labelToolTip, interactable.objectReferenceValue, typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable), true) as UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable;

            labelToolTip = new GUIContent("Animation Poses", "Adds second pose that animates to when trigger is pulled");
            hasAnimationPose.boolValue = EditorGUILayout.Toggle(labelToolTip, hasAnimationPose.boolValue);

            labelToolTip = new GUIContent("Maintain Hand On Object",
                "After the object is grabbed, the hand poser maintains the objects position every frame to lock the object in hand.");
            handPoseScript.MaintainHandOnObject = EditorGUILayout.Toggle(labelToolTip, handPoseScript.MaintainHandOnObject);
            
            if (handPoseScript.MaintainHandOnObject)
            {
                labelToolTip = new GUIContent("Wait Till Ease In Time To Maintain Position",
                    "Interactable's have an 'EaseIn' time, this waits till that time has elapsed before maintaining the hand position");
                handPoseScript.WaitTillEaseInTimeToMaintainPosition = EditorGUILayout.Toggle(labelToolTip, handPoseScript.WaitTillEaseInTimeToMaintainPosition);
            }
            
            labelToolTip = new GUIContent("Override Ease In Time",
                "Maintain Animation pose waits for XRGrabInteractables EaseInTime to start. Non XRGrabInteractables do not have this variable and can be added here");
            handPoseScript.overrideEaseTime = EditorGUILayout.Toggle(labelToolTip, handPoseScript.overrideEaseTime);
            
            if (handPoseScript.overrideEaseTime)
            {
                labelToolTip = new GUIContent("Ease In Time Override",
                    "Time till maintain pose starts");
                handPoseScript.easeInTimeOverride = EditorGUILayout.FloatField(labelToolTip, handPoseScript.easeInTimeOverride);
            }

            DrawDynamicPosingFields();
        }

        private void DrawDynamicPosingFields()
        {
            GUILayout.Space(8);
            GUILayout.Label("Dynamic Posing", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(posePolicy, new GUIContent("Pose Policy",
                "Auto: authored near the grip, dynamic fallback off-axis. AuthoredOnly: never solve " +
                "(e.g. a handgun). DynamicOnly: always solve (e.g. a cube). NoPosing: no hand posing."));

            EditorGUILayout.PropertyField(overrideGlobalSettings, new GUIContent("Override Global Settings",
                "When off, this object uses the global defaults on the HandPoserSettings asset. " +
                "Turn on to override the thresholds and grasp rule below for this object only."));

            if (overrideGlobalSettings.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(positionThreshold, new GUIContent("Position Threshold",
                    "How far (m) a grab can land from the authored grip before it counts as off-axis (DYNAMIC)."));
                EditorGUILayout.PropertyField(rotationThreshold, new GUIContent("Rotation Threshold",
                    "How far (deg) a grab can rotate from the authored grip before it counts as off-axis (DYNAMIC)."));
                EditorGUILayout.PropertyField(dynamicStepCount, new GUIContent("Step Count",
                    "Curl-sweep resolution: number of t steps per finger."));
                EditorGUILayout.PropertyField(dynamicProbeRadius, new GUIContent("Probe Radius",
                    "Fingertip probe sphere radius (m) used to detect contact during the sweep."));
                EditorGUILayout.PropertyField(dynamicSamplesPerFinger, new GUIContent("Samples Per Finger",
                    "Curl-sweep solver only. Joints from the fingertip inward to sphere-test each step. " +
                    "1 = tip only; 2+ catches a finger wrapping the object even when the tip slips past it."));
                EditorGUILayout.PropertyField(dynamicNoContactCurl, new GUIContent("No-Contact Curl",
                    "Progressive solver only. Curl for a finger that touches nothing — non-gripping " +
                    "fingers settle into a relaxed fist instead of opening. 1 = full fist; ~0.7 natural."));
                EditorGUILayout.PropertyField(graspRequireThumb, new GUIContent("Grasp Require Thumb",
                    "Require the thumb to make contact for a dynamic grasp to hold."));
                EditorGUILayout.PropertyField(graspRequiredFingers, new GUIContent("Grasp Required Fingers",
                    "Number of non-thumb fingers that must contact for a dynamic grasp to hold."));
                EditorGUILayout.PropertyField(failedGraspResponse, new GUIContent("Failed Grasp Response",
                    "What to do when a dynamic grasp fails its contact test."));
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Using global dynamic-pose defaults from the HandPoserSettings asset " +
                    "(Resources/HandPoserSettings). Enable Override Global Settings to tune this object.",
                    MessageType.None);
            }
        }
        private void DrawPoseSection()
        {

            DrawLeftPoseSection();

            DrawCenterLine();

            DrawRightPoseSection();

            GUILayout.BeginVertical();
            GUILayout.Label("   ", centerStyle);
            GUILayout.EndVertical();

        }

        private void DrawCenterLine()
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(8));
            int lineMulti = 8;
            if (hasAnimationPose.boolValue)
                lineMulti += 2;
            if (!hasRightHand && !hasLeftHand)
                lineMulti -= 4;

            r.height = 19.2f * lineMulti;
            r.y += 2;
            r.x += 4;
            r.width = 1;
            EditorGUI.DrawRect(r, Color.black);
        }

        private void DrawLeftPoseSection()
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Left Hand", poseTitleStyle);

            var labelToolTip = new GUIContent("Default Pose", "Hand will be animated to this pose when item is grabbed");
            GUILayout.Label(labelToolTip);
            leftHandPose.objectReferenceValue = EditorGUILayout.ObjectField(leftHandPose.objectReferenceValue, typeof(PoseScriptableObject), false);
            GUILayout.Space(2);

            if (hasAnimationPose.boolValue)
            {
                labelToolTip = new GUIContent("Animation Pose", "Hand animates to this based on trigger value");
                GUILayout.Label(labelToolTip);
                LeftHandAnimationPose.objectReferenceValue = EditorGUILayout.ObjectField(LeftHandAnimationPose.objectReferenceValue, typeof(PoseScriptableObject), false);
                GUILayout.Space(2);
            }
            
            customizeValues.value = !hasLeftHand;
            GUI.enabled = !hasLeftHand;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
            {
                labelToolTip = new GUIContent("Create Hand", "Create a temporary hand to animate/pose");
                if (GUILayout.Button(labelToolTip, GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.CreateLeftHand();
                    handPoseScript.SetLeftHandToPose();
                }
            }

            EditorGUILayout.EndFadeGroup();

            GUI.enabled = hasLeftHand;
            customizeValues.value = hasLeftHand;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
            {
                if (GUILayout.Button("Select Hand"))
                {
                    HandAnimator hand = currentLeftHand.objectReferenceValue as HandAnimator;
                    Selection.activeGameObject = hand.gameObject;
                }
            }

            EditorGUILayout.EndFadeGroup();

            customizeValues.value = hasLeftHand;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
            {
                GUI.enabled = leftHandPose.objectReferenceValue && hasLeftHand;
                if (GUILayout.Button("Show Pose", GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.SetLeftHandToPose();
                }

                GUI.enabled = LeftHandAnimationPose.objectReferenceValue && hasLeftHand;
                if (GUILayout.Button("Show AnimPose", GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.SetLeftHandToAnimationPose();
                }

                GUI.enabled = hasLeftHand && hasRightHand;
                if (GUILayout.Button("Mirror", GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.CopyLeftToRight();
                }

                GUI.enabled = hasLeftHand;
                if (GUILayout.Button("Remove Hand", GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.DestroyLeftHand();
                }
            }

            EditorGUILayout.EndFadeGroup();
            GUILayout.EndVertical();
        }

        private void DrawRightPoseSection()
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Right Hand", poseTitleStyle);

            GUI.enabled = true;

            var labelToolTip = new GUIContent("Default Pose", "Hand will be animated to this pose when item is grabbed");
            GUILayout.Label(labelToolTip);
            rightHandPose.objectReferenceValue = EditorGUILayout.ObjectField(rightHandPose.objectReferenceValue, typeof(PoseScriptableObject), false);
            GUILayout.Space(2);

            if (hasAnimationPose.boolValue)
            {
                GUILayout.Label(new GUIContent("Animation Pose", "Hand animates to this based on trigger value"));
                RightHandAnimationPose.objectReferenceValue = EditorGUILayout.ObjectField(RightHandAnimationPose.objectReferenceValue, typeof(PoseScriptableObject), false);
                GUILayout.Space(2);
            }
            
            customizeValues.value = !hasRightHand;
            GUI.enabled = !hasRightHand;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
            {
                labelToolTip = new GUIContent("Create Hand", "Create a temporary hand to animate/pose");
                if (GUILayout.Button(labelToolTip, GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.CreateRightHand();
                    handPoseScript.SetRightHandToPose();

                }
            }

            EditorGUILayout.EndFadeGroup();
            GUI.enabled = hasRightHand;

            customizeValues.value = hasRightHand;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
            {
                if (GUILayout.Button("Select Hand"))
                {
                    HandAnimator hand = currentRightHand.objectReferenceValue as HandAnimator;
                    Selection.activeGameObject = hand.gameObject;
                }
            }

            EditorGUILayout.EndFadeGroup();

            customizeValues.value = hasRightHand;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
            {
                GUI.enabled = (rightHandPose.objectReferenceValue != null && hasRightHand);
                if (GUILayout.Button("Show Pose", GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.SetRightHandToPose();
                }

                GUI.enabled = (RightHandAnimationPose.objectReferenceValue != null && hasRightHand);

                if (GUILayout.Button("Show AnimPose", GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.SetRightHandToAnimationPose();
                }

                GUI.enabled = hasLeftHand && hasRightHand;
                if (GUILayout.Button("Mirror", GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.CopyRightToLeft();
                }

                GUI.enabled = hasRightHand;
                if (GUILayout.Button("Remove Hand", GUILayout.MinWidth(buttonWidth)))
                {
                    handPoseScript.DestroyRightHand();
                }
            }

            EditorGUILayout.EndFadeGroup();
            GUILayout.EndVertical();
        }

        private void DrawBottomButtons()
        {
            customizeValues.value = hasRightHand || hasLeftHand;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
            {
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical(GUILayout.MinWidth(contextWidth / 3));
                GUILayout.Label("   ", centerStyle);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();

                GUI.enabled = rightHandMoved || leftHandMoved;
                if (GUILayout.Button("SaveAttachPoints"))
                {
                    handPoseScript.SaveAttachPoints();
                    SaveAttachPointsCurrentPositions();
                    rightHandMoved = false;
                }

                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUILayout.MinWidth(contextWidth / 3));
                GUILayout.Label("   ", centerStyle);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndFadeGroup();
        }

        private void DrawMessages()
        {
            GUI.enabled = true;

            customizeValues.value = leftHandMoved;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
                EditorGUILayout.HelpBox("Left Hand has moved, click SaveAttachPoints", MessageType.Warning);
            EditorGUILayout.EndFadeGroup();

            customizeValues.value = rightHandMoved;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
                EditorGUILayout.HelpBox("Right Hand has moved, click SaveAttachPoints", MessageType.Warning);
            EditorGUILayout.EndFadeGroup();
        }

        private bool leftHandMoved;
        private bool rightHandMoved;
        private TransformStruct originalLeftAttachTransform;
        private TransformStruct originalRightHandAttachTransform;


        private void CompareAttachPositions()
        {
            if (leftHandAttach.objectReferenceValue == null) return;

            var left = (currentLeftHand.objectReferenceValue as HandAnimator)?.transform;
            var right = (currentRightHand.objectReferenceValue as HandAnimator)?.transform;

            if (left != null && (originalLeftAttachTransform.position != left.position || originalLeftAttachTransform.rotation != left.transform.rotation))
                leftHandMoved = true;
            else
                leftHandMoved = false;

            if (right != null && (originalRightHandAttachTransform.position != right.position || originalRightHandAttachTransform.rotation != right.transform.rotation))
                rightHandMoved = true;
            else
                rightHandMoved = false;
        }

        private void SaveAttachPointsCurrentPositions()
        {
            var left = leftHandAttach.objectReferenceValue as Transform;
            if (left)
                originalLeftAttachTransform.SetTransformStruct(left.position, left.rotation, left.localScale);

            var right = rightHandAttach.objectReferenceValue as Transform;
            if (right)
                originalRightHandAttachTransform.SetTransformStruct(right.position, right.rotation, right.localScale);
        }
    }
}