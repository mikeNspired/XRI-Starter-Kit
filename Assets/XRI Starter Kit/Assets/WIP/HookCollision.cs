using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Handles hook collision events.
    /// </summary>
    public class HookCollision : MonoBehaviour
    {
        public delegate void HookHit(Vector3 hitPoint);
        public event HookHit OnHookHit;

        private void OnCollisionEnter(Collision collision)
        {
            // You can add more checks here to determine valid surfaces
            Vector3 hitPoint = collision.contacts[0].point;
            OnHookHit?.Invoke(hitPoint);
        }
    }
}