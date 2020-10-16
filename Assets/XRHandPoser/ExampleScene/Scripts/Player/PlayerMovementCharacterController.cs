using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerMovementCharacterController : PlayerMovement
{
    [SerializeField] private CharacterController characterController;

    protected override void Awake() => OnValidate();

    private new void OnValidate()
    {
        base.OnValidate();
        if (!gameObject.activeInHierarchy) return;
        if (!characterController) characterController = GetComponent<CharacterController>();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        AdjustCharacterControllerSizeToCamera();
    }

    public override void ShrinkColliderToHead()
    {
        PauseColliderAdjustment();
        characterController.center = head.transform.localPosition + xrRig.cameraYOffset * Vector3.up;
        characterController.height = 0;
    }

    private void AdjustCharacterControllerSizeToCamera()
    {
        if (pauseColliderAdjustment) return;

        Vector3 headLocalPos = head.transform.localPosition + xrRig.cameraYOffset * Vector3.up;
        //Set height of collider to camera 
        characterController.height = headLocalPos.y;

        //Center characterController to camera
        characterController.center = new Vector3(headLocalPos.x, characterController.height / 2 + characterController.skinWidth, headLocalPos.z);
    }

    public override void Move(Vector3 moveDirection)
    {
        characterController.Move(moveDirection);
    }

    public override bool isGrounded() => characterController.isGrounded;

    protected override bool CheckIfTeleportationGround(Vector3 moveDirection)
    {
        if (!MoveOnlyOnTeleportArea) return true;

        Vector3 startPos = transform.TransformPoint(characterController.center) + moveDirection + Vector3.back * .05f;
        Debug.DrawRay(startPos, (characterController.center.y * Vector3.down) * 2, Color.yellow);
        return Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, characterController.center.y + 2)
            ? hit.collider.GetComponentInParent<TeleportationArea>()
            : false;
    }
}

public interface IPlayerMovement
{
    void PauseGravity();
    void ResumeGravity();
    void PauseColliderAdjustment();
    void ResumeColliderAdjustment();
    void ShrinkColliderToHead();
}