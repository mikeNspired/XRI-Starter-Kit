using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class TestScript : MonoBehaviour
{
    [SerializeField] private GameObject viveRig;
    [SerializeField] private GameObject oculusRig;
    [SerializeField] private GameObject windowsRig;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(EnableCorrectRig());
    }

    IEnumerator EnableCorrectRig()
    {
        var hmdList = new List<InputDevice>();
        while (hmdList.Count == 0)
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdList);
            yield return new WaitForEndOfFrame();
        }

        var headSetName = hmdList[0].name.ToLower();

        if (headSetName.Contains("windows"))
            SetRigActive(windowsRig);
        else if (headSetName.Contains("vive"))
            SetRigActive(viveRig);
        else
            SetRigActive(oculusRig);
    }

    private void SetRigActive(GameObject rig)
    {
        viveRig.SetActive(false);
        oculusRig.SetActive(false);
        windowsRig.SetActive(false);

        rig.SetActive(true);
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

[Serializable]
public class Vector2Event : UnityEvent<Vector2>
{
}