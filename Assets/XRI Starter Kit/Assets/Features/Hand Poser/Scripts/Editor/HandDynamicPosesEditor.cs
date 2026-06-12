using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit.Editor
{
    [CustomEditor(typeof(HandDynamicPoses))]
    public class HandDynamicPosesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var dyn = (HandDynamicPoses)target;
            var hand = dyn.GetComponent<HandAnimator>();

            GUILayout.Space(6f);

            // Preview the solver's open/closed reference poses on the live hand (Play or Edit mode).
            using (new EditorGUI.DisabledScope(!hand))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Preview Open", "Pose the hand at the Open reference pose.")))
                    if (dyn.OpenOrDefault) hand.AnimateInstantly(dyn.OpenOrDefault);
                if (GUILayout.Button(new GUIContent("Preview Closed", "Pose the hand at the Closed (fist) reference pose.")))
                    if (dyn.ClosedPose) hand.AnimateInstantly(dyn.ClosedPose);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4f);

            var label = new GUIContent("Create / Refresh Probes",
                "Creates a *_TipProbe_Ignore transform just past each finger's last joint (reusing valid " +
                "existing ones) and fills the probe fields above. Nudge each probe onto the fingertip pad, " +
                "then save the prefab.");
            if (GUILayout.Button(label))
            {
                Undo.RecordObject(dyn, "Create Fingertip Probes");
                dyn.Resolve(createMissing: true,
                            onCreated: go => Undo.RegisterCreatedObjectUndo(go, "Create Fingertip Probe"));
                EditorUtility.SetDirty(dyn);
                Debug.Log("[HandDynamicPoses] Probes ready. Nudge each onto the fingertip pad and save the prefab.", dyn);
            }
        }
    }
}
