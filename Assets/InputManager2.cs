using UnityEngine;
using UnityEngine.XR;

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