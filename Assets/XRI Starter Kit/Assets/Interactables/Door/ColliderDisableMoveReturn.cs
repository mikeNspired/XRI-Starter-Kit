// Author MikeNspired. 
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class ColliderDisableMoveReturn : MonoBehaviour
    {
        public BoxCollider col;
        private Vector3 startingPosition;

        private void Start()
        {
            startingPosition = col.center;
        }

        public void DisableCollider()
        {
            if (!col.enabled) return;

            col.center = Vector3.forward * 1000;
            Invoke(nameof(Disable), .1f);
        }

        public void EnableCollider()
        {
            if (col.enabled) return;

            col.center = startingPosition;
            col.enabled = true;
        }

        private void Disable()
        {
            col.enabled = false;
            col.center = startingPosition;
        }
    }


}