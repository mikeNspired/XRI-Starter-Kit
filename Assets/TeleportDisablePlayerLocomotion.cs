using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportDisablePlayerLocomotion : MoveToLocation
{
    public LocomotionProvider[] moveProviders;
    public UnityEvent VehicleEntered, VehicleExited;
    public Transform TransformToChildPlayerTo;
    
    private void Start()
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