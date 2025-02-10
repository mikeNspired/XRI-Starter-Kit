// Author MikeNspired. 
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Stores transform data without instantiating a transform to store the data in.Transforms can only exist as a gameObject in a scene.
    /// </summary>
    [System.Serializable]
    public struct TransformStruct
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public TransformStruct(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public void SetTransformStruct(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

    }
}