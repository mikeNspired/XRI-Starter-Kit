using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
#if (UNITY_EDITOR) 

namespace MikeNspired.UnityXRHandPoser.Editor
{
    [CustomEditor(typeof(DebugPlayerMove))]
    [CanEditMultipleObjects]
    public class DebugPlayerMoveEditor : UnityEditor.Editor
    {
        private SerializedProperty origin;
        private SerializedProperty leftController;
        private SerializedProperty rightController;
        private DebugPlayerMove mainScript;

        protected void OnEnable()
        {
            mainScript = (DebugPlayerMove)target;
            origin = serializedObject.FindProperty("xrOrigin");
            leftController = serializedObject.FindProperty("leftController");
            rightController = serializedObject.FindProperty("rightController");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawCurrentPoses();
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawCurrentPoses()
        {
            serializedObject.Update();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Move Left")) 
                mainScript.Move((leftController.objectReferenceValue as ActionBasedController)?.transform);
            if (GUILayout.Button("Move Right")) 
                mainScript.Move((rightController.objectReferenceValue as ActionBasedController)?.transform);
            if (GUILayout.Button("Move Player")) 
                mainScript.Move((origin.objectReferenceValue as XROrigin)?.transform);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Left")) 
                DebugPlayerMove.Select((leftController.objectReferenceValue as ActionBasedController)?.gameObject);
            if (GUILayout.Button("Select Right")) 
                DebugPlayerMove.Select((rightController.objectReferenceValue as ActionBasedController)?.gameObject);
            if (GUILayout.Button("Select Player")) 
                DebugPlayerMove.Select((origin.objectReferenceValue as XROrigin)?.gameObject);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Disable Controller Tracking")) 
                mainScript.EnableControllerTracking(false);
            if (GUILayout.Button("Enable Controller Tracking")) 
                mainScript.EnableControllerTracking(true);
            GUILayout.EndHorizontal();
        }
    }
}
#endif