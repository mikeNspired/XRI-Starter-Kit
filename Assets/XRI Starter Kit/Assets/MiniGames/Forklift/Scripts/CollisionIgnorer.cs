using UnityEngine;
using System.Collections.Generic;

namespace MikeNspired.XRIStarterKit
{
    public class CollisionIgnorer : MonoBehaviour
    {
        [Tooltip("List of GameObjects to ignore collisions with")]
        public List<GameObject> targets = new List<GameObject>();

        [Tooltip("Should collision ignoring happen in Awake (earlier) or Start?")]
        public bool runInAwake = true;

        private List<Collider> myColliders = new List<Collider>();
        private List<Collider> targetColliders = new List<Collider>();

        void Awake()
        {
            if (runInAwake)
            {
                CacheColliders();
                IgnoreCollisions();
            }
        }

        void Start()
        {
            if (!runInAwake)
            {
                CacheColliders();
                IgnoreCollisions();
            }
        }

        void CacheColliders()
        {
            // Get all colliders on this object hierarchy
            myColliders.Clear();
            myColliders.AddRange(GetComponentsInChildren<Collider>());

            // Get all colliders from target objects
            targetColliders.Clear();
            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    targetColliders.AddRange(target.GetComponentsInChildren<Collider>());
                }
            }
        }

        public void IgnoreCollisions()
        {
            foreach (Collider myCol in myColliders)
            {
                if (myCol == null) continue;

                foreach (Collider targetCol in targetColliders)
                {
                    if (targetCol == null) continue;

                    Physics.IgnoreCollision(myCol, targetCol, true);
                }
            }
        }

        // Optional: For debugging in the inspector
        private void OnValidate()
        {
            // Remove any null entries from the list
            targets.RemoveAll(item => item == null);
        }
    }
}