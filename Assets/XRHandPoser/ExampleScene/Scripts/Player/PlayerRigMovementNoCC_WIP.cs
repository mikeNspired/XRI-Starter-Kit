using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerRigMovementNoCC_WIP : MonoBehaviour
{
    [SerializeField] private XRController controller = null;
    [SerializeField] private SnapTurnProvider.InputAxes buttonInput = SnapTurnProvider.InputAxes.Primary2DAxis;
    private XRRig xrRig;
    private GameObject head;

    [Tooltip("How fast or far the player will move")]
    public float MovementSpeed = 1;

    [Tooltip("Acceleration to max movement speed on SmoothMovement")] [SerializeField]
    private float movementAcceleration = 6;

    [Tooltip("How fast or slow you will fall multiplied by physics gravity")]
    public float GravityMultiplier = 1;

    [SerializeField] [Tooltip("The dead zone that the controller movement will have to be above for instantaneous movement.")]
    private float instantMoveInputDeadZone = 0.85f;

    [SerializeField] [Tooltip("The dead zone that the controller movement will have to be above for smoothMovement")]
    private float smoothMoveInputDeadZone = 0.15f;

    [SerializeField] [Tooltip("Disables smooth locomotion and provides small instant movements dependant on MovementSpeed")]
    private bool instantMove = false;

    [SerializeField] [Tooltip("Disables movement if the ground's rigid body does not contain the 'TeleportationArea.cs', Ensures the player only stays on ground used for teleporting")]
    private bool moveOnlyOnTeleportArea = true;

    [SerializeField] [Tooltip("Instead of forward being the head direction, forward will be in the controller direction")]
    private bool moveInControllerDirection = false;

    [SerializeField] [Tooltip("Instead of forward being the head direction, forward will be in the controller direction")]
    private bool applyFallGravity = false;

    [SerializeField] [Tooltip("Moves character only when the pad is clicked instead of touched")]
    private bool moveOnlyOnPadClick = true;

    [SerializeField] private float maxStepHeight = .1f;

    // Mapping of the above InputAxes to actual common usage values
    private static readonly InputFeatureUsage<Vector2>[] InputAxesToCommonUsage =
    {
        CommonUsages.primary2DAxis,
        CommonUsages.secondary2DAxis,
    };

    private void OnValidate()
    {
        if (!xrRig) xrRig = GetComponent<XRRig>();
        if (!head) head = xrRig.cameraGameObject;
    }

    private void Awake() => OnValidate();

    private void FixedUpdate()
    {
        Vector2 inputAxis = GetInputAxis();

        if (instantMove)
            TryInstantMovement(inputAxis);
        else
            TrySmoothMove(inputAxis);

        if (applyFallGravity)
            ApplyGravity();
    }

    private Vector2 GetInputAxis()
    {
        Vector2 inputAxis = Vector2.zero;

        if (!moveOnlyOnPadClick)
        {
            controller.inputDevice.TryGetFeatureValue(InputAxesToCommonUsage[(int) buttonInput], out inputAxis);
            return inputAxis;
        }

        if (InputAxesToCommonUsage[(int) buttonInput] == CommonUsages.primary2DAxis)
        {
            controller.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool onClick);
            if (onClick)
                controller.inputDevice.TryGetFeatureValue(InputAxesToCommonUsage[(int) buttonInput], out inputAxis);
        }
        else if (InputAxesToCommonUsage[(int) buttonInput] == CommonUsages.secondary2DAxis)
        {
            controller.inputDevice.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out bool onClick);
            if (onClick)
                controller.inputDevice.TryGetFeatureValue(InputAxesToCommonUsage[(int) buttonInput], out inputAxis);
        }

        return inputAxis;
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
                    Move(moveDirection);
            }

            isMoving = true;
            return;
        }

        isMoving = false;
    }

    private void Move(Vector3 moveDirection)
    {
        Vector3 startPos = transform.position + Vector3.up * maxStepHeight + moveDirection;
        Debug.DrawRay(startPos, (Vector3.down) * 2, Color.magenta, 1);
        if (Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, maxStepHeight * 2))
        {
            transform.position = hit.point;
        }
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
                Move(moveDirection);
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

        //Multiply direction times input axis to get movement Direction
        return direction * new Vector3(position.x, 0, position.y);
    }

    private float fallSpeed;
    [SerializeField] private LayerMask layerMask;

    private void ApplyGravity()
    {
        if (isGrounded())
        {
            fallSpeed = 0;
            return;
        }

        //Accelerate falling speed over time
        fallSpeed += Physics.gravity.y * Time.fixedDeltaTime;
        if (Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, out RaycastHit hit, .5f))
        {
            transform.position = hit.point;
        }
        else
            transform.position = transform.position + Vector3.up * fallSpeed;
    }

    private bool isGrounded()
    {
        Vector3 startPos = transform.position + Vector3.up;
        return Physics.SphereCast(startPos, .1f, Vector3.down, out RaycastHit hit, 1, layerMask);
    }

    private bool CheckIfTeleportationGround(Vector3 moveDirection)
    {
        if (!moveOnlyOnTeleportArea) return true;

        Vector3 startPos = transform.position + Vector3.up * maxStepHeight + moveDirection;

        return Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, maxStepHeight * 2)
            ? hit.collider.GetComponentInParent<TeleportationArea>()
            : false;
    }
}