using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public static class GameObjectCloner
    {
        public static GameObject DuplicateAndStrip(GameObject original)
        {
            if (original == null) return null;
            var originalParent = original.transform.parent;

            original.transform.parent = null;
            GameObject clone = Object.Instantiate(original);
            clone.name = original.name + "_Stripped";

            StripNonVisualComponents(clone.transform);
            RemoveNullMaterialSlots(clone.transform);

            original.transform.parent = originalParent;
            return clone;
        }

        private static void RemoveNullMaterialSlots(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var mr in renderers)
            {
                DestroyOutlineMaterials(mr);

                Material[] currentMats = mr.sharedMaterials;
                List<Material> validMats = currentMats.Where(t => t).ToList();

                if (validMats.Count != currentMats.Length) mr.materials = validMats.ToArray();
            }
        }

        private static void DestroyOutlineMaterials(Renderer r)
        {
            var materials = r.materials;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i].name.Contains("Outline"))
                    Object.DestroyImmediate(materials[i]);
        }

        private static void StripNonVisualComponents(Transform root)
        {
            // Remove components in safe order (To prevent errors from [RequiredComponent("")]
            RemoveDependentComponents<OnGrabEnableDisable>(root);
            RemoveDependentComponents<XRGrabInteractable>(root);
            RemoveDependentComponents<XRBaseInteractable>(root);
            RemoveDependentComponents<ColorMaterialPropertyAffordanceReceiver>(root);
            RemoveDependentComponents<GraphicRaycaster>(root);
            RemoveDependentComponents<Image>(root);
            RemoveDependentComponents<CanvasScaler>(root);

            // Remove physics components
            RemoveComponents<Rigidbody>(root);
            RemoveComponents<Collider>(root);

            // Remove all other non-visual components
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null) continue;
                if (ShouldPreserve(component)) continue;
                Object.DestroyImmediate(component);
            }
        }

        private static bool ShouldPreserve(Component component)
        {
            // Preserve essential visual components and enabled renderers
            if (component is Transform || component is MeshFilter)
                return true;

            if (component is Renderer renderer)
                return renderer.enabled;

            return false;
        }

        private static void RemoveDependentComponents<T>(Transform root) where T : Component
        {
            foreach (var component in root.GetComponentsInChildren<T>(true))
                Object.DestroyImmediate(component);
        }

        private static void RemoveComponents<T>(Transform root) where T : Component
        {
            foreach (var component in root.GetComponentsInChildren<T>(true))
                Object.DestroyImmediate(component);
        }
    }
}