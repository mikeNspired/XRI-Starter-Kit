// Copyright (c) MikeNspired. All Rights Reserved.
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace MikeNspired.UnityXRHandPoser
{
    /// <summary>
    /// Script located on the rootBone.
    /// Has the ability to draw spheres and lines to show the relationship between joints and join location
    /// </summary>
    [Serializable]
    public class Pose : MonoBehaviour, IComparable<Pose>
    {
        public bool debugSpheresEnabled;
        [SerializeField] private float jointDebugSphereSize = .0045f;

        void OnDrawGizmosSelected()
        {
            if (debugSpheresEnabled)
            {
                DrawJoints(transform);
            }
        }

        public void DrawJoints(Transform joint)
        {
            if (!joint.name.EndsWith("aux"))
                Gizmos.DrawWireSphere(joint.position, jointDebugSphereSize);

            for (int i = 0; i < joint.childCount; ++i)
            {
                Transform child = joint.GetChild(i);
                // if (child.name.EndsWith("aux"))
                // {
                //     DrawJoints(child);
                //     continue;
                // }

                Gizmos.DrawLine(joint.position, child.position);
                DrawJoints(child);
            }
        }

        public int CompareTo(Pose other)
        {
            return !other ? 1 : this.name.CompareTo(other.name);
        }
    }
}