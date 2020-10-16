using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class ConfigureHeadSetControls : MonoBehaviour
{
    [SerializeField] private TeleportRayEnabler teleportRayEnabler = null;
    [SerializeField] private XRController leftTeleportInteractor = null;
    [SerializeField] private XRController rightTeleportInteractor = null;
    [SerializeField] private SnapTurnProvider snapTurnProvider = null;
    [SerializeField] private PlayerMovementCharacterController playerMovementCharacterController = null;
    [SerializeField] private PlayerCrouch playerCrouch = null;
    [SerializeField] private HeadSetConfiguration[] headSetConfigs = null;
    [SerializeField] private HeadSetConfiguration debugShowCurrentHeadset = null;
    [SerializeField] private HeadSetConfiguration forceHeadSetConfig = null;

    private void Start()
    {
        if (forceHeadSetConfig)
        {
            SetConfig(forceHeadSetConfig);
            return;
        }

        StartCoroutine(EnableCorrectRig());
    }

    private IEnumerator EnableCorrectRig()
    {
        var hmdList = new List<InputDevice>();

        while (hmdList.Count == 0)
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdList);
            yield return new WaitForEndOfFrame();
        }

        var headSetName = hmdList[0].name.ToLower();

        foreach (var config in headSetConfigs)
        {
            if (ConfigByHeadsetType(headSetName, config))
                yield break;
        }
    }

    private bool ConfigByHeadsetType(string headSetName, HeadSetConfiguration headsetConfig)
    {
        if (!headsetConfig.nameSearch.Any(name => headSetName.Contains(name.ToLower()))) return false;
        SetConfig(headsetConfig);
        return true;
    }

    private void SetConfig(HeadSetConfiguration config)
    {
        teleportRayEnabler.activationButton = config.teleportRayEnabler;
        teleportRayEnabler.activationThreshold = config.teleportRayEnablerActivation;
        leftTeleportInteractor.selectUsage = config.teleportInteractor;
        rightTeleportInteractor.selectUsage = config.teleportInteractor;
        leftTeleportInteractor.axisToPressThreshold = config.teleportActivationThreshold;
        rightTeleportInteractor.axisToPressThreshold = config.teleportActivationThreshold;
        snapTurnProvider.turnUsage = config.snapTurn;
        Debug.Log(playerMovementCharacterController);
        Debug.Log(config.playerMovement);
        playerMovementCharacterController.buttonInput = config.playerMovement;
        playerMovementCharacterController.moveOnlyOnPadClick = config.movePlayerOnPadClick;
        playerCrouch.activationButton = config.playerCrouch;
        debugShowCurrentHeadset = config;
    }
}