using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class PlayerClimbingXR : LocomotionProvider
    {
        [SerializeField] private XRInteractionManager xrInteractionManager = null;

        [SerializeField] private ContinuousMoveProviderBase playerMovement = null;
        [SerializeField] private CharacterController characterController = null;
        [SerializeField] private XROrigin xrOrigin = null;
        [SerializeField] private LayerMask checkGroundLayerMask = 1;

        [Header("Climb Speed")] [SerializeField]
        private float oneHandClimbSpeed = .6f;

        [SerializeField] private float twoHandClimbSpeed = 1;

        private XRBaseController climbingHand, previousHand;
        private Vector3 overPosition = Vector3.zero;
        private float climbSpeed;

        [Header("Return To Old Location On Previous Hand Release")] [SerializeField]
        private float returnDistance = .1f;

        [SerializeField] private AnimationCurve returnToPlayerCurve = AnimationCurve.Linear(1f, 1f, 1f, 0f);
        [SerializeField] private float returnAnimationLength = .25f;
        private Vector3 prevLocation = Vector3.zero;

        [Header("Launching")] [SerializeField] private float launchSpeedMultiplier = 2;
        [SerializeField] private Vector3 launchVelocityDrag = new Vector3(.1f, .1f, .1f);
        private Vector3 launchVelocity = Vector3.up;


        private void Start()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            if (!gameObject.activeInHierarchy) return;
            if (!system)
                system = GetComponent<LocomotionSystem>();
            if (!playerMovement)
                playerMovement = GetComponent<ContinuousMoveProviderBase>();
            if (!xrOrigin)
                xrOrigin = GetComponentInParent<XROrigin>();
            if (!xrInteractionManager)
                xrInteractionManager = FindObjectOfType<XRInteractionManager>();
            if (!characterController)
                characterController = FindObjectOfType<CharacterController>();
        }

        public void Update()
        {
            if (characterController.isGrounded && !isClimbing)
            {
                playerMovement.useGravity = true;
                launchVelocity = Vector3.zero;
                return;
            }

            if (isClimbing || overPosition != Vector3.zero) return;
            launchVelocity.x /= 1 + launchVelocityDrag.x * Time.deltaTime;
            launchVelocity.y += Physics.gravity.y * Time.fixedDeltaTime;

            launchVelocity.z /= 1 + launchVelocityDrag.x * Time.deltaTime;
            characterController.Move(launchVelocity * Time.deltaTime);
        }
        
        private void FixedUpdate()
        {
            if (!isClimbing) return;

            BeginLocomotion();
            Climb();
        }

        public void SetClimbHand(XRBaseController controller)
        {
            ClimbingStarted();

            var stamina = controller.GetComponentInParent<HandReference>().Hand.GetComponent<ClimbingStamina>();
            stamina.Activate();
            stamina.OutOfStamina.AddListener(CancelClimbing);

            prevLocation = xrOrigin.transform.position;

            if (climbingHand)
                previousHand = climbingHand;

            climbingHand = controller;

            AdjustMoveSpeed();
        }

        private void AdjustMoveSpeed()
        {
            climbSpeed = previousHand ? twoHandClimbSpeed : oneHandClimbSpeed;
        }

        public void RemoveClimbHand(XRBaseController controller)
        {
            var stamina = controller.GetComponentInChildren<ClimbingStamina>();
            stamina.Deactivate();
            stamina.OutOfStamina.RemoveListener(CancelClimbing);

            if (climbingHand == controller)
            {
                climbingHand = null;
                if (previousHand)
                {
                    climbingHand = previousHand;
                    previousHand = null;
                    CheckIfReturnToHand();
                }
            }

            if (previousHand == controller)
                previousHand = null;

            AdjustMoveSpeed();

            if (previousHand == null && climbingHand == null)
                ClimbingEnded();
        }

        public void CancelClimbing()
        {
            ClimbingEnded();

            if (previousHand)
            {
                var prevInteractor = previousHand.GetComponentInChildren<XRDirectInteractor>();
                if (prevInteractor.hasSelection)
                {
                    xrInteractionManager.SelectExit(prevInteractor, prevInteractor.selectTarget);
                }
            }

            if (climbingHand)
            {
                var climbInteractor = climbingHand.GetComponentInChildren<XRDirectInteractor>();
                if (climbInteractor.hasSelection)
                {
                    xrInteractionManager.SelectExit(climbInteractor, climbInteractor.selectTarget);
                }
            }

            climbingHand = null;
            previousHand = null;
        }

        private bool isClimbing = false;

        private void ClimbingStarted()
        {
            launchVelocity = Vector3.zero;
            isClimbing = true;
            playerMovement.useGravity = false;
            //playerMovement.ShrinkColliderToHead();
        }

        private void ClimbingEnded()
        {
            playerMovement.useGravity = true;
            isClimbing = false;

            if (overPosition != Vector3.zero)
                MoveToPositionWhenReleased();
            EndLocomotion();

            // playerMovement.ResumeColliderAdjustment();
        }

     

        private XRNode GetClimbingHandNode()
        {
            return climbingHand.GetComponentInParent<HandReference>().LeftRight == LeftRight.Left ? XRNode.LeftHand : XRNode.RightHand;
        }

        private void Climb()
        {
            InputDevices.GetDeviceAtXRNode(GetClimbingHandNode()).TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 velocity);
            if (!isReturningPlayer)
                characterController.Move(transform.rotation * -velocity * (Time.fixedDeltaTime * climbSpeed));

            //CheckIfOverGround();
        }

        private void CheckIfReturnToHand()
        {
            if (Vector3.Distance(xrOrigin.transform.position, prevLocation) >= returnDistance)
                StartCoroutine(ReturnToPrevHandPosition());
        }

        private bool isReturningPlayer = false;

        private IEnumerator ReturnToPrevHandPosition()
        {
            float currentTimer = 0;
            Vector3 startPosition = xrOrigin.transform.position;
            Vector3 goalPosition = prevLocation;

            isReturningPlayer = true;
            while (currentTimer <= returnAnimationLength + Time.deltaTime)
            {
                xrOrigin.transform.position = Vector3.Lerp(startPosition, goalPosition, returnToPlayerCurve.Evaluate(currentTimer / returnAnimationLength));
                yield return null;
                currentTimer += Time.deltaTime;
            }

            isReturningPlayer = false;
        }

        private void OnDrawGizmos()
        {
            // Gizmos.DrawWireSphere(overPosition, .1f);
            // Vector3 heightAdjustment = xrOrigin.transform.up * xrOrigin.CameraInOriginSpaceHeight;
            // Vector3 cameraDestination = overPosition + heightAdjustment;
            // Gizmos.DrawWireSphere(cameraDestination, .1f);
        }

        private void MoveToPositionWhenReleased()
        {
            Vector3 heightAdjustment = xrOrigin.transform.up * xrOrigin.CameraInOriginSpaceHeight;
            Vector3 cameraDestination = overPosition + heightAdjustment;
            xrOrigin.MoveCameraToWorldLocation(cameraDestination);
            overPosition = Vector3.zero;
        }

        public void SetReleasedVelocity(Vector3 controllerVelocityCurrentSmoothedVelocity)
        {
            if (isClimbing) return;
            playerMovement.useGravity = false;
            launchVelocity = controllerVelocityCurrentSmoothedVelocity * launchSpeedMultiplier;
        }
    }
}