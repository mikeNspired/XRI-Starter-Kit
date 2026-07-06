// Author MikeNspired. 
using UnityEngine;
using System;
using System.Linq;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Script located on the rootBone.
    /// Draw spheres and lines to show the relationship between joints and join location
    /// </summary>
    [Serializable]
    public class Pose : MonoBehaviour
    {
        public bool debugSpheresEnabled;
        [SerializeField] private float jointDebugSphereSize = .0045f;

        void OnDrawGizmosSelected()
        {
            if (debugSpheresEnabled) DrawJoints(transform);
        }

        public void DrawJoints(Transform joint)
        {
            if (!joint.name.EndsWith("aux"))
                Gizmos.DrawWireSphere(joint.position, jointDebugSphereSize);

            for (int i = 0; i < joint.childCount; ++i)
            {
                Transform child = joint.GetChild(i);
                Gizmos.DrawLine(joint.position, child.position);
                DrawJoints(child);
            }
        }
    }
}