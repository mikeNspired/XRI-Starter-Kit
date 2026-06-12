using System;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [CreateAssetMenu(menuName = "HandPoser/PoseScriptableObject", fileName = "NewPose")]
    public class PoseScriptableObject : ScriptableObject, IComparable<PoseScriptableObject>
    {
        [System.Serializable]
        public struct JointData
        {
            public string jointName;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        public JointData[] joints;

        public int CompareTo(PoseScriptableObject other)
        {
            return !other ? 1 : string.Compare(this.name, other.name, StringComparison.Ordinal);
        }
    }
}