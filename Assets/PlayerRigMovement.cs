using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerRigMovement : MonoBehaviour
{
    public List<XRNode> xrNodes;
    InputAxes TurningInput = InputAxes.Primary2DAxis;
    public List<XRController> controllers;
    private CharacterController characterController;
    private GameObject head;

    public float speed;
    public float gravityMultiplier;

    [SerializeField] [Tooltip("The deadzone that the controller movement will have to be above for instantaneous movement.")]
    float inputDeadZone = 0.75f;

    public bool instantMove;

    [SerializeField] private bool moveOnlyOnTeleportArea;

    private enum InputAxes
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
        CheckForInput();
        ApplyGravity();
    }


    private bool isMoving;

    private void CheckForInputInstantMovement()
    {
//        foreach (var controller in controllers)
//        {
//            if (controller.enableInputActions)
//                CheckForMovement(controller.inputDevice);
//        }

        foreach (var node in xrNodes)
        {
            InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(InputAxesToCommonUsage[(int) TurningInput], out Vector2 inputAxis);

            //Test stop from both controllers trying to move player at same time
            if (inputAxis != Vector2.zero && !isMoving)
            {
                if (Mathf.Abs(inputAxis.x) > inputDeadZone || Mathf.Abs(inputAxis.y) > inputDeadZone)
                {
                    StartMove(inputAxis);
                    isMoving = true;
                }

                return;
            }

            isMoving = false;
        }
    }

    private void CheckForInput()
    {
//        foreach (var controller in controllers)
//        {
//            if (controller.enableInputActions)
//                CheckForMovement(controller.inputDevice);
//        }

        foreach (var node in xrNodes)
        {
            InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(InputAxesToCommonUsage[(int) TurningInput], out Vector2 inputAxis);
            StartMove(inputAxis);

            //Test stop from both controllers trying to move player at same time
            if (inputAxis != Vector2.zero)
            {
                return;
            }
        }
    }

//    private void CheckForMovement(InputDevice controllerInputDevice)
//    {
//        if (controllerInputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 position))
//        {
//            StartMove(position);
//        }
//    }

    private void StartMove(Vector2 position)
    {
//        Vector3 direction = new Vector3(position.x, 0, position.y);
//        Vector3 rotation = new Vector3(0, head.transform.eulerAngles.y, 0);
//
//        direction = Quaternion.Euler(rotation) * direction;
//
//        Vector3 movement = direction * speed;

        Quaternion headDirection = Quaternion.Euler(0, head.transform.eulerAngles.y, 0);
        Vector3 moveDirection = headDirection * new Vector3(position.x, 0, position.y);


        if (instantMove && CheckIfTeleportationGround(moveDirection * speed))
            characterController.Move(moveDirection * speed);
        else if (CheckIfTeleportationGround(moveDirection * speed))
            characterController.Move(moveDirection * Time.deltaTime * speed);
    }

    private void PositionController()
    {
        Vector3 headLocalPos = head.transform.localPosition;

        float headHeight = Mathf.Clamp(headLocalPos.y, 1, 2);

        characterController.height = headHeight;

        Vector3 newCenter = new Vector3(headLocalPos.x, characterController.height / 2 + characterController.skinWidth, headLocalPos.z);

        characterController.center = newCenter;
    }

    private float fallingSpeed;
    private int layerMask;

    private void ApplyGravity()
    {
//        Vector3 gravity = gravityMulti * Time.deltaTime * new Vector3(0, Physics.gravity.y, 0);
//        characterController.Move(gravity * Time.deltaTime);

        if (characterController.isGrounded)
            fallingSpeed = 0;
        else
            fallingSpeed += Physics.gravity.y * Time.fixedDeltaTime;

        characterController.Move(fallingSpeed * Time.fixedDeltaTime * Vector3.up * gravityMultiplier);
    }

    private bool isGrounded()
    {
        Vector3 startPos = transform.TransformPoint(characterController.center);
        return Physics.SphereCast(startPos, characterController.radius, Vector3.down, out RaycastHit hit, characterController.center.y + .01f, layerMask);
    }

    private bool CheckIfTeleportationGround(Vector3 movedirection)
    {
        if (!moveOnlyOnTeleportArea) return true;
        
        Vector3 startPos = transform.TransformPoint(characterController.center) + movedirection;
        Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, characterController.center.y + .1f, layerMask);
        return hit.collider.GetComponent<TeleportationArea>();
    }
}