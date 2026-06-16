#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MikeNspired.XRIStarterKit.Editor
{
    /// <summary>
    /// Shared one-line "what is this" description boxes for the dynamic-posing components, with a single
    /// per-user toggle (menu: Tools ▸ XRI Starter Kit ▸ Dynamic Pose Info Boxes) that hides them all at
    /// once. The toggle is an EditorPref, so it is a personal editor preference and never serialized into
    /// a scene or asset.
    /// </summary>
    public static class DynamicPoseInfoBox
    {
        private const string MenuPath = "Tools/XRI Starter Kit/Dynamic Pose Info Boxes";
        private const string PrefKey  = "MikeNspired.XRI.ShowDynamicPoseInfo";

        public static bool Show
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        /// Draw a description box at the top of an inspector, unless the user has hidden them.
        public static void Draw(string text)
        {
            if (Show) EditorGUILayout.HelpBox(text, MessageType.None);
        }

        [MenuItem(MenuPath)]
        private static void Toggle() => Show = !Show;

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Show);
            return true;
        }
    }
}
#endif
