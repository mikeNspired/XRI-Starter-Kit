using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public static class JointUtility
    {
        // Update these rules as needed
        public static bool ShouldSkipTransform(Transform t)
        {
            // Example logic:
            // Skip if name ends with "aux" or "Ignore"
            //if (t.name.EndsWith("aux")) return true;
            if (t.name.EndsWith("Ignore")) return true;
            return false;
        }

        // Recursively gather child transforms, skipping ones that ShouldSkipTransform returns true for
        public static void GatherTransformsForPose(Transform root, List<Transform> results)
        {
            if (!ShouldSkipTransform(root))
                results.Add(root);

            for (int i = 0; i < root.childCount; i++) GatherTransformsForPose(root.GetChild(i), results);
        }
    }
}

