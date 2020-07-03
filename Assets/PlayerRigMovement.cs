using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerRigMovement : MonoBehaviour
{
    public XRNode controllerType;
    public InputAxes TurningInput = InputAxes.Primary2DAxis;
    private CharacterController characterController;
    private GameObject head;

    public float speed = 1;
    public float gravityMultiplier = 1;

    [SerializeField] [Tooltip("The deadzone that the controller movement will have to be above for instantaneous movement.")]
    private float instantMoveInputDeadZone = 0.85f;
    [SerializeField] [Tooltip("The deadzone that the controller movement will have to be above for smoothMovement")]
    private float smoothMoveInputDeadZone = 0.15f;

    [SerializeField] private LayerMask layerMask = 1;

    [SerializeField] private bool instantMove;
    [SerializeField] private bool moveOnlyOnTeleportArea;

    public enum InputAxes
    {
        Primary2DAxis = 0,
        Secondary2DAxis = 1,
    };

    // Mapping of the above InputAxes to actual common usage values
    static readonly InputFeatureUsage<Vector2>[] InputAxesToCommonUsage = new InputFeatureUsage<Vector2>[]
    {
        CommonUsages.primary2DAxis,
        CommonUsages.secondary2DAxis,
    };

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        head = GetComponent<XRRig>().cameraGameObject;
    }

    private void FixedUpdate()
    {
        PositionController();

        if (instantMove)
            CheckForInputInstantMovement();
        else
            CheckForInput();

        ApplyGravity();
    }


    private bool isMoving;


    private void CheckForInputInstantMovement()
    {
        InputDevices.GetDeviceAtXRNode(controllerType).TryGetFeatureValue(InputAxesToCommonUsage[(int) TurningInput], out Vector2 inputAxis);

        if (Mathf.Abs(inputAxis.x) > instantMoveInputDeadZone || Mathf.Abs(inputAxis.y) > instantMoveInputDeadZone)
        {
            if (!isMoving)
                StartMove(inputAxis);
            isMoving = true;
            return;
        }

        isMoving = false;
    }

    private void CheckForInput()
    {
        InputDevices.GetDeviceAtXRNode(controllerType).TryGetFeatureValue(InputAxesToCommonUsage[(int) TurningInput], out Vector2 inputAxis);
        if (Mathf.Abs(inputAxis.x) > smoothMoveInputDeadZone && !(Mathf.Abs(inputAxis.y) > smoothMoveInputDeadZone))
        StartMove(inputAxis);
    }

    private void StartMove(Vector2 position)
    {
        Quaternion headDirection = Quaternion.Euler(0, head.transform.eulerAngles.y, 0);
        Vector3 moveDirection = headDirection * new Vector3(position.x, 0, position.y);

        if (instantMove && CheckIfTeleportationGround(moveDirection * speed))
            characterController.Move(moveDirection * speed);
        else if (CheckIfTeleportationGround(moveDirection * (Time.deltaTime * speed)))
            characterController.Move(moveDirection * (Time.deltaTime * speed));
    }

    private void PositionController()
    {
        Vector3 headLocalPos = head.transform.localPosition;

        float headHeight = Mathf.Clamp(headLocalPos.y, 1, 2);

        characterController.height = headHeight;

        Vector3 newCenter = new Vector3(headLocalPos.x, characterController.height / 2 + characterController.skinWidth, headLocalPos.z);

        characterController.center = newCenter;
    }

    private float fallSpeed;

    private void ApplyGravity()
    {
        if (characterController.isGrounded) fallSpeed = 0;
        else fallSpeed += Physics.gravity.y * Time.fixedDeltaTime;

        characterController.Move(fallSpeed * Time.fixedDeltaTime * Vector3.up * gravityMultiplier);
    }

    // private bool isGrounded()
    // {
    //     Vector3 startPos = transform.TransformPoint(characterController.center);
    //     return Physics.SphereCast(startPos, characterController.radius, Vector3.down, out RaycastHit hit, characterController.center.y + .01f, layerMask);
    // }

    private bool CheckIfTeleportationGround(Vector3 movedirection)
    {
        if (!moveOnlyOnTeleportArea) return true;

        Vector3 startPos = transform.TransformPoint(characterController.center) + movedirection;
        Debug.DrawRay(startPos, Vector3.down * (characterController.center.y + .1f), Color.yellow);
        return Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, characterController.center.y + 1, layerMask) 
            ? hit.collider.attachedRigidbody?.GetComponent<TeleportationArea>() : false;
    }
}