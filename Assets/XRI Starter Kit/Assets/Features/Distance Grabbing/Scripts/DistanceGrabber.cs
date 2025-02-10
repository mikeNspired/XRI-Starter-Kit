// Author: MikeNspired

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Allows the user to grab items from a distance by flicking their wrist or by holding a button (easy mode).
    /// Includes optional line visuals, item highlighting, and auto-grabbing if the grip is held.
    /// </summary>
    public class DistanceGrabber : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Main")] [SerializeField] private InputActionReference activationInput;
        [SerializeField] private NearFarInteractor nearFarInteractor = null;
        [SerializeField] private DistanceGrabberLineBender lineEffect = null;
        [SerializeField] private AudioRandomize launchAudio = null;
        [SerializeField] private SphereCollider mainHandCollider = null;
        [SerializeField] private GameObject EnableOnActive = null;

        [SerializeField, Tooltip("If item is less than this distance from hand, it will ignore the item")]
        private float minDistanceToAllowGrab = .2f;

        [Header("Easy Mode Settings")]
        [SerializeField, Tooltip("Disables flicking and enables button holding to easily grab item")]
        private bool easyModeGrabNoWristFlick = false;

        [SerializeField, Tooltip("Time holding button to grab item")]
        private float easyModeTimeTillGrab = .4f;

        [SerializeField,
         Tooltip("When item gets within distance to hand during launch, it will autoGrab if grip is held down")]
        private bool autoGrabIfGripping = false;

        [SerializeField, Tooltip("Distance before autoGrabbing if grip is held down and autoGrabIfGripping is true")]
        private float distanceToAutoGrab = .1f;

        [Header("Item Searching")] [SerializeField, Tooltip("How far RayCast will go")]
        private float rayCastLength = 10;

        [SerializeField,
         Tooltip(
             "Size of sphere that is created where rayCast hit. Items inside this sphere are potential grabbable items")]
        private float overlapSphereRadius = 1;

        [SerializeField,
         Tooltip(
             "distance to start shrinking overlapsphere, prevents grabbing items nearby when hand is close to table")]
        private float distanceStartShrinkingOverlap = 2f;

        [SerializeField, Tooltip("Min size when fully shrunk of overlap sphere")]
        private float overlapSphereRadiusMinSize = .1f;

        [SerializeField] private LayerMask rayCastMask = 1;

        [SerializeField, Tooltip("How far SphereCast will go")]
        private float sphereCastRadius = .5f;

        [SerializeField, Tooltip("Use a rayCast, where rayCastHit, will do a overLapSphere to search for items")]
        private bool rayCastSearch = true;

        [SerializeField,
         Tooltip(
             "Typically Fires after rayCast if nothing found, this will shoot a SphereCast, works well for items on desks that are hard to hit with raycast")]
        private bool sphereCastSearch = true;

        [Header("Debug")] [SerializeField] private bool showDebug = false;

        [SerializeField, Tooltip("Shows the distance and how large the Physics.SphereCast is")]
        private Transform debugSphereCast = null;

        [SerializeField, Tooltip("Shows the size of sphere overlap")]
        private Transform debugOverLapSphere = null;

        [Header("Line Canceling")]
        [SerializeField,
         Tooltip(
             "When to cancel trying to grab item based on rotation. A value of 0 lets you rotate this perpendicular to pointing at the item before canceling.")]
        private float dotProductCancel = .2f;

        [SerializeField] private Color outlineColor;

        [Header("Launch Wrist Flick")]
        [SerializeField, Tooltip("How much wrist flick is required to launch values 0 to 1")]
        private float rotationMagnitudeToLaunch = .4f;

        [Header("Item Launching")]
        [SerializeField, Tooltip("Main attribute to adjust flight time. Near 0 will be faster")]
        private float flightTimeMultiplier = .15f;

        [SerializeField, Tooltip("Grows main collider on hand while item is in flight to allow easier grabbing")]
        private float mainHandColliderSizeGrow = .2f;

        [SerializeField, Tooltip("Length of time collider is large after flight animation")]
        private float colliderLargeExtraTime = .25f;

        [SerializeField, Tooltip("Distance in world Y to add to hand position")]
        private float verticalGoalAddOn = .1f;

        //[SerializeField, Tooltip("Random rotation amount to add when item is flying")] private float randomRotationSpeed = 4; // (Commented in original script)
        [SerializeField, Tooltip("How much velocity the item will have when reached hand")]
        private float velocitySpeedWhenFinished = 1f;

        [SerializeField,
         Tooltip("Limits flight time from being too fast especially when the item distance is close to hand")]
        private float minFlightTime = .25f;

        [SerializeField, Tooltip("Distance to Stop animation to hand, use when adding drag to pause item near hand")]
        private float distanceToStop = 0;

        [SerializeField, Tooltip("Length of time to add drag to item when reaching hand")]
        private float dragTime = 0;

        [SerializeField, Tooltip("Amount of drag to add after item reaches hand")]
        private float dragHoldAmount = 0;

        #endregion

        #region Private Fields

        private XRInteractionManager interactionManager = null;
        private Transform currentTarget;
        private bool isActive = false;
        private bool isLaunching = false;
        private bool isGripping;
        private bool isInputActivated;

        private float sphereStartingSize;
        private float sphereCastStartingSize;
        private float mainHandColliderStartingSize;
        private Vector3 rayCastDebugPosition;

        // Easy Mode
        private float currentEasyModeTimer;

        // Item Launching
        private Vector3 velocity;
        private float startingDrag;

        // Wrist Flicking
        private const int KSmoothingFrameCount = 20;
        private readonly float[] smoothingFrameTimes = new float[KSmoothingFrameCount];
        private readonly Vector3[] smoothingAngularVelocityFrames = new Vector3[KSmoothingFrameCount];
        private int smoothingCurrentFrame;
        private Quaternion lastFrameRotation;
        private Vector3 currentSmoothedRotation;
        private float currentRotationMagnitude;
        private readonly float smoothingDuration = 0.25f;
        private readonly AnimationCurve smoothingCurve = AnimationCurve.Linear(1f, 1f, 1f, 0f);

        #endregion

        #region Unity Lifecycle

        private void OnValidate()
        {
            if (!lineEffect)
                lineEffect = GetComponent<DistanceGrabberLineBender>();
            if (!nearFarInteractor)
                nearFarInteractor = GetComponentInParent<NearFarInteractor>();
            if (!interactionManager)
                interactionManager = FindFirstObjectByType<XRInteractionManager>();

            if (debugSphereCast)
                debugSphereCast.gameObject.SetActive(showDebug);
            if (debugOverLapSphere)
                debugOverLapSphere.gameObject.SetActive(showDebug);
        }

        private void OnEnable() => activationInput.EnableAction();
        private void OnDisable() => activationInput.DisableAction();

        private void Start()
        {
            OnValidate();
            WristRotationReset();

            // Listen to the select event to reset states when something is grabbed
            nearFarInteractor.selectEntered.AddListener(x => ResetAll(x.interactableObject));

            if (mainHandCollider)
                mainHandColliderStartingSize = mainHandCollider.radius;

            EnableOnActive.SetActive(false);
            sphereStartingSize = overlapSphereRadius;
            sphereCastStartingSize = sphereCastRadius;

            // Subscribe to input
            activationInput.GetInputAction().performed += _ => isInputActivated = true;
            activationInput.GetInputAction().canceled += _ => isInputActivated = false;
        }

        private void Update()
        {
            // If already holding something, stop searching
            if (nearFarInteractor.interactablesSelected.Count > 0)
                return;

            if (isLaunching)
                return;

            if (currentTarget && !currentTarget.gameObject.activeInHierarchy)
                currentTarget = null;

            SearchForObjects();
            InitiateGrabFromInput();
            CheckToCancelRotation();

            if (!currentTarget)
                return;

            // Easy mode
            if (easyModeGrabNoWristFlick)
            {
                HandleEasyModeGrab();
                return;
            }

            // Wrist Flick
            UpdateRotationFrames();
            SetCurrentRotationMagnitude();
            TryToLaunchItem(currentTarget);
        }

        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            Gizmos.color = Color.cyan;
            if (rayCastDebugPosition != Vector3.zero)
                Gizmos.DrawWireSphere(rayCastDebugPosition, overlapSphereRadius);

            if (debugSphereCast)
                debugSphereCast.transform.localScale =
                    new Vector3(sphereCastRadius * 2, sphereCastRadius * 2, rayCastLength);

            if (debugOverLapSphere)
                debugOverLapSphere.transform.localScale = Vector3.one * overlapSphereRadius * 2;
        }

        #endregion

        #region Object Searching

        /// <summary>
        /// Looks for potential objects to grab using raycast/overlapsphere and spherecast.
        /// </summary>
        private void SearchForObjects()
        {
            if (isActive)
                return;

            // 1) RayCast Search
            if (rayCastSearch)
            {
                if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, rayCastLength,
                        rayCastMask, QueryTriggerInteraction.Ignore))
                {
                    rayCastDebugPosition = hit.point;
                    Collider[] closestHits = Physics.OverlapSphere(
                        hit.point, overlapSphereRadius, rayCastMask, QueryTriggerInteraction.Ignore);

                    ScaleOverlapSphere();
                    Transform[] potentialTargets = Array.ConvertAll(closestHits, c => c.transform);
                    if (TrySetNearestTarget(potentialTargets, hit.point))
                    {
                        return;
                    }
                    // Removed the unconditional return here so that if no valid target is found,
                    // execution continues to try other search methods or clear the target.
                }
                else
                {
                    rayCastDebugPosition = Vector3.zero;
                }
            }
            
            // 2) SphereCast Search (if RayCast didn't find anything)
            if (sphereCastSearch)
            {
                RaycastHit[] sphereCastHits = Physics.SphereCastAll(
                    transform.position, sphereCastRadius, transform.forward, rayCastLength,
                    rayCastMask, QueryTriggerInteraction.Ignore);

                if (sphereCastHits.Length > 0)
                {
                    Transform[] potentialTargets = Array.ConvertAll(sphereCastHits, s => s.transform);
                    if (TrySetNearestTarget(potentialTargets, transform.position))
                        return;
                } 
            }

            if (showDebug)
            {
                Debug.DrawRay(transform.position, transform.forward * rayCastLength, Color.cyan);

                if (rayCastDebugPosition != Vector3.zero && debugOverLapSphere)
                {
                    debugOverLapSphere.gameObject.SetActive(true);
                    debugOverLapSphere.position = rayCastDebugPosition;
                }
                else if (debugOverLapSphere)
                {
                    debugOverLapSphere.gameObject.SetActive(false);
                }
            }

            StopHighlight(currentTarget);
            currentTarget = null;
        }

        /// <summary>
        /// Dynamically shrinks or grows the overlap sphere (and sphere cast radius) based on distance.
        /// </summary>
        private void ScaleOverlapSphere()
        {
            float distance = Vector3.Distance(transform.position, rayCastDebugPosition);
            float t = Mathf.Clamp01(distance / distanceStartShrinkingOverlap);

            overlapSphereRadius = Mathf.Lerp(overlapSphereRadiusMinSize, sphereStartingSize, t);
            sphereCastRadius = Mathf.Lerp(overlapSphereRadiusMinSize, sphereCastStartingSize, t);
        }

        /// <summary>
        /// Tries to set the nearest valid grab target from a set of colliders/transforms.
        /// </summary>
        /// <param name="hits">Potential colliders or transforms to check.</param>
        /// <param name="comparisonPoint">Center point to measure closest distance from.</param>
        /// <returns>True if a valid target is found and set; otherwise false.</returns>
        private bool TrySetNearestTarget(Transform[] hits, Vector3 comparisonPoint)
        {
            if (hits.Length == 0)
                return false;

            float nearestDistance = float.PositiveInfinity;
            bool foundHit = false;
            XRGrabInteractable nearestGrabbable = null;

            foreach (Transform hit in hits)
            {
                if (!hit) continue;
                if (Vector3.Distance(transform.position, hit.position) <= minDistanceToAllowGrab)
                    continue; // too close

                var interactable = hit.GetComponentInParent<XRGrabInteractable>();
                if (!interactable || interactable.interactorsSelecting.Count > 0 || !interactable.enabled)
                    continue;

                // Check if allowed to DistanceGrab
                var itemData = interactable.GetComponent<InteractableItemData>();
                if (!itemData || !itemData.canDistanceGrab)
                    continue;

                // Check if anything is blocking (could refine by layerMasks)
                if (Physics.Raycast(hit.position, (transform.position - hit.position), out RaycastHit blockCheck, 1,
                        rayCastMask, QueryTriggerInteraction.Ignore))
                {
                    if (blockCheck.transform != interactable.transform)
                    {
                        if (showDebug)
                            Debug.DrawRay(hit.position, (transform.position - hit.position).normalized, Color.magenta);
                        continue;
                    }
                }

                float distance = Vector3.Distance(hit.position, comparisonPoint);
                if (distance < nearestDistance)
                {
                    foundHit = true;
                    nearestDistance = distance;
                    nearestGrabbable = interactable;
                }
            }

            if (!foundHit || !nearestGrabbable)
                return false;

            SetNewCurrentTarget(nearestGrabbable.transform);
            return true;
        }

        /// <summary>
        /// Sets the current target, handling highlighting and resetting if needed.
        /// </summary>
        /// <param name="newTarget">Target transform to set.</param>
        private void SetNewCurrentTarget(Transform newTarget)
        {
            if (currentTarget == newTarget)
                return;

            if (currentTarget)
                StopHighlight(currentTarget);

            currentTarget = newTarget;
            WristRotationReset();
            HighlightTarget();
        }

        #endregion

        #region Grab Initiation and Cancel

        /// <summary>
        /// Initiates grab based on input button states.
        /// </summary>
        private void InitiateGrabFromInput()
        {
            if (isInputActivated && !isGripping)
            {
                isGripping = true;
                isActive = true;
                currentEasyModeTimer = 0f;
                SetupLine();
            }
            else if (!isInputActivated)
            {
                isGripping = false;
                isActive = false;
                StopLine();
            }
        }

        /// <summary>
        /// Checks if we should cancel the grab based on rotation dot product.
        /// </summary>
        private void CheckToCancelRotation()
        {
            if (!currentTarget)
                return;

            Vector3 targetDirection = currentTarget.position - transform.position;
            float dot = Vector3.Dot(transform.forward, targetDirection.normalized);

            if (dot < dotProductCancel)
                StopLine();
        }

        /// <summary>
        /// Stops line visuals, highlighting, and toggles off active states.
        /// </summary>
        /// <param name="target">Current target transform.</param>
        private void CancelTarget(Transform target)
        {
            if (target)
                StopHighlight(target);

            StopLine();
            isActive = false;
        }

        #endregion

        #region Easy Mode

        /// <summary>
        /// Handles the "easy mode" grab logic where holding a button for a set time triggers a grab.
        /// </summary>
        private void HandleEasyModeGrab()
        {
            if (!isActive || !currentTarget)
                return;

            currentEasyModeTimer += Time.deltaTime;
            if (currentEasyModeTimer < easyModeTimeTillGrab)
                return;

            currentEasyModeTimer = 0;
            Launch(currentTarget);
        }

        #endregion

        #region Wrist Flick / Rotation Detection

        /// <summary>
        /// Updates rotation frames, capturing angular velocity for flick detection.
        /// </summary>
        private void UpdateRotationFrames()
        {
            smoothingFrameTimes[smoothingCurrentFrame] = Time.time;

            Quaternion rotationDiff = transform.rotation * Quaternion.Inverse(lastFrameRotation);
            Vector3 eulerDelta = new Vector3(
                Mathf.DeltaAngle(0, rotationDiff.eulerAngles.x),
                Mathf.DeltaAngle(0, rotationDiff.eulerAngles.y),
                Mathf.DeltaAngle(0, rotationDiff.eulerAngles.z)
            );

            smoothingAngularVelocityFrames[smoothingCurrentFrame] = (eulerDelta / Time.deltaTime) * Mathf.Deg2Rad;

            smoothingCurrentFrame = (smoothingCurrentFrame + 1) % KSmoothingFrameCount;
            lastFrameRotation = transform.rotation;
        }

        /// <summary>
        /// Resets the stored rotation frames and times to start fresh.
        /// </summary>
        private void WristRotationReset()
        {
            lastFrameRotation = transform.rotation;
            Array.Clear(smoothingFrameTimes, 0, smoothingFrameTimes.Length);
            Array.Clear(smoothingAngularVelocityFrames, 0, smoothingAngularVelocityFrames.Length);
            smoothingCurrentFrame = 0;
        }

        /// <summary>
        /// Calculates and sets the smoothed rotation magnitude (flick strength).
        /// </summary>
        private void SetCurrentRotationMagnitude()
        {
            Vector3 smoothedAngularVelocity = GetSmoothedVelocityValue(smoothingAngularVelocityFrames);
            currentSmoothedRotation = smoothedAngularVelocity;
            currentRotationMagnitude = currentSmoothedRotation.magnitude;
        }

        /// <summary>
        /// Applies the smoothing curve to the recorded angular velocity frames.
        /// </summary>
        /// <param name="velocityFrames">Stored angular velocity frames.</param>
        /// <returns>Smoothed angular velocity.</returns>
        private Vector3 GetSmoothedVelocityValue(Vector3[] velocityFrames)
        {
            Vector3 accumulatedVelocity = Vector3.zero;
            float totalWeights = 0f;
            int frameCounter = 0;

            for (; frameCounter < KSmoothingFrameCount; frameCounter++)
            {
                int frameIdx =
                    (((smoothingCurrentFrame - frameCounter - 1) % KSmoothingFrameCount) + KSmoothingFrameCount) %
                    KSmoothingFrameCount;
                if (smoothingFrameTimes[frameIdx] == 0f)
                    break;

                float timeAlpha = (Time.time - smoothingFrameTimes[frameIdx]) / smoothingDuration;
                float velocityWeight = smoothingCurve.Evaluate(Mathf.Clamp01(1f - timeAlpha));

                accumulatedVelocity += velocityFrames[frameIdx] * velocityWeight;
                totalWeights += velocityWeight;

                if (Time.time - smoothingFrameTimes[frameIdx] > smoothingDuration)
                    break;
            }

            return totalWeights > 0f ? accumulatedVelocity / totalWeights : Vector3.zero;
        }

        /// <summary>
        /// Attempts to launch the current target if flick strength is high enough.
        /// </summary>
        /// <param name="target">The potential target to launch.</param>
        private void TryToLaunchItem(Transform target)
        {
            if (!isActive) return;
            if (currentRotationMagnitude < rotationMagnitudeToLaunch) return;
            Launch(target);
        }

        #endregion

        #region Launch / Flight Logic

        /// <summary>
        /// Launches the target item towards the controller.
        /// </summary>
        /// <param name="target">Target to launch.</param>
        private void Launch(Transform target)
        {
            if (!target) return;

            // Audio
            launchAudio.transform.position = target.position;
            launchAudio.Play();

            isLaunching = true;
            CancelTarget(target);
            StartCoroutine(SimulateProjectile(target));
        }

        /// <summary>
        /// Simulates the item moving towards the hand in an arc / direct path, then optionally auto-grabs.
        /// </summary>
        private IEnumerator SimulateProjectile(Transform target)
        {
            var rb = target.GetComponent<Rigidbody>();
            if (!rb) yield break;

            startingDrag = rb.linearDamping;
            Vector3 goalPosition = transform.position + Vector3.up * verticalGoalAddOn;
            Vector3 startPosition = target.position;
            Quaternion startRotation = target.rotation;

            velocity = goalPosition - startPosition;
            rb.angularVelocity = Vector3.zero;

            float distanceToGoal = velocity.magnitude;
            float adjustedFlightTime =
                Mathf.Clamp(flightTimeMultiplier * distanceToGoal, minFlightTime, float.MaxValue);

            mainHandCollider.radius = mainHandColliderSizeGrow;

            float elapsedTime = 0f;
            while (elapsedTime <= adjustedFlightTime)
            {
                rb.Sleep(); // temporarily disable physics influence

                float currentStep = elapsedTime / adjustedFlightTime;

                // Position
                Vector3 slerpPos = Vector3.Slerp(startPosition, goalPosition, currentStep);
                Vector3 lerpPos = Vector3.Lerp(startPosition, goalPosition, currentStep);
                target.position = (slerpPos + (lerpPos * 2)) / 3f;

                // Rotation
                target.rotation = Quaternion.Lerp(startRotation, transform.rotation, currentStep);

                // Exit if close enough
                if (Vector3.Distance(transform.position, target.position) < distanceToStop)
                    break;

                elapsedTime += Time.deltaTime;
                TryToAutoGrab();
                yield return null;
            }

            // Return hand collider back to original size eventually
            StartCoroutine(ShrinkColliderAfterDelay());

            rb.linearDamping = dragHoldAmount;
            rb.linearVelocity = velocity * velocitySpeedWhenFinished;
            rb.WakeUp();

            yield return new WaitForSeconds(dragTime);
            rb.linearDamping = startingDrag;
            isLaunching = false;
        }

        /// <summary>
        /// Grows the hand collider for easier catch, then shrinks after a delay.
        /// </summary>
        private IEnumerator ShrinkColliderAfterDelay()
        {
            yield return new WaitForSeconds(colliderLargeExtraTime);
            mainHandCollider.radius = mainHandColliderStartingSize;
        }

        #endregion

        #region Auto Grab

        /// <summary>
        /// Tries to auto-grab the item if it's close enough and grip is held.
        /// </summary>
        private void TryToAutoGrab()
        {
            if (!autoGrabIfGripping) return;
            if (nearFarInteractor.interactablesSelected.Count > 0) return;
            if (!currentTarget) return;

            // If item is close enough and input is still held
            if (Vector3.Distance(currentTarget.position, transform.position) >= distanceToAutoGrab) return;
            if (!isInputActivated) return;

            StopAllCoroutines();

            // Snap item to hand
            currentTarget.position = nearFarInteractor.transform.position;
            currentTarget.rotation = nearFarInteractor.transform.rotation;

            StartCoroutine(GrabItem(nearFarInteractor, currentTarget.GetComponent<XRBaseInteractable>()));
        }

        /// <summary>
        /// Makes the controller grab the interactable item the next frame, resetting states.
        /// </summary>
        private IEnumerator GrabItem(XRBaseInteractor interactor, XRBaseInteractable interactable)
        {
            yield return new WaitForFixedUpdate();
            if (!interactable) yield break;

            ResetAll(interactable);

            mainHandCollider.radius = mainHandColliderStartingSize;
            if (interactor.interactablesSelected.Count > 0) yield break;

            // Perform the grab
            interactionManager.SelectEnter(interactor, (IXRSelectInteractable)interactable);
        }

        #endregion

        #region Reset

        /// <summary>
        /// Resets state when an item is actually grabbed (SelectEnter event).
        /// </summary>
        private void ResetAll(IXRSelectInteractable interactable)
        {
            CancelTarget(currentTarget);
            isLaunching = false;
            mainHandCollider.radius = mainHandColliderStartingSize;

            // Zero out velocity if the grabbed object is not kinematic
            if (interactable != null &&
                interactable.transform.TryGetComponent(out Rigidbody rb) &&
                !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }

        #endregion

        #region Visual Feedback

        /// <summary>
        /// Highlights the current target with an XRQuickOutline if present.
        /// </summary>
        private void HighlightTarget()
        {
            if (!currentTarget) return;
            var outline = currentTarget.GetComponentInChildren<XRQuickOutline>();
            if (outline)
                outline.HighlightWithColor(outlineColor);
        }

        /// <summary>
        /// Stops highlighting the given transform if it has an XRQuickOutline.
        /// </summary>
        private void StopHighlight(Transform target)
        {
            if (!target) return;
            var outline = target.GetComponentInChildren<XRQuickOutline>();
            if (outline)
                outline.StopHighlight();
        }

        /// <summary>
        /// Sets up the line renderer/effect to show the distance grab path.
        /// </summary>
        private void SetupLine()
        {
            if (!currentTarget) return;
            lineEffect.Start(currentTarget);
            EnableOnActive.SetActive(true);
        }

        /// <summary>
        /// Disables the line renderer/effect.
        /// </summary>
        private void StopLine()
        {
            lineEffect.Stop();
            EnableOnActive.SetActive(false);
        }

        #endregion
    }
}