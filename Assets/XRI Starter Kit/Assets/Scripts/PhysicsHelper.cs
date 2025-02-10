using System.Collections;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Provides methods to quickly "move" objects or colliders out of the scene,
    /// optionally wait for physics updates, and then disable or destroy them.
    /// Also includes an optional method to simulate OnTriggerExit events.
    /// </summary>
    public static class PhysicsHelper
    {
        // Position far away from origin, so objects effectively vanish from gameplay
        private static readonly Vector3 OffscreenPosition = Vector3.one * 9999999.9f;

        /// <summary>
        /// Immediately teleports the Transform to the offscreen position.
        /// </summary>
        public static void TeleportOffscreen(Transform t)
        {
            if (t != null)
                t.position = OffscreenPosition;
        }

        /// <summary>
        /// Immediately teleports a Collider offscreen by moving its Transform.
        /// </summary>
        public static void TeleportOffscreen(Collider col)
        {
            if (col != null)
                col.transform.position = OffscreenPosition;
        }

        /// <summary>
        /// Coroutine that teleports the GameObject offscreen, waits a specified number
        /// of physics frames, then disables it.
        /// </summary>
        /// <param name="go">GameObject to move and disable.</param>
        /// <param name="physicsFramesToWait">Number of fixed update frames to wait before disabling.</param>
        public static IEnumerator MoveAndDisable(GameObject go, int physicsFramesToWait = 2)
        {
            if (go == null) yield break;

            // 1) Move it offscreen right away
            go.transform.position = OffscreenPosition;

            // 2) Wait the desired number of fixed updates
            for (int i = 0; i < physicsFramesToWait; i++)
                yield return new WaitForFixedUpdate();

            // 3) Disable
            go.SetActive(false);
        }

        /// <summary>
        /// Coroutine that teleports the GameObject offscreen, waits a specified number
        /// of physics frames, then destroys it.
        /// </summary>
        /// <param name="go">GameObject to move offscreen, then destroy.</param>
        /// <param name="physicsFramesToWait">Number of fixed update frames to wait before destroying.</param>
        public static IEnumerator MoveAndDestroy(GameObject go, int physicsFramesToWait = 2)
        {
            if (go == null) yield break;

            // 1) Move it offscreen
            go.transform.position = OffscreenPosition;

            // 2) Wait
            for (int i = 0; i < physicsFramesToWait; i++)
                yield return new WaitForFixedUpdate();

            // 3) Destroy
            Object.Destroy(go);
        }

        /// <summary>
        /// Moves a Collider offscreen, waits a specified number of fixed updates,
        /// then disables the collider and resets its local position.
        /// </summary>
        /// <param name="collider">Collider to move and disable.</param>
        /// <param name="resetLocalPosition">Position in local space to restore after disabling.</param>
        /// <param name="physicsFramesToWait">Number of fixed update frames to wait before disabling.</param>
        public static IEnumerator MoveAndDisableCollider(Collider collider, Vector3 resetLocalPosition,
            int physicsFramesToWait = 2)
        {
            if (collider == null) yield break;

            // 1) Move offscreen
            collider.transform.position = OffscreenPosition;

            // 2) Wait
            for (int i = 0; i < physicsFramesToWait; i++)
                yield return new WaitForFixedUpdate();

            // 3) Disable the collider
            collider.enabled = false;
            collider.transform.localPosition = resetLocalPosition;
        }

        /// <summary>
        /// (Optional) Simulates a trigger exit for every collider overlapping the given collider,
        /// allowing you to effectively notify other objects that this collider is "gone."
        /// You might call this right before teleporting or disabling the collider.
        /// </summary>
        /// <param name="collider">The collider that is effectively exiting.</param>
        /// <remarks>
        /// By default, we use OverlapBox with the collider's bounding box. If you have a different shape (sphere, capsule),
        /// consider using OverlapSphere, OverlapCapsule, etc.
        /// </remarks>
        public static void SimulateTriggerExit(Collider collider)
        {
            if (collider == null) return;

            // Compute an approximate bounding box for Overlap
            Vector3
                halfExtents = collider.bounds.extents * 0.5f; // somewhat smaller if you want to avoid false positives
            var hits = Physics.OverlapBox(
                collider.bounds.center,
                halfExtents,
                collider.transform.rotation,
                ~0,
                QueryTriggerInteraction.Collide
            );

            foreach (var hit in hits)
            {
                // Optional: you can do a more robust approach like interface-based calls
                // For simplicity, here's a quick SendMessage approach:
                hit.SendMessage("OnTriggerExit", collider, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}