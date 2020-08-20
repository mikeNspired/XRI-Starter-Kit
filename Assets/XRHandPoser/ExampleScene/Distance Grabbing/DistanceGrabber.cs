using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class DistanceGrabber : MonoBehaviour
    {
        [Header("Main")] [SerializeField] private XRController controller = null;
        [SerializeField] private XRDirectInteractor directInteractor = null;
        [SerializeField] private ControllerInput buttonControllerInput = ControllerInput.gripButton;
        [SerializeField] private DistanceGrabberLineBender lineEffect;
        [SerializeField] private AudioRandomize launchAudio = null;
        [SerializeField] private SphereCollider mainHandCollider = null;
        [SerializeField] private float mainHandColliderSizeGrow;
        [SerializeField] private bool easyModeGrabNoWristFlick;
        [SerializeField] private float easyModeTimeTillGrab;

        [Header("Item Searching")] [SerializeField] [Tooltip("How far RayCast will go")]
        private float rayCastLength = 10;

        [SerializeField] [Tooltip("Size of sphere that is created where rayCast hit. Items inside this sphere are potential grabbable items")]
        private float overlapSphereRadius = 1;

        [SerializeField] private LayerMask raycastMask;

        [Header("Line Canceling")] [SerializeField] [Tooltip("When to cancel trying to grab item based on rotation. A value of 0 lets you rotate this perpendicular to pointing at the item before canceling.")]
        private float dotProductCancel = .2f;

        private RaycastHit[] rayCastHits;
        private Transform currentTarget;
        private bool isActive = false, isLaunching = false;
        private float mainHandColliderStartingSize;

        public enum ControllerInput
        {
            triggerButton = 0,
            gripButton = 1,
        };

        // Mapping of the above InputAxes to actual common usage values
        private static readonly InputFeatureUsage<bool>[] InputAxesToCommonUsage =
        {
            CommonUsages.triggerButton,
            CommonUsages.gripButton,
        };

        private bool isGripping;


        // Start is called before the first frame update
        void Start()
        {
            OnValidate();
            WristRotationReset();
            directInteractor.onSelectEnter.AddListener(SetGravityOnCurrentTarget);
            if (mainHandCollider)
                mainHandColliderStartingSize = mainHandCollider.radius;
        }

        private void OnValidate()
        {
            if (!lineEffect)
                lineEffect = GetComponent<DistanceGrabberLineBender>();
            if (!controller)
                controller = GetComponentInParent<XRController>();
            if (!directInteractor)
                directInteractor = GetComponentInParent<XRDirectInteractor>();
        }


        private void Update()
        {
            if (isLaunching) return;

            //Check if controller is holding an item already
            if (directInteractor.selectTarget)
            {
                CancelTarget(currentTarget);
                return;
            }

            SearchWithRayCast();
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

        private void SearchWithRayCast()
        {
            if (isActive) return;

            Debug.DrawRay(transform.position, transform.forward * rayCastLength, Color.cyan);


            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit rayCastHit, rayCastLength, raycastMask))
            {
                Collider[] closestHits = Physics.OverlapSphere(rayCastHit.point, overlapSphereRadius, raycastMask);
                bool foundHit = false;
                float nearestDistance = Single.PositiveInfinity;
                XRGrabInteractable nearestGrabbable = null;
                if (closestHits.Length > 0)
                {
                    foreach (var hit in closestHits)
                    {
                        if (!hit.transform) continue;
                        var interactable = hit.transform.GetComponentInParent<XRGrabInteractable>();
                        //Check if interactable or if its being grabbed then ignore
                        if (!interactable || interactable.selectingInteractor) continue;

                        //Check if a distance grabbable item
                        var itemData = interactable.GetComponent<InteractableItemData>();
                        if (!itemData || !itemData.canDistanceGrab) continue;

                        float distance = Vector3.Distance(hit.transform.position, rayCastHit.point);
                        if (distance < nearestDistance)
                        {
                            foundHit = true;
                            nearestDistance = distance;
                            nearestGrabbable = hit.transform.GetComponentInParent<XRGrabInteractable>();
                        }
                    }

                    if (foundHit)
                    {
                        SetNewCurrentTarget(nearestGrabbable.transform);
                        return;
                    }
                }
            }


            if (!currentTarget) return;
            StopHighlight(currentTarget);
            currentTarget = null;
        }

        private void SetNewCurrentTarget(Transform newTarget)
        {
            if (currentTarget)
                StopHighlight(currentTarget);

            currentTarget = newTarget.transform;
            WristRotationReset();
            HighLight();
        }

        private void InitiateGrabStartFromTrigger()
        {
            var inputDevice = controller.inputDevice;
            if (!inputDevice.TryGetFeatureValue(InputAxesToCommonUsage[(int) buttonControllerInput], out bool gripValue)) return;

            if (gripValue && !isGripping)
            {
                isGripping = true;
                isActive = true;
                currentEasyModeTimer = 0;
                SetupLine();
            }
            else if (!gripValue)
            {
                isGripping = false;
                isActive = false;
                StopLine();
            }
        }

        private void SetGravityOnCurrentTarget(XRBaseInteractable arg0)
        {
            if (currentTarget)
            {
                currentTarget.GetComponent<Rigidbody>().useGravity = true;
            }
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
            launchAudio.PlaySound();
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
                CancelTarget(currentTarget);
        }


        private void HighLight()
        {
            var highlight = currentTarget.GetComponent<Highlight>();
            if (highlight)
                highlight.HighlightMesh();
        }

        private void StopHighlight(Transform target)
        {
            var highlight = target.GetComponent<Highlight>();
            if (highlight)
                highlight.RemoveHighlight();
        }

        private void CancelTarget(Transform target)
        {
            if (target)
            {
                StopHighlight(currentTarget);
                target.GetComponent<Rigidbody>().useGravity = true;
                target.GetComponent<Rigidbody>().drag = 0;
            }

            lineEffect.Stop();
            isActive = false;
        }

        private void SetupLine()
        {
            lineEffect.Start(currentTarget);
        }

        private void StopLine()
        {
            lineEffect.Stop();
        }


        #region ItemLaunching

        [Header("Launch Wrist Flick")] [SerializeField] [Tooltip("How much wrist flick is required to launch values 0 to 1")]
        private float rotationMagnitudeToLaunch = .4f;

        [Header("Item Launching")] [SerializeField] [Tooltip("Main attribute to adjust flight time. Near 0 will be faster")]
        private float flightTimeMultiplier = .15f;

        [SerializeField] [Tooltip("Distance in world Y to add to hand position")]
        private float verticalGoalAddOn = .1f;

        [SerializeField] [Tooltip("Random rotation amount to add when item is flying")]
        private float randomRotationSpeed = 4;

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

        private IEnumerator SimulateProjectile(Transform target)
        {
            var rigidBody = target.GetComponent<Rigidbody>();

            float elapse_time = 0;
            rigidBody.useGravity = false;
            Vector3 goalPosition = transform.position + Vector3.up * verticalGoalAddOn;
            Vector3 startPosition = target.position;
            Quaternion startRotation = target.rotation;

            velocity = goalPosition - target.position;

            //Add velocity for when the lerping action stops the item keeps moving
            rigidBody.velocity = velocity * velocitySpeedWhenFinished;

            //Add some randomRotation to item
            // rigidBody.angularVelocity = UnityEngine.Random.onUnitSphere * randomRotationSpeed;
            rigidBody.angularVelocity = Vector3.zero;
            var newFlightTime = Mathf.Clamp(flightTimeMultiplier * velocity.magnitude, minFlightTime, 999);

            mainHandCollider.radius = mainHandColliderSizeGrow;

            while (elapse_time <= newFlightTime)
            {
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
                yield return null;
            }

            mainHandCollider.radius = mainHandColliderStartingSize;

            rigidBody.drag = dragHoldAmount;
            yield return new WaitForSeconds(dragTime);
            rigidBody.drag = 0;

            rigidBody.useGravity = true;
            isLaunching = false;
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

        void WristRotationReset()
        {
            lastFrameRotation = transform.rotation;
            Array.Clear(smoothingFrameTimes, 0, smoothingFrameTimes.Length);
            Array.Clear(smoothingAngularVelocityFrames, 0, smoothingAngularVelocityFrames.Length);
            smoothingCurrentFrame = 0;
        }

        void SetCurrentRotationMagnitude()
        {
            Vector3 smoothedAngularVelocity = getSmoothedVelocityValue(smoothingAngularVelocityFrames);
            currentSmoothedRotation = smoothedAngularVelocity;
            currentRotationMagnitude = currentSmoothedRotation.magnitude;
        }

        void UpdateRotationFrames()
        {
            smoothingFrameTimes[smoothingCurrentFrame] = Time.time;


            Quaternion VelocityDiff = (transform.rotation * Quaternion.Inverse(lastFrameRotation));
            smoothingAngularVelocityFrames[smoothingCurrentFrame] = (new Vector3(Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.x),
                                                                         Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.y), Mathf.DeltaAngle(0, VelocityDiff.eulerAngles.z))
                                                                     / Time.deltaTime) * Mathf.Deg2Rad;

            smoothingCurrentFrame = (smoothingCurrentFrame + 1) % KSmoothingFrameCount;
            lastFrameRotation = transform.rotation;
        }

        Vector3 getSmoothedVelocityValue(Vector3[] velocityFrames)
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
    }
}