using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

[CreateAssetMenu(fileName = "NewButtonHandler")]
public class ButtonHandler : InputHandler
{
    public XRController leftController, rightController;

    public InputHelpers.Button button = InputHelpers.Button.None;
    public override void HandleState(XRController controller)
    {
    
    }
}