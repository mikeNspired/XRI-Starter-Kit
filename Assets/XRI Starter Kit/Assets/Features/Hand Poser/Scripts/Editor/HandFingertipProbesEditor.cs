using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit.Editor
{
    [CustomEditor(typeof(HandFingertipProbes))]
    public class HandFingertipProbesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var probes = (HandFingertipProbes)target;
            GUILayout.Space(6f);

            var label = new GUIContent("Create / Refresh Probes",
                "Creates a *_TipProbe_Ignore transform just past each finger's last joint (reusing valid " +
                "existing ones) and fills the fields above. Nudge each probe onto the fingertip pad, then " +
                "save the prefab.");
            if (GUILayout.Button(label))
            {
                Undo.RecordObject(probes, "Create Fingertip Probes");
                probes.Resolve(createMissing: true,
                               onCreated: go => Undo.RegisterCreatedObjectUndo(go, "Create Fingertip Probe"));
                EditorUtility.SetDirty(probes);
                Debug.Log("[HandFingertipProbes] Probes ready. Nudge each onto the fingertip pad and save the prefab.", probes);
            }
        }
    }
}
