using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Grappling Gun: Fires a hook, renders a rope, and pulls the gun towards the hook point.
    /// </summary>
    public class GrapplingGun : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The prefab of the hook projectile.")]
        [SerializeField] private GameObject hookPrefab;

        [Tooltip("The point from where the hook is fired.")]
        [SerializeField] private Transform gunTip;

        [Tooltip("The Line Renderer to visualize the rope.")]
        [SerializeField] private LineRenderer ropeRenderer;

        [Tooltip("The XR controller used for firing the grappling hook.")]
        [SerializeField] private XRGrabInteractable controller;

        [Header("Grappling Settings")]
        [Tooltip("The speed at which the hook travels.")]
        [SerializeField] private float hookSpeed = 50f;

        [Tooltip("Maximum distance the hook can travel.")]
        [SerializeField] private float maxHookDistance = 50f;

        [Tooltip("The speed at which the gun is pulled towards the hook.")]
        [SerializeField] private float pullSpeed = 20f;

        [Tooltip("Delay before the hook auto-retracts if no hit occurs.")]
        [SerializeField] private float retractDelay = 2f;

        [Header("Climbing System Integration")]
        [Tooltip("The ClimbGrabPoint used to handle climbing mechanics.")]
        [SerializeField] private ClimbGrabPoint climbGrabPoint;

        // Internal variables
        private GameObject currentHook;
        private bool isHookFired = false;
        private bool isPulling = false;
        private Vector3 hookPoint;
        private Coroutine retractRoutine;

        private void Awake()
        {
            // Validate references
            if (!hookPrefab)
                Debug.LogError("Hook Prefab is not assigned!");

            if (!gunTip)
                Debug.LogError("Gun Tip is not assigned!");

            if (!ropeRenderer)
                Debug.LogError("Rope Renderer is not assigned!");

            if (!climbGrabPoint)
                Debug.LogError("ClimbGrabPoint is not assigned!");

            // Initialize the rope renderer
            ropeRenderer.positionCount = 2;
            ropeRenderer.enabled = false;

            // Subscribe to controller input
            if (controller)
            {
                controller.selectEntered.AddListener(OnTriggerPressed);
                controller.selectExited.AddListener(OnTriggerReleased);
            }
            else
            {
                Debug.LogError("XRBaseControllerInteractor is not assigned!");
            }
        }

        private void Update()
        {
            if (isHookFired)
            {
                UpdateRope();
            }

            // Pulling is handled via Coroutines
        }

        /// <summary>
        /// Called when the trigger is pressed to fire the hook.
        /// </summary>
        private void OnTriggerPressed(SelectEnterEventArgs args)
        {
            if (!isHookFired)
            {
                FireHook();
            }
        }

        /// <summary>
        /// Called when the trigger is released to stop pulling.
        /// </summary>
        private void OnTriggerReleased(SelectExitEventArgs args)
        {
            if (isPulling)
            {
                StopPulling();
            }

            if (isHookFired)
            {
                RetractHook();
            }
        }

        /// <summary>
        /// Fires the grappling hook.
        /// </summary>
        private void FireHook()
        {
            currentHook = Instantiate(hookPrefab, gunTip.position, gunTip.rotation);
            Rigidbody hookRb = currentHook.GetComponent<Rigidbody>();

            if (hookRb != null)
            {
                hookRb.linearVelocity = gunTip.forward * hookSpeed;
            }

            HookCollision hookCollision = currentHook.GetComponent<HookCollision>();
            if (hookCollision != null)
            {
                hookCollision.OnHookHit += OnHookHit;
            }

            isHookFired = true;
            ropeRenderer.enabled = true;
            hookPoint = Vector3.zero;

            // Start retracting the hook after a delay if it doesn't hit anything
            retractRoutine = StartCoroutine(RetractAfterDelay());
        }

        /// <summary>
        /// Handles the hook hitting a collider.
        /// </summary>
        /// <param name="hitPoint">The point where the hook hit.</param>
        private void OnHookHit(Vector3 hitPoint)
        {
            if (retractRoutine != null)
            {
                StopCoroutine(retractRoutine);
                retractRoutine = null;
            }

            hookPoint = hitPoint;
            isPulling = true;

            // Start pulling the gun towards the hook point
            StartCoroutine(PullGunTowardsHook());
        }

        /// <summary>
        /// Coroutine to pull the gun towards the hook point.
        /// </summary>
        private IEnumerator PullGunTowardsHook()
        {
            while (isPulling)
            {
                Vector3 direction = (hookPoint - gunTip.position).normalized;
                float distance = Vector3.Distance(gunTip.position, hookPoint);

                if (distance <= 0.5f)
                {
                    StopPulling();
                    yield break;
                }

                // Calculate step based on pullSpeed and deltaTime
                Vector3 step = direction * pullSpeed * Time.deltaTime;

                // Ensure we don't overshoot
                if (step.magnitude > distance)
                {
                    step = direction * distance;
                }

                // Move the gun towards the hook point
                transform.position += step;

                yield return null;
            }
        }

        /// <summary>
        /// Coroutine to retract the hook after a delay.
        /// </summary>
        private IEnumerator RetractAfterDelay()
        {
            yield return new WaitForSeconds(retractDelay);
            if (!isPulling)
            {
                RetractHook();
            }
        }

        /// <summary>
        /// Retracts the hook back to the gun.
        /// </summary>
        private void RetractHook()
        {
            if (currentHook != null)
            {
                HookCollision hookCollision = currentHook.GetComponent<HookCollision>();
                if (hookCollision != null)
                {
                    hookCollision.OnHookHit -= OnHookHit;
                }

                Destroy(currentHook);
            }

            isHookFired = false;
            ropeRenderer.enabled = false;
            hookPoint = Vector3.zero;

            if (retractRoutine != null)
            {
                StopCoroutine(retractRoutine);
                retractRoutine = null;
            }

            // Ensure pulling is stopped
            isPulling = false;
        }

        /// <summary>
        /// Updates the rope's position between the gun and the hook.
        /// </summary>
        private void UpdateRope()
        {
            if (currentHook != null)
            {
                ropeRenderer.SetPosition(0, gunTip.position);
                ropeRenderer.SetPosition(1, currentHook.transform.position);
            }
            else if (hookPoint != Vector3.zero)
            {
                ropeRenderer.SetPosition(0, gunTip.position);
                ropeRenderer.SetPosition(1, hookPoint);
            }
        }

        /// <summary>
        /// Stops pulling the gun towards the hook.
        /// </summary>
        private void StopPulling()
        {
            isPulling = false;
            RetractHook();
        }
    }

 
}
