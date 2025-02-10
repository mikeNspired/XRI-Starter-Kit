using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class IgnoreCharacterControllerCollider : MonoBehaviour
    {
        private Collider[] mainColliders;

        private void Start()
        {
            mainColliders = GetComponentsInChildren<Collider>(true);
            var playerCollider = FindFirstObjectByType<CharacterController>();
            if (!playerCollider) return;
            foreach (var c in mainColliders)
            {
                Physics.IgnoreCollision(c, playerCollider);
            }
        }
    }
}