using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerMovementNoCC : PlayerMovement
{
    [SerializeField] private float maxStepHeight = .1f;

    [SerializeField] private LayerMask checkGroundLayerMask = 1;

    public override void Move(Vector3 moveDirection)
    {
        Vector3 startPos = transform.position + Vector3.up * maxStepHeight + moveDirection;
        Debug.DrawRay(startPos, (Vector3.down) * 2, Color.magenta, 1);
        if (Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, maxStepHeight * 2))
        {
            transform.position = hit.point;
        }
    }

    protected override void ApplyGravity()
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

    public override bool isGrounded()
    {
        Vector3 startPos = transform.position + Vector3.up;
        return Physics.SphereCast(startPos, .1f, Vector3.down, out RaycastHit hit, 1, checkGroundLayerMask);
    }

    protected override bool CheckIfTeleportationGround(Vector3 moveDirection)
    {
        if (!moveOnlyOnTeleportArea) return true;

        Vector3 startPos = transform.position + Vector3.up * maxStepHeight + moveDirection;

        return Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, maxStepHeight * 2)
            ? hit.collider.GetComponentInParent<TeleportationArea>()
            : false;
    }
}