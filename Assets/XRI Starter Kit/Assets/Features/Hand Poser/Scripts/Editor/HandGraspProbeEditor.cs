using UnityEditor;

namespace MikeNspired.XRIStarterKit.Editor
{
    [CustomEditor(typeof(HandGraspProbe))]
    public class HandGraspProbeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DynamicPoseInfoBox.Draw(
                "Optional. While the hand is empty, curls the fingers onto nearby NON-grabbable world " +
                "geometry (e.g. press the open hand on a table). Off by default. AutoGraspTest = editor-only " +
                "dial-in tool; GripHoldPose = runtime, solves while the grip is held near a surface.");
            DrawDefaultInspector();
        }
    }
}
