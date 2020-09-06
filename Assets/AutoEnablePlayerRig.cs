using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class AutoEnablePlayerRig : MonoBehaviour
{
    [SerializeField] private GameObject viveRig;
    [SerializeField] private GameObject oculusRig;
    [SerializeField] private GameObject windowsRig;

    private void Awake()
    {
        StartCoroutine(EnableCorrectRig());
    }

    IEnumerator EnableCorrectRig()
    {
        var hmdList = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdList);

        while (hmdList.Count == 0)
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdList);
            yield return new WaitForEndOfFrame();
        }

        var headSetName = hmdList[0].name.ToLower();

        if (headSetName.Contains("windows") || headSetName.Contains("wmr"))
            SetRigActive(windowsRig);
        else if (headSetName.Contains("vive") ||  headSetName.Contains("openvr"))
            SetRigActive(viveRig);
        else
            SetRigActive(oculusRig);
        
        
        yield return new WaitForEndOfFrame();

        SetAllCanvasesToRig();
    }

    private void SetRigActive(GameObject rig)
    {
        viveRig.SetActive(false);
        oculusRig.SetActive(false);
        windowsRig.SetActive(false);

        rig.SetActive(true);
    }

    private void SetAllCanvasesToRig()
    {
        var canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            canvas.worldCamera = Camera.main;
        }
    }


    // Update is called once per frame
    void Update()
    {
        // var hmdList = new List<InputDevice>();
        // InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdList);
        // foreach (var item in hmdList)
        //     Debug.Log($"hmd.name={item.name} hmd.characteristics={item.characteristics}");
    }
}

public class InputManager2 : MonoBehaviour
{
    //Gripping
    //Primary Function = Shooting
    //Secondary Function = Ammo release
    //Tri Function = Change fire mode

    //Potential Button types
    //Clicking on touchpad in different parts
    //Normal Buttons
    //Joystick clicking

    //Teleportation Type : Joystick forward release, Touchpad Click, button press
    
    private void Update()
    {
    
    }

    
    bool wasPressed = false;
    private void GetButtonState()
    {
        InputDevice device = new InputDevice();
        device.TryGetFeatureValue(CommonUsages.gripButton, out bool isPressed);
       

        bool isActive = false;
        PressType pressType = PressType.Begin;
        switch (pressType)
        {
            case PressType.Pressed:
                isActive = isPressed;
                break;
            case PressType.Begin:
                isActive = isPressed && !wasPressed;
                break;
            case PressType.Released:
                isActive = !isPressed && wasPressed;
                break;
        }

        wasPressed = isPressed; 
    }

    public enum PressType
    {
        Begin,
        Pressed,
        Released
    }
}

