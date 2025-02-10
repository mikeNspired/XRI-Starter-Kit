// Author: MikeNspired

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// This component toggles certain child Transforms when an XRGrabInteractable is grabbed or released.
    /// 
    /// - <see cref="disableOnGrab"/> is disabled when grabbed and enabled when released.
    /// - <see cref="enableOnGrab"/> is enabled when grabbed and disabled when released.
    /// 
    /// Additionally, if <see cref="moveAndDisableAfterFrameOnGrabColliders"/> is true, colliders on the 
    /// target object are moved far away and then disabled, letting physics update before the object is hidden.
    /// 
    /// Implements <see cref="IReturnMovedColliders"/> so it can reset transforms if they’ve been moved.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class OnGrabEnableDisable : MonoBehaviour, IReturnMovedColliders
    {
        [Header("Main References")]
        [SerializeField] 
        private XRGrabInteractable grabInteractable;

        [Tooltip("Transform is disabled when grabbed, enabled when released.")]
        [SerializeField] 
        private Transform disableOnGrab;

        [Tooltip("Transform is enabled when grabbed, disabled when released.")]
        [SerializeField] 
        private Transform enableOnGrab;

        [Header("Settings")]
        [Tooltip("If true, moves the transform offscreen and disables colliders after a short delay.")]
        [SerializeField] 
        private bool moveAndDisableAfterFrameOnGrabColliders = true;

        private Vector3 disableOnGrabStartPosition;
        private Vector3 enableOnGrabStartPosition;

        private void Awake()
        {
            OnValidate();

            if (disableOnGrab)
                disableOnGrabStartPosition = disableOnGrab.localPosition;
            if (enableOnGrab)
                enableOnGrabStartPosition = enableOnGrab.localPosition;

            grabInteractable.selectEntered.AddListener(_ => OnGrab());
            grabInteractable.selectExited.AddListener(_ => OnRelease());
        }

 
        private void OnValidate()
        {
            if (!grabInteractable)
                grabInteractable = GetComponent<XRGrabInteractable>();
        }

        private void Start()
        {
            // Initialize GameObjects to their default active states
            if (disableOnGrab) 
                disableOnGrab.gameObject.SetActive(true);
            if (enableOnGrab) 
                enableOnGrab.gameObject.SetActive(false);
        }

        private void OnGrab()
        {
            // If we should move away & disable, use the coroutine approach
            if (moveAndDisableAfterFrameOnGrabColliders)
            {
                StopAllCoroutines();

                // Immediately enable the "enableOnGrab" object
                EnableTransform(enableOnGrab, enableOnGrabStartPosition);

                // Schedule the disabling of the "disableOnGrab" object
                if(disableOnGrab)
                {
                    // Reset colliders on the one about to be moved
                    var collidersTrigger = disableOnGrab.GetComponent<CollidersSetToTrigger>();
                    collidersTrigger?.ReturnToDefaultState();

                    StartCoroutine(MoveDisableAndReturn(disableOnGrab, disableOnGrabStartPosition));
                }
            }
            else
            {
                // No coroutine approach => immediate toggling
                if (disableOnGrab)
                    disableOnGrab.gameObject.SetActive(false);
                EnableTransform(enableOnGrab, enableOnGrabStartPosition);
            }
        }


        private void OnRelease()
        {
            if (moveAndDisableAfterFrameOnGrabColliders)
            {
                StopAllCoroutines();

                // Immediately enable the "disableOnGrab" object
                EnableTransform(disableOnGrab, disableOnGrabStartPosition);

                // Schedule the disabling of the "enableOnGrab" object
                if (enableOnGrab)
                {
                    var collidersTrigger = enableOnGrab.GetComponent<CollidersSetToTrigger>();
                    collidersTrigger?.ReturnToDefaultState();

                    StartCoroutine(MoveDisableAndReturn(enableOnGrab, enableOnGrabStartPosition));
                }
            }
            else
            {
                // No coroutine approach => immediate toggling
                if (enableOnGrab)
                    enableOnGrab.gameObject.SetActive(false);
                if (disableOnGrab)
                    disableOnGrab.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Sets all tracked objects to their enabled states and resets positions/colliders.
        /// </summary>
        public void EnableAll()
        {
            StopAllCoroutines();

            // Bring back disableOnGrab if assigned
            if (disableOnGrab)
            {
                disableOnGrab.gameObject.SetActive(true);
                ResetTransformLocal(disableOnGrab, disableOnGrabStartPosition);
                ResetCollidersToDefault(disableOnGrab);
            }

            // Bring back enableOnGrab if assigned
            if (enableOnGrab)
            {
                enableOnGrab.gameObject.SetActive(true);
                ResetTransformLocal(enableOnGrab, enableOnGrabStartPosition);
                ResetCollidersToDefault(enableOnGrab);
            }
        }

        /// <summary>
        /// Resets the transforms if they’ve been moved away.
        /// </summary>
        public void ReturnMovedColliders()
        {
            StopAllCoroutines();
            if (enableOnGrab)
                enableOnGrab.localPosition = enableOnGrabStartPosition;
            if (disableOnGrab)
                disableOnGrab.localPosition = disableOnGrabStartPosition;
        }

        /// <summary>
        /// Coroutine that moves the object far away, waits for physics updates, and disables it.
        /// </summary>
        private IEnumerator MoveDisableAndReturn(Transform objectToMove, Vector3 originalLocalPosition)
        {
            if (!objectToMove) yield break;
            
            // Temporarily set colliders to trigger
            var collidersTrigger = objectToMove.GetComponent<CollidersSetToTrigger>();
            collidersTrigger?.SetAllToTrigger();

            yield return PhysicsHelper.MoveAndDisable(objectToMove.gameObject);

            // Reset local position and colliders
            ResetTransformLocal(objectToMove, originalLocalPosition);
            collidersTrigger?.ReturnToDefaultState();
        }

        #region Helper Methods

        private void ResetTransformLocal(Transform target, Vector3 localPos)
        {
            if (!target) return;
            target.localPosition = localPos;
        }

        private void EnableTransform(Transform target, Vector3 startPos)
        {
            if (!target) return;

            // Enable object
            target.gameObject.SetActive(true);

            // Reset position
            target.localPosition = startPos;

            // Reset any colliders
            ResetCollidersToDefault(target);
        }

        private void ResetCollidersToDefault(Transform target)
        {
            if (!target) return;
            var collidersTrigger = target.GetComponent<CollidersSetToTrigger>();
            collidersTrigger?.ReturnToDefaultState();
        }

        #endregion
    }
}
