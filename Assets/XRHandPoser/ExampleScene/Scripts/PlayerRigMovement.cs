using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerRigMovement : MonoBehaviour
{
    [SerializeField] private XRController controller;
    [SerializeField] private InputAxes TurningInput = InputAxes.Primary2DAxis;
    [SerializeField] private CharacterController characterController;
    private GameObject head;

    [Tooltip("How fast or far the player will move")]
    public float MovementSpeed = 1;

    [Tooltip("Acceleration to max movement speed on SmoothMovement")] [SerializeField]
    private float movementAcceleration;

    [Tooltip("How fast or slow you will fall multiplied by physics gravity")]
    public float GravityMultiplier = 1;

    [SerializeField] [Tooltip("The dead zone that the controller movement will have to be above for instantaneous movement.")]
    private float instantMoveInputDeadZone = 0.85f;

    [SerializeField] [Tooltip("The dead zone that the controller movement will have to be above for smoothMovement")]
    private float smoothMoveInputDeadZone = 0.15f;

    [SerializeField] [Tooltip("Disables smooth locomotion and provides small instant movements dependant on MovementSpeed")]
    private bool instantMove;

    [SerializeField] [Tooltip("Disables movement if the ground's rigid body does not contain the 'TeleportationArea.cs', Ensures the player only stays on ground used for teleporting")]
    private bool moveOnlyOnTeleportArea;

    [SerializeField] [Tooltip("Instead of forward being the head direction, forward will be in the controller direction")]
    private bool moveInControllerDirection;

    [SerializeField] [Tooltip("Instead of forward being the head direction, forward will be in the controller direction")]
    private bool fadeSpeed;

    private enum InputAxes
    {
        Primary2DAxis = 0,
        Secondary2DAxis = 1
    }

    // Mapping of the above InputAxes to actual common usage values
    private static readonly InputFeatureUsage<Vector2>[] InputAxesToCommonUsage =
    {
        CommonUsages.primary2DAxis,
        CommonUsages.secondary2DAxis
    };

    private void OnValidate()
    {
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!head) head = GetComponent<XRRig>().cameraGameObject;
    }

    private void Awake() => OnValidate();

    private void FixedUpdate()
    {
        AdjustCharacterControllerSizeToCamera();

        controller.inputDevice.TryGetFeatureValue(InputAxesToCommonUsage[(int) TurningInput], out Vector2 inputAxis);

        if (instantMove)
            TryInstantMovement(inputAxis);
        else
            TrySmoothMove(inputAxis);

        ApplyGravity();
    }


    private bool isMoving; //Used to stop the movement after a single press

    private void TryInstantMovement(Vector2 inputAxis)
    {
        if (Mathf.Abs(inputAxis.x) > instantMoveInputDeadZone || Mathf.Abs(inputAxis.y) > instantMoveInputDeadZone)
        {
            if (!isMoving)
            {
                Vector3 moveDirection = GetMoveDirection(inputAxis) * MovementSpeed;
                if (CheckIfTeleportationGround(moveDirection))
                    characterController.Move(moveDirection);
            }

            isMoving = true;
            return;
        }

        isMoving = false;
    }

    private float currentMovementSpeed;

    private void TrySmoothMove(Vector2 inputAxis)
    {
        //Used for joysticks that are not zero when not being touched
        if ((Mathf.Abs(inputAxis.x) > smoothMoveInputDeadZone) || (Mathf.Abs(inputAxis.y) > smoothMoveInputDeadZone))
        {
            currentMovementSpeed = Mathf.Lerp(currentMovementSpeed, MovementSpeed, Time.deltaTime * movementAcceleration);

            Vector3 moveDirection = GetMoveDirection(inputAxis) * (Time.deltaTime * currentMovementSpeed);
            if (CheckIfTeleportationGround(moveDirection))
            {
                characterController.Move(moveDirection);
            }
        }
        else currentMovementSpeed = 0;
    }

    private Vector3 GetMoveDirection(Vector2 position)
    {
        Quaternion direction;

        if (moveInControllerDirection)
        {
            //Get controller direction
            direction = Quaternion.Euler(0, controller.transform.eulerAngles.y, 0);
        }
        else //get head direction
            direction = Quaternion.Euler(0, head.transform.eulerAngles.y, 0);

        Debug.Log("Direction:" + direction + "Input: " + position);
        //Multiply direction times input axis to get movement Direction
        return direction * new Vector3(position.x, 0, position.y);
    }

    private void AdjustCharacterControllerSizeToCamera()
    {
        Vector3 headLocalPos = head.transform.localPosition;

        //Set height of collider to camera 
        characterController.height = headLocalPos.y;

        //Center characterController to camera
        characterController.center = new Vector3(headLocalPos.x, characterController.height / 2 + characterController.skinWidth, headLocalPos.z);
    }

    private float fallSpeed;

    private void ApplyGravity()
    {
        if (characterController.isGrounded) fallSpeed = 0;
        //Accelerate falling speed over time
        else fallSpeed += Physics.gravity.y * Time.fixedDeltaTime;

        characterController.Move(fallSpeed * Time.fixedDeltaTime * GravityMultiplier * Vector3.up);
    }

    // private bool isGrounded()
    // {
    //     Vector3 startPos = transform.TransformPoint(characterController.center);
    //     return Physics.SphereCast(startPos, characterController.radius, Vector3.down, out RaycastHit hit, characterController.center.y + .01f, layerMask);
    // }

    private bool CheckIfTeleportationGround(Vector3 moveDirection)
    {
        if (!moveOnlyOnTeleportArea) return true;

        Vector3 startPos = transform.TransformPoint(characterController.center) + moveDirection;
        Debug.DrawRay(startPos, Vector3.down * (characterController.center.y + .1f), Color.yellow);
        return Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, characterController.center.y + 1)
            ? hit.collider.attachedRigidbody?.GetComponent<TeleportationArea>()
            : false;
    }
}