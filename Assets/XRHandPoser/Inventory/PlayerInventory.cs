using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerInventory : MonoBehaviour
{
    public InventorySlot[] inventorySlots;
    [SerializeField] private InputHelpers.Button activationButton;
    [SerializeField] private XRController leftController = null, rightController = null;
    [SerializeField] private AudioSource enableAudio, disableAudio;

    [SerializeField] private bool lookAtController;
    private bool isActive = false;

    private void Awake()
    {
        inventorySlots = GetComponentsInChildren<InventorySlot>();

        foreach (var itemSlot in inventorySlots)
            itemSlot.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        inventorySlots = GetComponentsInChildren<InventorySlot>();
    }
    
    private void Update()
    {
        CheckController();
    }

    private bool buttonClicked;

    private void CheckController()
    {
        float activationThreshold = .5f;
        bool isRightHand = false;

        leftController.inputDevice.IsPressed(activationButton, out bool isActive, activationThreshold);
        if (!isActive)
        {
            rightController.inputDevice.IsPressed(activationButton, out isActive, activationThreshold);
            isRightHand = true;
        }

        if (isActive && !buttonClicked)
        {
            buttonClicked = true;
            ToggleInventoryAtController(isRightHand);
        }
        else if (!isActive)
            buttonClicked = false;
    }

    private void ToggleInventoryAtController(bool isRightHand)
    {
        if (isRightHand)
            TurnOnInventory(rightController.gameObject);
        else
            TurnOnInventory(leftController.gameObject);
    }

    private void TurnOnInventory(GameObject sender)
    {
        isActive = !isActive;
        ToggleInventoryItems(isActive, sender);
        PlayAudio(isActive);
    }

    private void PlayAudio(bool state)
    {
        if(state)
            enableAudio.Play();
        else
            disableAudio.Play();
    }

    
    private void ToggleInventoryItems(bool state, GameObject sender)
    {
        foreach (var itemSlot in inventorySlots)
        {
            if (!state)
            {
                itemSlot.DisableSlot();
            }
            else
            {
                if (itemSlot.gameObject.active)
                    itemSlot.EnableSlot();
                itemSlot.gameObject.SetActive(state);
                SetPositionAndRotation(sender);
            }
        }
    }

    private void SetPositionAndRotation(GameObject sender)
    {
        transform.position = sender.transform.position;
        transform.localEulerAngles = Vector3.zero;
        
        if(lookAtController)
            transform.LookAt(sender.transform);
        else
          transform.LookAt(Camera.main.transform);
    } 

    
}

