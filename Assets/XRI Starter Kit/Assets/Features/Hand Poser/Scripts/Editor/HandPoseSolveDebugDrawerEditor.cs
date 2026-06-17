using UnityEditor;

namespace MikeNspired.XRIStarterKit.Editor
{
    [CustomEditor(typeof(HandPoseSolveDebugDrawer))]
    public class HandPoseSolveDebugDrawerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DynamicPoseInfoBox.Draw(
                "Editor-only visualization of the last dynamic solve — per-finger spheres, contact state, " +
                "locked t, and labels. Purely diagnostic: safe to remove, or turn Draw off to hide it.");
            DrawDefaultInspector();
        }
    }
}
