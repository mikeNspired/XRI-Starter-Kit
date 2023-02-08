using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportDisablePlayerLocomotion : MoveToLocation
{
    public UnityEvent VehicleEntered, VehicleExited;
    [SerializeField] Transform TransformToChildPlayerTo;
    private LocomotionProvider[] moveProviders;

    private void Awake()
    {
        moveProviders = rig.GetComponentsInChildren<LocomotionProvider>();
    }

    public void EnterVehicle()
    {
        VehicleEntered.Invoke();
        TeleportWithHeadAtLocationAndRotate();
        SetCharacterControllersState(false);
        rig.transform.parent.SetParent(TransformToChildPlayerTo);
    }

    public void ExitVehicle()
    {
        VehicleExited.Invoke();
        SetCharacterControllersState(false);
        rig.transform.SetParent(null);
    }

    private void SetCharacterControllersState(bool state)
    {
        foreach (var moveProvider in moveProviders) moveProvider.enabled = state;
    }
}