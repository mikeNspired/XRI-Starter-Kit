using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[CreateAssetMenu(fileName = "Headset Button Configuration", menuName = "ScriptableObject/HeadSetConfiguration")]
public class HeadSetConfiguration : ScriptableObject
{
    public string[] nameSearch;
    public InputHelpers.Button teleportRayEnabler = InputHelpers.Button.SecondaryAxis2DDown;
    public float teleportRayEnablerActivation = .9f;
    public InputHelpers.Button teleportInteractor = InputHelpers.Button.Grip;
    public float teleportActivationThreshold = .75f;
   // public SnapTurnProvider.InputAxes snapTurn = SnapTurnProvider.InputAxes.Primary2DAxis;
    public PlayerMovementCharacterController.InputAxes playerMovement = PlayerMovementCharacterController.InputAxes.Primary2DAxis;
    public bool movePlayerOnPadClick = false;
    public InputHelpers.Button playerCrouch = InputHelpers.Button.SecondaryAxis2DDown;
    public Vector3 handPosition;
    public Vector3 handRotation;
}