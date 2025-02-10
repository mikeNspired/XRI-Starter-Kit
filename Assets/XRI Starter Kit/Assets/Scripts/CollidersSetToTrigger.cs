using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class CollidersSetToTrigger : MonoBehaviour
    {
        [SerializeField] List<colliderData> colliders = new List<colliderData>();

        private void Start()
        {
            var cols = GetComponentsInChildren<Collider>();
            foreach (var c in cols)
                colliders.Add(new colliderData(c, c.isTrigger));
        }

        public void SetAllToTrigger()
        {
            foreach (var c in colliders)
                c.collider.isTrigger = true;
        }
  
        public void ReturnToDefaultState()
        {
            foreach (var c in colliders)
                c.collider.isTrigger = c.isTrigger;
        }

        [Serializable]
        private struct colliderData
        {
            public Collider collider;
            public bool isTrigger;

            public colliderData(Collider c, bool isTrigger)
            {
                collider = c;
                this.isTrigger = isTrigger;
            } 
        }
    }
}