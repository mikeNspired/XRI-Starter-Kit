using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class IgnoreCharacterControllerCollider : MonoBehaviour
    {
        private Collider[] mainColliders;

        private void Start()
        {
            mainColliders = GetComponentsInChildren<Collider>(true);
            var playerCollider = FindObjectOfType<CharacterController>();
            if (!playerCollider) return;
            foreach (var c in mainColliders)
            {
                Physics.IgnoreCollision(c, playerCollider);
            }
        }
    }
}