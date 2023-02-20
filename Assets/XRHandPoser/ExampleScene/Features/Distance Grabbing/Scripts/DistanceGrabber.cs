// Author MikeNspired. 

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace MikeNspired.UnityXRHandPoser
{
    public class DistanceGrabber : MonoBehaviour
    {
        [Header("Main")] [SerializeField] private InputActionReference activationInput;

        [SerializeField] private XRDirectInteractor directInteractor = null;
        [SerializeField] private DistanceGrabberLineBender lineEffect = null;
        [SerializeField] private AudioRandomize launchAudio = null;
        [SerializeField] private SphereCollider mainHandCollider = null;
        [SerializeField] private GameObject EnableOnActive = null;

        [SerializeField] [Tooltip("If item is less than this distance from hand, it will ignore the item")]
        private float minDistanceToAllowGrab = .2f;


        [Header("Easy Mode Settings")] [SerializeField] [Tooltip("Disables flicking and enables button holding to easily grab item")]
        private bool easyModeGrabNoWristFlick = false;

        [SerializeField] [Tooltip("Time holding button to grab item")]
        private float easyModeTimeTillGrab = .4f;

        [SerializeField] [Tooltip("When item gets within distance to hand during launch, it will autoGrab if grip is held down")]
        private bool autoGrabIfGripping = false;

        [SerializeField] [Tooltip("Distance before autoGrabbing if grip is held down and autoGrabIfGripping is true")]
        private float distanceToAutoGrab = .1f;


        [Header("Item Searching")] [SerializeField] [Tooltip("How far RayCast will go")]
        private float rayCastLength = 10;

        [SerializeField] [Tooltip("Size of sphere that is created where rayCast hit. Items inside this sphere are potential grabbable items")]
        private float overlapSphereRadius = 1;

        [SerializeField] [Tooltip("distance to start shrinking overlapsphere, prevents grabbing items nearby when hand is close to table")]
        private float distanceStartShrinkingOverlap = 2f;

        [SerializeField] [Tooltip("Min size when fully shrunk of overlap sphere")]
        private float overlapSphereRadiusMinSize = .1f;

        [SerializeField] private LayerMask rayCastMask = 1;


        [SerializeField] [Tooltip("How far RayCast will go")]
        private float sphereCastRadius = .5f;

        [SerializeField] [Tooltip("Use a rayCast, where rayCastHit, will do a overLapSphere to search for items")]
        private bool rayCastSearch = true;

        [SerializeField] [Tooltip("Typically Fires after rayCast if nothing found, this will shoot a SphereCast, works well for items on desks that is hard to hit with raycast")]
        private bool sphereCastSearch = true;


        [Header("Debug")] [SerializeField] private bool showDebug = false;

        [SerializeField] [Tooltip("Shows the distance and how large the Physics.SphereCast is")]
        private Transform debugSphereCast = null;

        [SerializeField] [Tooltip("Shows the size of sphere overlap")]
        private Transform debugOverLapSphere = null;


        [Header("Line Canceling")] [SerializeField] [Tooltip("When to cancel trying to grab item based on rotation. A value of 0 lets you rotate this perpendicular to pointing at the item before canceling.")]
        private float dotProductCancel = .2f;

        [SerializeField] private Color outlineColor;

        private float sphereStartingSize, sphereCastStartingSize;
        private XRInteractionManager interactionManager = null;
        private RaycastHit[] rayCastHits;
        private Transform currentTarget;
        private bool isActive = false, isLaunching = false;
        private float mainHandColliderStartingSize;
        private Vector3 rayCastDebugPosition;
        private bool isGripping, isInputActivated;

        private void Start()
        {
            OnValidate();
            WristRotationReset();
            directInteractor.onSelectEntered.AddListener(Reset);
            if (mainHandCollider)
                mainHandColliderStartingSize = mainHandCollider.radius;
            EnableOnActive.SetActive(false);

            sphereStartingSize = overlapSphereRadius;
            sphereCastStartingSize = sphereCastRadius;

            activationInput.GetInputAction().performed += x => isInputActivated = true;
            activationInput.GetInputAction().canceled += x => isInputActivated = false;
        }

        private void OnValidate()
        {
            if (!lineEffect)
                lineEffect = GetComponent<DistanceGrabberLineBender>();
            if (!directInteractor)
                directInteractor = GetComponentInParent<XRDirectInteractor>();
            if (!interactionManager)
                interactionManager = FindObjectOfType<XRInteractionManager>();

            debugSphereCast.gameObject.SetActive(showDebug);
            debugOverLapSphere.gameObject.SetActive(showDebug);
        }

        private void OnEnable() => activationInput.EnableAction();

        private void OnDisable() => activationInput.DisableAction();

        private void Update()
        {
            if (directInteractor.selectTarget) return;
            if (isLaunching) return;

            //Check if controller is holding an item already
            if (directInteractor.selectTarget)
            {
                CancelTarget(currentTarget);
                return;
            }

            SearchForObjects();

            InitiateGrabStartFromTrigger();
            CheckToCancel();

            if (!currentTarget) return;

            if (easyModeGrabNoWristFlick)
            {
                EasyModeLaunch(currentTarget);
                return;
            }

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
                debugSphereCast.transform.localScale = new Vector3(sphereCastRadius * 2, sphereCastRadius * 2, rayCastLength);
            if (debugOverLapSphere)
                debugOverLapSphere.transform.localScale = Vector3.one * overlapSphereRadius * 2;
        }

        private void SearchForObjects()
        {
            if (isActive) return;

            if (rayCastSearch)
            {
                if (Physics.Raycast(transform.position, transform.forward, out RaycastHit rayCastHit, rayCastLength, rayCastMask, QueryTriggerInteraction.Ignore))
                {
                    rayCastDebugPosition = rayCastHit.point;
                    Collider[] closestHits = Physics.OverlapSphere(rayCastHit.point, overlapSphereRadius, rayCastMask, QueryTriggerInteraction.Ignore);
                    Transform[] potentialGrabbableItems = Array.ConvertAll(closestHits, s => s.transform);
                    ScaleOverlapSphere();
                    if (CheckForNearest(potentialGrabbableItems, rayCastHit.point))
                        return;
                }
                else
                    rayCastDebugPosition = Vector3.zero;
            }

            if (sphereCastSearch)
            {
                //If nothing found, try SphereCasting incase on a small area like a podium where raycasting is hard to hit
                RaycastHit[] sphereCastHits = Physics.SphereCastAll(transform.position, sphereCastRadius, transform.forward, rayCastLength, rayCastMask, QueryTriggerInteraction.Ignore);
                if (sphereCastHits.Length > 0)
                {
                    Transform[] potentialGrabbaleItems = Array.ConvertAll(sphereCastHits, s => s.transform);
                    if (CheckForNearest(potentialGrabbaleItems, transform.position))
                        return;
                }
            }

            if (showDebug)
            {
                Debug.DrawRay(transform.position, transform.forward * rayCastLength, Color.cyan);
                if (rayCastDebugPosition != Vector3.zero)
                {
                    debugOverLapSphere.gameObject.SetActive(true);
                    debugOverLapSphere.transform.position = rayCastDebugPosition;
                }
                else
                    debugOverLapSphere.gameObject.SetActive(false);
            }


            StopHighlight(currentTarget);
            currentTarget = null;
        }

        private void ScaleOverlapSphere()
        {
            var distance = Vector3.Distance(transform.position, rayCastDebugPosition);
            overlapSphereRadius = Mathf.Lerp(overlapSphereRadiusMinSize, sphereStartingSize, distance / distanceStartShrinkingOverlap);

            sphereCastRadius = Mathf.Lerp(overlapSphereRadiusMinSize, sphereCastStartingSize, distance / distanceStartShrinkingOverlap);
        }

        private bool CheckForNearest(Transform[] hitObjects, Vector3 startingPoint)
        {
            if (hitObjects.Length > 0)
            {
                float nearestDistance = Single.PositiveInfinity;
                bool foundHit = false;
                XRGrabInteractable nearestGrabbable = null;

                foreach (var hit in hitObjects)
                {
                    if (!hit) continue;

                    //Check if item is within distance to cancel
                    if (Vector3.Distance(transform.position, hit.position) <= minDistanceToAllowGrab) continue;

                    var interactable = hit.transform.GetComponentInParent<XRGrabInteractable>();
                    //Check if interactable or if its being grabbed then ignore
                    if (!interactable || interactable.selectingInteractor || !interactable.enabled) continue;

                    //Check if allowed to DistanceGrab
                    var itemData = interactable.GetComponent<InteractableItemData>();
                    if (!itemData || !itemData.canDistanceGrab) continue;

                    //Check if anything blocking - Would be way better to using layerMasks
                    if (Physics.Raycast(hit.transform.position, (transform.position - hit.transform.position), out RaycastHit rayCastHit, 1, rayCastMask, QueryTriggerInteraction.Ignore))
                        if (rayCastHit.transform != interactable.transform)
                        {
                            if (showDebug)
                                Debug.DrawRay(hit.transform.position, (transform.position - hit.transform.position).normalized * 1, Color.magenta);
                            continue;
                        }

                    float distance = Vector3.Distance(hit.transform.position, startingPoint);

                    if (distance < nearestDistance)
                    {
                        foundHit = true;
                        nearestDistance = distance;
                        nearestGrabbable = hit.transform.GetComponentInParent<XRGrabInteractable>();
                    }
                }

                if (!foundHit) return false;

                SetNewCurrentTarget(nearestGrabbable.transform);
                return true;
            }

            return false;
        }

        private void SetNewCurrentTarget(Transform newTarget)
        {
            if (currentTarget == newTarget) return;
            if (currentTarget)
                StopHighlight(currentTarget);

            currentTarget = newTarget.transform;
            WristRotationReset();
            HighLight();
        }

        private void InitiateGrabStartFromTrigger()
        {
            if (isInputActivated && !isGripping)
            {
                isGripping = true;
                isActive = true;
                currentEasyModeTimer = 0;
                SetupLine();
            }
            else if (!isInputActivated)
            {
                isGripping = false;
                isActive = false;
                StopLine();
            }
        }

        private void Reset(XRBaseInteractable x)
        {
            CancelTarget(currentTarget);
            isLaunching = false;
            mainHandCollider.radius = mainHandColliderStartingSize;

            if (!x.TryGetComponent(out Rigidbody rb) || rb.isKinematic) return;
            rb.velocity = Vector3.zero;
        }


        private void TryToLaunchItem(Transform target)
        {
            if (!isActive) return;
            if (currentRotationMagnitude < rotationMagnitudeToLaunch) return;
            Launch(target);
        }

        private float currentEasyModeTimer;

        private void EasyModeLaunch(Transform target)
        {
            if (!isActive) return;
            currentEasyModeTimer += Time.deltaTime;
            if (currentEasyModeTimer < easyModeTimeTillGrab) return;
            currentEasyModeTimer = 0;
            Launch(target);
        }

        private void Launch(Transform target)
        {
            launchAudio.transform.position = target.position;
            launchAudio.Play();
            isLaunching = true;
            CancelTarget(target);

            StartCoroutine(SimulateProjectile(target));
        }


        private void CheckToCancel()
        {
            if (!currentTarget) return;
            var targetDirection = currentTarget.position - transform.position;
            var dot = Vector3.Dot(transform.forward, targetDirection.normalized);

            if (dot < dotProductCancel)
                StopLine();
        }


        private void HighLight()
        {
            var outline = currentTarget.GetComponentInChildren<XRQuickOutline>();
            if (outline) outline.HighlightWithColor(outlineColor);
        }

        private void StopHighlight(Transform target)
        {
            if (!target) return;
            var outline = currentTarget.GetComponentInChildren<XRQuickOutline>();
            if (outline) outline.StopHighlight();
        }

        private void CancelTarget(Transform target)
        {
            if (target)
            {
                StopHighlight(currentTarget);
            }

            StopLine();
            isActive = false;
        }

        private void SetupLine()
        {
            if (!currentTarget) return;
            lineEffect.Start(currentTarget);
            EnableOnActive.SetActive(true);
        }

        private void StopLine()
        {
            lineEffect.Stop();
            EnableOnActive.SetActive(false);
        }


        #region ItemLaunching

        [Header("Launch Wrist Flick")] [SerializeField] [Tooltip("How much wrist flick is required to launch values 0 to 1")]
        private float rotationMagnitudeToLaunch = .4f;

        [Header("Item Launching")] [SerializeField] [Tooltip("Main attribute to adjust flight time. Near 0 will be faster")]
        private float flightTimeMultiplier = .15f;

        [SerializeField] [Tooltip("Grows main collider on hand while item is in flight to allow easier grabbing")]
        private float mainHandColliderSizeGrow = .2f;

        [SerializeField] [Tooltip("Length of time collider is large after flight animation")]
        private float colliderLargeExtraTime = .25f;

        [SerializeField] [Tooltip("Distance in world Y to add to hand position")]
        private float verticalGoalAddOn = .1f;

        // [SerializeField] [Tooltip("Random rotation amount to add when item is flying")]
        // private float randomRotationSpeed = 4;

        [SerializeField] [Tooltip("How much velocity the item will have when reached hand")]
        private float velocitySpeedWhenFinished = 1f;

        [SerializeField] [Tooltip("Limits flight time from being too fast especially when the item distance is close to hand")]
        private float minFlightTime = .25f;

        [SerializeField] [Tooltip("Distance to Stop animation to hand, use when adding drag to pause item near hand")]
        private float distanceToStop = 0;

        [SerializeField] [Tooltip("Length of time to add drag to item when reaching hand")]
        private float dragTime = 0;

        [SerializeField] [Tooltip("Amount of drag to add after item reaches hand")]
        private float dragHoldAmount = 0;

        private Vector3 velocity;
        private float startingDrag;

        private IEnumerator SimulateProjectile(Transform target)
        {
            var rigidBody = target.GetComponent<Rigidbody>();
            startingDrag = rigidBody.drag;
            Vector3 goalPosition = transform.position + Vector3.up * verticalGoalAddOn;
            Vector3 startPosition = target.position;
            Quaternion startRotation = target.rotation;
            velocity = goalPosition - target.position;

            if(!rigidBody.isKinematic)
                rigidBody.angularVelocity = Vector3.zero;
            var newFlightTime = Mathf.Clamp(flightTimeMultiplier * velocity.magnitude, minFlightTime, 999);

            mainHandCollider.radius = mainHandColliderSizeGrow;

            float elapse_time = 0;
            while (elapse_time <= newFlightTime)
            {
                rigidBody.Sleep();

                float currentStep = elapse_time / (newFlightTime);

                //Position
                var slerpPosition = Vector3.Slerp(startPosition, goalPosition, currentStep);
                var lerpPosition = Vector3.Lerp(startPosition, goalPosition, currentStep);
                target.position = (slerpPosition + lerpPosition * 2) / 3;

                //Rotation
                target.rotation = Quaternion.Lerp(startRotation, transform.rotation, currentStep);

                if (Vector3.Distance(transform.position, target.position) < distanceToStop)
                    break;

                elapse_time += Time.deltaTime;
                TryToAutoGrab();
                yield return null;
            }

            StartCoroutine(ShrinkCollider());
            rigidBody.drag = dragHoldAmount;
            rigidBody.velocity = velocity * velocitySpeedWhenFinished;
            rigidBody.WakeUp();
            yield return new WaitForSeconds(dragTime);
            rigidBody.drag = startingDrag;
            isLaunching = false;
        }

        private IEnumerator ShrinkCollider()
        {
            yield return new WaitForSeconds(colliderLargeExtraTime);
            mainHandCollider.radius = mainHandColliderStartingSize;
        }

        #endregion

        #region WristFlicking

        private const int KSmoothingFrameCount = 20;
        private float smoothingDuration = 0.25f;
        private AnimationCurve smoothingCurve = AnimationCurve.Linear(1f, 1f, 1f, 0f);
        private int smoothingCurrentFrame;
        private float[] smoothingFrameTimes = new float[KSmoothingFrameCount];
        private Vector3[] smoothingAngularVelocityFrames = new Vector3[KSmoothingFrameCount];
        private Quaternion lastFrameRotation;
        private Vector3 currentSmoothedRotation;
        private float currentRotationMagnitude;

        private void WristRotationReset()
        {
            lastFrameRotation = transform.rotation;
            Array.Clear(smoothingFrameTimes, 0, smoothingFrameTimes.Length);
            Array.Clear(smoothingAngularVelocityFrames, 0, smoothingAngularVelocityFrames.Length);
            smoothingCurrentFrame = 0;
        }

        private void SetCurrentRotationMagnitude()
        {
            Vector3 smoothedAngularVelocity = getSmoothedVelocityValue(smoothingAngularVelocityFrames);
            currentSmoothedRotation = smoothedAngularVelocity;
            currentRotationMagnitude = currentSmoothedRotation.magnitude;
        }

        private void UpdateRotationFrames()
        {
            smoothingFrameTimes[smoothingCurrentFrame] = Time.time;

            Quaternion VelocityDiff = (transform.rotation * Quaternion.Inverse(lastFrameRotation));
            smoothingAngularVelocityFrames[smoothingCurrentFrame] = (new Vector3(Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.x),
                                                                         Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.y), Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.z))
                                                                     / Time.deltaTime) * Mathf.Deg2Rad;

            smoothingCurrentFrame = (smoothingCurrentFrame + 1) % KSmoothingFrameCount;
            lastFrameRotation = transform.rotation;
        }

        private Vector3 getSmoothedVelocityValue(Vector3[] velocityFrames)
        {
            Vector3 calcVelocity = new Vector3();
            int frameCounter = 0;
            float totalWeights = 0.0f;
            for (; frameCounter < KSmoothingFrameCount; frameCounter++)
            {
                int frameIdx = (((smoothingCurrentFrame - frameCounter - 1) % KSmoothingFrameCount) + KSmoothingFrameCount) % KSmoothingFrameCount;
                if (smoothingFrameTimes[frameIdx] == 0.0f)
                    break;

                float timeAlpha = (Time.time - smoothingFrameTimes[frameIdx]) / smoothingDuration;
                float velocityWeight = smoothingCurve.Evaluate(Mathf.Clamp(1.0f - timeAlpha, 0.0f, 1.0f));
                calcVelocity += velocityFrames[frameIdx] * velocityWeight;
                totalWeights += velocityWeight;
                if (Time.time - smoothingFrameTimes[frameIdx] > smoothingDuration)
                    break;
            }

            if (totalWeights > 0.0f)
                return calcVelocity / totalWeights;
            else
                return Vector3.zero;
        }

        #endregion

        private enum ControllerInput
        {
            triggerButton = 0,
            gripButton = 1,
        };

        // Mapping of the above InputAxes to actual common usage values
        private static readonly InputFeatureUsage<bool>[] InputAxesToCommonUsage =
        {
            CommonUsages.triggerButton,
            CommonUsages.gripButton
        };

        private void TryToAutoGrab()
        {
            if (!autoGrabIfGripping) return;
            if (directInteractor.selectTarget) return;
            if (Vector3.Distance(currentTarget.position, transform.position) >= distanceToAutoGrab) return;

            if (!isInputActivated) return;

            StopAllCoroutines();
            currentTarget.transform.SetPositionAndRotation(directInteractor.transform.position, directInteractor.transform.rotation);
            StartCoroutine(GrabItem(directInteractor, currentTarget.GetComponent<XRBaseInteractable>()));
        }

        IEnumerator GrabItem(XRBaseInteractor currentInteractor, XRBaseInteractable interactable)
        {
            yield return new WaitForFixedUpdate();
            Reset(interactable);
            mainHandCollider.radius = mainHandColliderStartingSize;
            if (currentInteractor.selectTarget || directInteractor.selectTarget) yield break;
            interactionManager.SelectEnter(currentInteractor, interactable);
        }
    }
}