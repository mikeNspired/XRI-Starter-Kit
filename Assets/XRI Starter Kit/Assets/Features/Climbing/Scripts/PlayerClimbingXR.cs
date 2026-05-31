using System.Collections;
using XR.Interaction.Toolkit.Samples;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion; 

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Simple climbing approach that also supports grabbing/moving with dynamic objects
    /// by applying the object's movement delta to the player.
    /// </summary>
    [AddComponentMenu("XR/Locomotion/Player Climbing XR")]
    public class PlayerClimbingXR : LocomotionProvider
    {
        [Header("References")] [SerializeField]
        private XRInteractionManager xrInteractionManager;

        [SerializeField] private DynamicMoveProvider playerMovement;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private LayerMask checkGroundLayerMask = 1; // Not used in this minimal snippet

        [Header("Climb Speed")] [SerializeField]
        private float oneHandClimbSpeed = 0.6f;

        [SerializeField] private float twoHandClimbSpeed = 1f;

        [Header("Return To Old Location On Previous Hand Release")] [SerializeField]
        private float returnDistance = 0.1f;

        [SerializeField] private AnimationCurve returnToPlayerCurve = AnimationCurve.Linear(1f, 1f, 1f, 0f);
        [SerializeField] private float returnAnimationLength = 0.25f;

        [Header("Launching")] [SerializeField] private float launchSpeedMultiplier = 2f;
        [SerializeField] private Vector3 launchVelocityDrag = new Vector3(0.1f, 0.1f, 0.1f);

        private ControllerInputActionManager climbingHand;
        private ControllerInputActionManager previousHand;

        private Vector3 overPosition = Vector3.zero;
        private Vector3 prevLocation = Vector3.zero;
        private float climbSpeed;

        // For "launch" motion after letting go
        private Vector3 launchVelocity = Vector3.up;

        private bool isClimbing;
        private bool isReturningPlayer;

        // -- NEW FIELDS FOR DYNAMIC OBJECT MOVEMENT --
        private Transform grabbedMovingObject = null;
        private Vector3 lastObjectPos;
        private Quaternion lastObjectRot;

        private void Start() => OnValidate();

        private void OnValidate()
        {
            if (!mediator)
                mediator = GetComponent<LocomotionMediator>();

            if (!playerMovement)
                playerMovement = GetComponent<DynamicMoveProvider>();

            if (!xrOrigin)
                xrOrigin = GetComponentInParent<XROrigin>();

            if (!xrInteractionManager)
                xrInteractionManager = FindFirstObjectByType<XRInteractionManager>();

            if (!characterController)
                characterController = FindFirstObjectByType<CharacterController>();
        }

        private void Update()
        {
            // If grounded and not climbing, restore gravity & zero launch velocity
            if (characterController && characterController.isGrounded && !isClimbing)
            {
                playerMovement.useGravity = true;
                launchVelocity = Vector3.zero;
                return;
            }

            // If actively climbing or we have an "overPosition," skip applying launch motion
            if (isClimbing || overPosition != Vector3.zero)
                return;

            // Apply "launch" motion with drag
            launchVelocity.x /= 1 + launchVelocityDrag.x * Time.deltaTime;
            launchVelocity.y += Physics.gravity.y * Time.deltaTime;
            launchVelocity.z /= 1 + launchVelocityDrag.z * Time.deltaTime;

            characterController?.Move(launchVelocity * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!isClimbing)
                return;

            // If mediator says "Preparing," try to start locomotion right now
            if (locomotionState == LocomotionState.Preparing)
                TryStartLocomotionImmediately();

            // If mediator says "Moving," keep climbing
            if (locomotionState == LocomotionState.Moving)
            {
                ApplyMovingObjectDelta();

                // Then apply your usual "pull the player" climbing logic
                Climb();
            }
        }

        #region Public Climb Interface

        /// <summary>
        /// Called when a new climbing hand grabs a climbable object.
        /// Overload that sets which object is being grabbed (for dynamic movement).
        /// </summary>
        public void SetClimbHand(ControllerInputActionManager controller, Transform grabbedObject)
        {
            // Store the transform so we can track its position changes
            grabbedMovingObject = grabbedObject;
            lastObjectPos = grabbedObject.position;
            lastObjectRot = grabbedObject.rotation;

            // Original logic
            SetClimbHand(controller);
        }

        /// <summary>
        /// Called when a new climbing hand grabs a climbable object (static usage).
        /// Existing method so older logic doesn't break; 
        /// can still be used by static climb points.
        /// </summary>
        public void SetClimbHand(ControllerInputActionManager controller)
        {
            ClimbingStarted();

            // Example: If your hand has "ClimbingStamina" logic
            var stamina = controller.GetComponentInParent<HandReference>().Hand.GetComponent<ClimbingStamina>();
            stamina.Activate();
            stamina.OutOfStamina.AddListener(CancelClimbing);

            // Record the player's position before they move (for return logic)
            prevLocation = xrOrigin.transform.position;

            // If there's already a climbingHand, push that into 'previousHand'
            if (climbingHand)
                previousHand = climbingHand;

            // Set the new "climbingHand" to our new controller
            climbingHand = controller;

            // Adjust climb speed based on whether we have 1 or 2 hands
            AdjustMoveSpeed();
        }

        /// <summary>
        /// Called when one climbing hand is released.
        /// </summary>
        public void RemoveClimbHand(ControllerInputActionManager controller)
        {
            // Deactivate stamina logic
            var stamina = controller.GetComponentInChildren<ClimbingStamina>();
            stamina.Deactivate();
            stamina.OutOfStamina.RemoveListener(CancelClimbing);

            // If that was our active climbing hand, revert to 'previousHand' if available
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

            // If that was the "previousHand," just clear it
            if (previousHand == controller)
                previousHand = null;

            AdjustMoveSpeed();

            // If no hands left, end climbing
            if (previousHand == null && climbingHand == null)
            {
                // Clear reference to the grabbed object
                grabbedMovingObject = null;
                ClimbingEnded();
            }
        }

        /// <summary>
        /// Cancel climbing (e.g., out of stamina).
        /// </summary>
        public void CancelClimbing()
        {
            ClimbingEnded();

            // Release the previous hand’s climb, if still selected
            if (previousHand)
            {
                var prevInteractor = previousHand.GetComponentInChildren<XRBaseInteractor>();
                if (prevInteractor && prevInteractor.interactablesSelected.Count > 0)
                {
                    var selectedInteractable = prevInteractor.interactablesSelected[0];
                    xrInteractionManager.SelectExit(prevInteractor, selectedInteractable);
                    Debug.Log("Released Prev Hand");
                }
            }

            // Release the current climbing hand’s climb
            if (climbingHand)
            {
                var climbInteractor = climbingHand.GetComponentInChildren<XRBaseInteractor>();
                if (climbInteractor && climbInteractor.interactablesSelected.Count > 0)
                {
                    var selectedInteractable = climbInteractor.interactablesSelected[0];
                    xrInteractionManager.SelectExit(climbInteractor, selectedInteractable);
                    Debug.Log("Released Climbing Hand");
                }
            }

            climbingHand = null;
            previousHand = null;
            grabbedMovingObject = null;
        }

        /// <summary>
        /// If the player flings their hand on release, we can set a "launch" velocity.
        /// </summary>
        public void SetReleasedVelocity(Vector3 controllerVelocityCurrentSmoothedVelocity)
        {
            if (isClimbing)
                return;

            playerMovement.useGravity = false;
            launchVelocity = controllerVelocityCurrentSmoothedVelocity * launchSpeedMultiplier;
        }

        #endregion

        #region Internal Climb Logic

        private void ClimbingStarted()
        {
            launchVelocity = Vector3.zero;
            isClimbing = true;

            // Turn off gravity from our MoveProvider while climbing
            playerMovement.useGravity = false;

            // Request that the LocomotionProvider go from Idle -> Preparing
            if (!isLocomotionActive)
                TryPrepareLocomotion();
        }

        private void ClimbingEnded()
        {
            // Re-enable gravity
            playerMovement.useGravity = true;
            isClimbing = false;

            // End Locomotion
            if (isLocomotionActive)
                TryEndLocomotion();

            // If we have an “overPosition,” move the camera
            if (overPosition != Vector3.zero)
                MoveToPositionWhenReleased();

            overPosition = Vector3.zero;
        }

        /// <summary>
        /// Move the player while climbing by reading the active climbing hand’s velocity
        /// and moving in the opposite direction.
        /// </summary>
        private void Climb()
        {
            // Identify which controller is climbing, gather velocity
            var xrNode = GetClimbingHandNode();
            InputDevices.GetDeviceAtXRNode(xrNode)
                .TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 velocity);

            // Move in the opposite direction of the hand's velocity
            if (!isReturningPlayer && characterController)
            {
                characterController.Move(transform.rotation * -velocity * (Time.fixedDeltaTime * climbSpeed));
            }
        }

        private XRNode GetClimbingHandNode()
        {
            if (climbingHand == null)
                return XRNode.LeftHand;

            return climbingHand.GetComponentInParent<HandReference>().LeftRight == LeftRight.Left
                ? XRNode.LeftHand
                : XRNode.RightHand;
        }

        /// <summary>
        /// 1 or 2 hands determines the climb speed.
        /// </summary>
        private void AdjustMoveSpeed() =>
            climbSpeed = previousHand ? twoHandClimbSpeed : oneHandClimbSpeed;

        /// <summary>
        /// If the player is too far from the original position of the last hand, we return them.
        /// </summary>
        private void CheckIfReturnToHand()
        {
            if (Vector3.Distance(xrOrigin.transform.position, prevLocation) >= returnDistance)
                StartCoroutine(ReturnToPrevHandPosition());
        }

        private IEnumerator ReturnToPrevHandPosition()
        {
            isReturningPlayer = true;

            float currentTimer = 0f;
            var startPosition = xrOrigin.transform.position;
            var goalPosition = prevLocation;

            while (currentTimer < returnAnimationLength)
            {
                float t = currentTimer / returnAnimationLength;
                xrOrigin.transform.position =
                    Vector3.Lerp(startPosition, goalPosition, returnToPlayerCurve.Evaluate(t));

                currentTimer += Time.deltaTime;
                yield return null;
            }

            isReturningPlayer = false;
        }

        private void MoveToPositionWhenReleased()
        {
            // Move the camera to overPosition + camera height
            var heightAdjustment = xrOrigin.transform.up * xrOrigin.CameraInOriginSpaceHeight;
            var cameraDestination = overPosition + heightAdjustment;
            xrOrigin.MoveCameraToWorldLocation(cameraDestination);
        }

        /// <summary>
        /// Called from FixedUpdate if we are climbing and have a grabbed object.
        /// </summary>
        private void ApplyMovingObjectDelta()
        {
            if (grabbedMovingObject == null || characterController == null)
                return;

            // 1) Calculate position delta
            Vector3 currentPos = grabbedMovingObject.position;
            Vector3 deltaPos = currentPos - lastObjectPos;

            // 2) Move the CharacterController by that delta
            characterController.Move(deltaPos);

            // (Optional) If you want to also follow the object's rotation:
            // Quaternion currentRot = grabbedMovingObject.rotation;
            // Quaternion deltaRot = currentRot * Quaternion.Inverse(lastObjectRot);
            // RotateRigAroundPivot(deltaRot);

            // 3) Update for next frame
            lastObjectPos = currentPos;
            // lastObjectRot = currentRot; // If also doing rotation
        }

        /// <summary>
        /// (Optional) Example method to rotate the rig around the camera pivot.
        /// Call this inside ApplyMovingObjectDelta if you want to track rotation.
        /// </summary>
        private void RotateRigAroundPivot(Quaternion deltaRot)
        {
            Vector3 pivot = xrOrigin.Camera.transform.position; // Or wherever the hand is
            Vector3 rigPos = xrOrigin.transform.position;
            Vector3 offset = rigPos - pivot;

            // Rotate offset
            offset = deltaRot * offset;

            // Move back
            xrOrigin.transform.position = pivot + offset;

            // Also rotate the rig
            xrOrigin.transform.rotation = deltaRot * xrOrigin.transform.rotation;
        }

        #endregion
    }
}