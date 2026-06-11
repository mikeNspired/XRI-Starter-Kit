using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace MikeNspired.XRIStarterKit.Editor
{
    [CustomEditor(typeof(HandAnimator))]
    [CanEditMultipleObjects]
    public class HandAnimatorEditor : UnityEditor.Editor
    {
        private int buttonWidth = 100;
        private SerializedProperty originScript;

        private SerializedProperty handType;
        private SerializedProperty Point;
        private SerializedProperty drawHelperSpheres;
        private SerializedProperty defaultPose;
        private SerializedProperty animationPose;
        private SerializedProperty secondButtonPose;
        private SerializedProperty openPose;
        private SerializedProperty closedPose;
        private SerializedProperty closedPoses;
        private HandAnimator mainScript;

        private bool showReferencePoses;
        private bool showFingerSliders;
        private bool hasLeftHand;
        private bool hasRightHand;
        private AnimBool customizeValues;
        private GUIStyle poseTitleStyle;
        private HandPoser handPoserScript;

        protected void OnEnable()
        {
            mainScript = (HandAnimator)target;

            handType = serializedObject.FindProperty("handType");
            drawHelperSpheres = serializedObject.FindProperty("drawHelperSpheres");
            defaultPose = serializedObject.FindProperty("DefaultPose");
            animationPose = serializedObject.FindProperty("AnimationPose");
            secondButtonPose = serializedObject.FindProperty("SecondButtonPose");
            openPose = serializedObject.FindProperty("OpenPose");
            closedPose = serializedObject.FindProperty("ClosedPose");
            closedPoses = serializedObject.FindProperty("ClosedPoses");

            if (mainScript.RootBone == null)
            {
                mainScript.RootBone = mainScript.GetComponentInChildren<Pose>();
            }

            handPoserScript = mainScript.GetComponentInParent<HandPoser>();
        }

        public List<int> testList = new List<int>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SetVariables();
            DrawSettings();
            DrawCurrentPoses();
            DrawSetHandPoserAnimations();
            ReferencePoses();
            FingerSliders();
            serializedObject.ApplyModifiedProperties();
            DrawButtons();
            DrawMessages();
        }


        private void SetVariables()
        {
            customizeValues = new AnimBool(true);
            customizeValues.valueChanged.AddListener(Repaint);
            poseTitleStyle = new GUIStyle();
            poseTitleStyle.fontSize = 12;
            poseTitleStyle.fontStyle = FontStyle.Bold;
            poseTitleStyle.alignment = TextAnchor.MiddleCenter;
        }

        private void DrawSettings()
        {
            GUILayout.Space(5f);
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            serializedObject.Update();

            var labelToolTip = new GUIContent("Joint Debug Spheres", "Draws spheres to see joints");
            drawHelperSpheres.boolValue = EditorGUILayout.Toggle(labelToolTip, drawHelperSpheres.boolValue);


            GUILayout.BeginHorizontal();
            labelToolTip = new GUIContent("Hand Side", "Left or Right hand, used for determining which attachpoint needed");
            // GUILayout.Label("Hand Side");
            handType.enumValueIndex = EditorGUILayout.Popup(labelToolTip, handType.enumValueIndex, handType.enumDisplayNames);
            GUILayout.EndHorizontal();

            labelToolTip = new GUIContent("Root Bone", "Root bone of skeleton on hand");
            mainScript.RootBone = EditorGUILayout.ObjectField(labelToolTip, mainScript.RootBone, typeof(PoseScriptableObject), true) as Pose;

            labelToolTip = new GUIContent("Time To New Pose", "Time hand skeleton animates to next pose");
            mainScript.animationTimeToNewPose = EditorGUILayout.FloatField(labelToolTip, mainScript.animationTimeToNewPose);
            if (mainScript.animationTimeToNewPose <= 0)
                mainScript.animationTimeToNewPose = .01f;

            labelToolTip = new GUIContent("Move To Target Time", "Time to move hand to the item being grabbed");
            mainScript.handMoveToTargetAnimationTime = EditorGUILayout.FloatField(labelToolTip, mainScript.handMoveToTargetAnimationTime);
            if (mainScript.handMoveToTargetAnimationTime <= 0)
                mainScript.handMoveToTargetAnimationTime = .01f;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCurrentPoses()
        {
            serializedObject.Update();

            GUILayout.Space(5f);
            GUILayout.Label("Active Poses", EditorStyles.boldLabel);
            DrawLine();
            GUILayout.Space(5f);

            GUILayout.BeginHorizontal();
            var labelToolTip = new GUIContent("Default Pose", "Pose the hand will be in when no buttons are being pressed");
            EditorGUILayout.PropertyField(defaultPose, labelToolTip);
            if (GUILayout.Button("Animate", GUILayout.MaxWidth(buttonWidth))) mainScript.AnimateToCurrent();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            labelToolTip = new GUIContent("Animation Pose", "Animate to this pose  from Default Pose when pulling the trigger from values 0 to 1");
            EditorGUILayout.PropertyField(animationPose, labelToolTip);
            if (GUILayout.Button("Animate", GUILayout.MaxWidth(buttonWidth))) mainScript.AnimateInstantly(mainScript.AnimationPose);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            labelToolTip = new GUIContent("Second Button Pose", "Animation used when not holding an item and pulling the grip button");
            EditorGUILayout.PropertyField(secondButtonPose, labelToolTip);
            if (GUILayout.Button("Animate", GUILayout.MaxWidth(buttonWidth))) mainScript.AnimateInstantly(mainScript.SecondButtonPose);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            labelToolTip = new GUIContent("Open Pose", "Fully-open/splayed pose used as the open end (t=0) of the procedural per-finger curl sweep. Distinct from Default Pose; falls back to Default Pose if unassigned.");
            EditorGUILayout.PropertyField(openPose, labelToolTip);
            if (GUILayout.Button("Animate", GUILayout.MaxWidth(buttonWidth))) mainScript.AnimateInstantly(mainScript.OpenPose);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            labelToolTip = new GUIContent("Closed Pose", "Fist/grip pose used as the closed end (t=1) of the procedural per-finger curl sweep");
            EditorGUILayout.PropertyField(closedPose, labelToolTip);
            if (GUILayout.Button("Animate", GUILayout.MaxWidth(buttonWidth))) mainScript.AnimateInstantly(mainScript.ClosedPose);
            GUILayout.EndHorizontal();

            labelToolTip = new GUIContent("Extra Closed Poses",
                "Optional additional closed shapes (e.g. a precision/pinch pose). The dynamic solver sweeps " +
                "each finger against Closed Pose plus these, keeping the one whose fingertip ends closest to " +
                "the object. Leave empty to use only Closed Pose.");
            EditorGUILayout.PropertyField(closedPoses, labelToolTip, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSetHandPoserAnimations()
        {
            GUILayout.Space(5f);

            customizeValues.value = handPoserScript != null;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Set Poses in Hand Poser");


                if (GUILayout.Button("Set Default Pose", GUILayout.MinWidth(buttonWidth)))
                {
                    if (handType.enumValueIndex == 0)
                        handPoserScript.leftHandPose = mainScript.DefaultPose;
                    else
                        handPoserScript.rightHandPose = mainScript.DefaultPose;
                }

                if (GUILayout.Button("Set Animation Pose", GUILayout.MinWidth(buttonWidth)))
                {
                    if (handType.enumValueIndex == 0)
                        handPoserScript.LeftHandAnimationPose = mainScript.AnimationPose;
                    else
                        handPoserScript.RightHandAnimationPose = mainScript.AnimationPose;
                }

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndFadeGroup();
        }


        private void ReferencePoses()
        {
            GUILayout.Space(5f);
            GUILayout.Label("Reference Poses", EditorStyles.boldLabel);
            DrawLine();
            GUILayout.Space(5f);
            PoseScriptableObject[] referencePoses = HandPoserSettings.Instance.ReferencePoses.ToArray();
            var labelToolTip = new GUIContent("Show Reference Poses", "Use these as starting points for poses, you can add your own through Resourses/HandPoserSettings");
            showReferencePoses = EditorGUILayout.Toggle(labelToolTip, showReferencePoses);
            customizeValues.value = showReferencePoses;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))

                foreach (var index in referencePoses)
                {
                    if (index == null) continue;
                    GUILayout.BeginHorizontal();
                    labelToolTip = new GUIContent(index.name.First().ToString().ToUpper() + index.name.Substring(1));
                    EditorGUILayout.ObjectField(labelToolTip, index, typeof(PoseScriptableObject), false);
                    if (GUILayout.Button("Animate"))
                    {
                        mainScript.DefaultPose = index;
                        mainScript.AnimateInstantly(index);
                    }

                    GUILayout.EndHorizontal();
                }

            EditorGUILayout.EndFadeGroup();
        }

        private void DrawButtons()
        {
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
    
            customizeValues.value = handPoserScript != null;

            bool fadeValue = EditorGUILayout.BeginFadeGroup(customizeValues.faded);

            if (fadeValue)
            {
                if (GUILayout.Button("Return To Poser", GUILayout.MinWidth(buttonWidth)))
                {
                    Selection.activeGameObject = handPoserScript.gameObject;
                }
            }

            EditorGUILayout.EndFadeGroup();

            GUI.enabled = mainScript.RootBone;
            var labelToolTip = new GUIContent("Save Pose", "Save current hand position as a new pose");

            if (GUILayout.Button(labelToolTip, GUILayout.MinWidth(buttonWidth)))
                SavePose();

            GUILayout.EndHorizontal();

            GUI.enabled = mainScript.RootBone;
            var tipTip = new GUIContent("Create Fingertip Probes",
                "Generates a solver-only probe transform just past each finger's last joint (named *_Ignore " +
                "so poses skip it). Needed on skeletons with no tip joint. Nudge each onto the fingertip pad, " +
                "then save the prefab.");
            if (GUILayout.Button(tipTip, GUILayout.MinWidth(buttonWidth)))
                CreateFingertipProbes();
            GUI.enabled = true;
        }

        // Generates (or reuses) a fingertip probe per finger and assigns the HandAnimator fields. Idempotent:
        // an already-assigned field or an existing "*Ignore" child is kept rather than duplicated.
        private void CreateFingertipProbes()
        {
            if (!mainScript.RootBone)
            {
                Debug.LogWarning("[HandAnimator] Assign a RootBone before creating fingertip probes.", mainScript);
                return;
            }

            mainScript.SetBones(); // refresh finger chains so the extrapolation uses the current skeleton

            mainScript.thumbTipProbe  = EnsureTipProbe(mainScript.fingerMap.thumb,  "Thumb",  mainScript.thumbTipProbe);
            mainScript.indexTipProbe  = EnsureTipProbe(mainScript.fingerMap.index,  "Index",  mainScript.indexTipProbe);
            mainScript.middleTipProbe = EnsureTipProbe(mainScript.fingerMap.middle, "Middle", mainScript.middleTipProbe);
            mainScript.ringTipProbe   = EnsureTipProbe(mainScript.fingerMap.ring,   "Ring",   mainScript.ringTipProbe);
            mainScript.pinkyTipProbe  = EnsureTipProbe(mainScript.fingerMap.pinky,  "Pinky",  mainScript.pinkyTipProbe);

            EditorUtility.SetDirty(mainScript);
            mainScript.SetBones(); // re-resolve fingerMap.tips from the freshly-assigned probes
            Debug.Log("[HandAnimator] Fingertip probes ready. Nudge each onto the fingertip pad and save the prefab.", mainScript);
        }

        private Transform EnsureTipProbe(List<Transform> chain, string fingerName, Transform existingField)
        {
            if (existingField) return existingField; // already wired — leave it
            if (chain == null || chain.Count == 0) return null;

            var found = HandAnimator.FindIgnoredTipChild(chain[chain.Count - 1]);
            if (found) return found;

            var probe = HandAnimator.CreateTipProbe(chain, fingerName + "_TipProbe");
            if (!probe)
            {
                Debug.LogWarning($"[HandAnimator] {fingerName}: finger chain too short to place a fingertip probe.", mainScript);
                return null;
            }
            Undo.RegisterCreatedObjectUndo(probe.gameObject, "Create Fingertip Probe");
            return probe;
        }


        private void DrawMessages()
        {
            GUI.enabled = true;

            customizeValues.value = mainScript.RootBone == null;
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
                EditorGUILayout.HelpBox("Add 'Pose' script to root bone", MessageType.Warning);
            EditorGUILayout.EndFadeGroup();
        }
        
        private void SavePose()
        {
            if (mainScript.RootBone == null)
            {
                Debug.LogWarning("RootBone is not assigned. Cannot save pose.");
                return;
            }

            // Open a save file dialog to get the path for the new PoseScriptableObject
            string path = EditorUtility.SaveFilePanelInProject(
                "Save New Pose",
                "NewPose",
                "asset",
                "Specify where to save the new PoseScriptableObject"
            );

            if (string.IsNullOrEmpty(path)) return; // Exit if user cancels the save dialog

            // Create a new PoseScriptableObject
            PoseScriptableObject newPose = CreateInstance<PoseScriptableObject>();

            // Collect joint data from the RootBone hierarchy
            List<PoseScriptableObject.JointData> jointDataList = new List<PoseScriptableObject.JointData>();
            GatherJointData(mainScript.RootBone.transform, jointDataList);
            

            // Assign the joint data to the PoseScriptableObject
            newPose.joints = jointDataList.ToArray();

            // Save the PoseScriptableObject as an asset
            AssetDatabase.CreateAsset(newPose, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Pose saved successfully at: {path}");
        }

        private void GatherJointData(Transform root, List<PoseScriptableObject.JointData> jointDataList)
        {
            foreach (Transform joint in root.GetComponentsInChildren<Transform>())
            {
                if(JointUtility.ShouldSkipTransform(joint)) continue;
                jointDataList.Add(new PoseScriptableObject.JointData
                {
                    jointName = joint.name,
                    localPosition = joint.localPosition,
                    localRotation = joint.localRotation
                });
            }
        }
        private void DrawLine()
        {
            Rect horizontalLine = EditorGUILayout.GetControlRect(GUILayout.Height(1f));
            horizontalLine.height = 1f;
            EditorGUI.DrawRect(horizontalLine, Color.black);
        }

        private float indexValue, middleValue, ringValue, pinkyValue, thumbValue;

        private void FingerSliders()
        {
            GUILayout.Space(5f);
            GUILayout.Label("Finger Sliders", EditorStyles.boldLabel);
            DrawLine();
            GUILayout.Space(5f);

            var labelToolTip = new GUIContent("Show Joint Sliders", "Use these sliders to help start a new pose");
            showFingerSliders = EditorGUILayout.Toggle(labelToolTip, showFingerSliders);
            customizeValues.value = showFingerSliders;
           
            EditorGUI.BeginChangeCheck();
            
            if (EditorGUILayout.BeginFadeGroup(customizeValues.faded))
            {
                if (mainScript.defaultPose && mainScript.goalPose)
                {
                    if (mainScript.indexTopTransform)
                        SetSlider("Index Finger", ref indexValue, mainScript.indexTopTransform);
                    if (mainScript.middleTopTransform)
                        SetSlider("Middle Finger", ref middleValue, mainScript.middleTopTransform);
                    if (mainScript.ringTopTransform)
                        SetSlider("Ring Finger", ref ringValue, mainScript.ringTopTransform);
                    if (mainScript.pinkyTopTransform)
                        SetSlider("Pinky Finger", ref pinkyValue, mainScript.pinkyTopTransform);
                    if (mainScript.thumbTopTransform)
                        SetSlider("Thumb Finger", ref thumbValue, mainScript.thumbTopTransform);
                }

                RequiredFields();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(mainScript);
            }

            GUILayout.Space(5f);
            DrawLine();
            GUILayout.Space(5f);

            EditorGUILayout.EndFadeGroup();

            void SetSlider(string label, ref float value, Transform transform)
            {
                GUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.Slider(label, value, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                    mainScript.SetPoseByValue(transform, mainScript.defaultPose, mainScript.goalPose, value);
                GUILayout.EndHorizontal();
            }

            void RequiredFields()
            {
                EditorGUI.BeginChangeCheck();

                GUILayout.BeginHorizontal();
                labelToolTip = new GUIContent("Default Pose", "Pose the hand will be in when no buttons are being pressed");
                mainScript.defaultPose = EditorGUILayout.ObjectField(labelToolTip, mainScript.defaultPose, typeof(PoseScriptableObject), true) as PoseScriptableObject;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                labelToolTip = new GUIContent("Goal Pose", "Pose the hand will be in when no buttons are being pressed");
                mainScript.goalPose = EditorGUILayout.ObjectField(labelToolTip, mainScript.goalPose, typeof(PoseScriptableObject), true) as PoseScriptableObject;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                labelToolTip = new GUIContent("Index Finger Parent Transform", "Root bone of skeleton on hand");
                mainScript.indexTopTransform = EditorGUILayout.ObjectField(labelToolTip, mainScript.indexTopTransform, typeof(Transform), true) as Transform;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                labelToolTip = new GUIContent("Middle Finger Parent Transform", "Root bone of skeleton on hand");
                mainScript.middleTopTransform = EditorGUILayout.ObjectField(labelToolTip, mainScript.middleTopTransform, typeof(Transform), true) as Transform;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                labelToolTip = new GUIContent("Ring Finger Parent Transform", "Root bone of skeleton on hand");
                mainScript.ringTopTransform = EditorGUILayout.ObjectField(labelToolTip, mainScript.ringTopTransform, typeof(Transform), true) as Transform;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                labelToolTip = new GUIContent("Pinky Finger Parent Transform", "Root bone of skeleton on hand");
                mainScript.pinkyTopTransform = EditorGUILayout.ObjectField(labelToolTip, mainScript.pinkyTopTransform, typeof(Transform), true) as Transform;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                labelToolTip = new GUIContent("Thumb Finger Parent Transform", "Root bone of skeleton on hand");
                mainScript.thumbTopTransform = EditorGUILayout.ObjectField(labelToolTip, mainScript.thumbTopTransform, typeof(Transform), true) as Transform;
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(mainScript);
            }
        }
    }
}