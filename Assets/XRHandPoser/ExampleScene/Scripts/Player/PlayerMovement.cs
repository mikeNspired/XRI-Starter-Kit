using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public abstract class PlayerMovement : LocomotionProvider
{
    [SerializeField] protected XRController controller = null;
    [SerializeField] public InputAxes buttonInput = InputAxes.Primary2DAxis;
    protected XRRig xrRig;
    protected GameObject head;

    [Tooltip("How fast or far the player will move")]
    public float MovementSpeed = 1;

    [Tooltip("Acceleration to max movement speed on SmoothMovement")] [SerializeField]
    protected float movementAcceleration = 6;

    [Tooltip("How fast or slow you will fall multiplied by physics gravity")]
    public float GravityMultiplier = 1;

    [SerializeField] [Tooltip("The dead zone that the controller movement will have to be above for instantaneous movement.")]
    protected float instantMoveInputDeadZone = 0.85f;

    [SerializeField] [Tooltip("The dead zone that the controller movement will have to be above for smoothMovement")]
    protected float smoothMoveInputDeadZone = 0.15f;

    [SerializeField] [Tooltip("Disables smooth locomotion and provides small instant movements dependant on MovementSpeed")]
    protected bool instantMove = false;

    [SerializeField] [Tooltip("Disables movement if the ground's rigid body does not contain the 'TeleportationArea.cs', Ensures the player only stays on ground used for teleporting")]
    protected bool moveOnlyOnTeleportArea = true;

    [SerializeField] [Tooltip("Instead of forward being the head direction, forward will be in the controller direction")]
    protected bool moveInControllerDirection = false;

    [SerializeField] [Tooltip("Moves character only when the pad is clicked instead of touched")]
    public bool moveOnlyOnPadClick = true;

    [SerializeField] [Tooltip("")] private bool applyFallGravity = false;
    public bool pauseGravity = false;
    public bool pauseColliderAdjustment = false;

    public bool MoveOnlyOnTeleportArea => moveOnlyOnTeleportArea;
    protected float fallSpeed;
    protected float currentMovementSpeed;
    private bool isMoving; //Used to stop the movement after a single press

    public enum InputAxes
    {
        Primary2DAxis = 0,
        Secondary2DAxis = 1
    }

    // Mapping of the above InputAxes to actual common usage values
    protected static readonly InputFeatureUsage<Vector2>[] InputAxesToCommonUsage =
    {
        CommonUsages.primary2DAxis,
        CommonUsages.secondary2DAxis,
    };

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

    protected override void Awake() => OnValidate();
    
    protected void OnValidate()
    {
        if (!gameObject.activeInHierarchy) return;
        if (!xrRig) xrRig = GetComponentInParent<XRRig>();
        if (!head) head = xrRig.cameraGameObject;
    }

    protected virtual void LateUpdate()
    {
        Vector2 inputAxis = GetInputAxis();

        if (!CanBeginLocomotion()) return;

        if (instantMove)
            TryInstantMovement(inputAxis);
        else
            TrySmoothMove(inputAxis);

        if (applyFallGravity)
            ApplyGravity();
    }

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
        }
        else
            isMoving = false;
    }

    protected void TrySmoothMove(Vector2 inputAxis)
    {
        //Used for joysticks that are not zero when not being touched
        if ((Mathf.Abs(inputAxis.x) > smoothMoveInputDeadZone) || (Mathf.Abs(inputAxis.y) > smoothMoveInputDeadZone))
        {
            currentMovementSpeed = Mathf.Lerp(currentMovementSpeed, MovementSpeed, Time.deltaTime * movementAcceleration);

            Vector3 moveDirection = GetMoveDirection(inputAxis) * (Time.deltaTime * currentMovementSpeed);
            if (CheckIfTeleportationGround(moveDirection))
                Move(moveDirection);
        }
        else
            currentMovementSpeed = 0;
    }

    protected Vector3 GetMoveDirection(Vector2 position)
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

    protected virtual bool CheckIfTeleportationGround(Vector3 moveDirection)
    {
        return false;
    }

    public virtual void Move(Vector3 moveDirection)
    {
    }

    public virtual bool isGrounded()
    {
        return false;
    }
    public virtual void ShrinkColliderToHead()
    {
    }
    protected virtual void ApplyGravity()
    {
        if (pauseGravity) return;
        if (isGrounded())
        {
            fallSpeed = 0;
            return;
        }
        //Accelerate falling speed over time
        fallSpeed += Physics.gravity.y * Time.fixedDeltaTime;

        Move(fallSpeed * Time.fixedDeltaTime * GravityMultiplier * Vector3.up);
    }

   
    public void PauseColliderAdjustment() => pauseColliderAdjustment = true;
    public void ResumeColliderAdjustment() => pauseColliderAdjustment = false;
    public void PauseGravity() => pauseGravity = true;
    public void ResumeGravity() => pauseGravity = false;
}