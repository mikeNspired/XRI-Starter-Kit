using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerInventory : MonoBehaviour
{
    public InventorySlot[] inventoryItems;
    public GameObject TopItem;
    public GameObject BottomItem;
    public GameObject LeftItem;
    public GameObject RightItem;
    public InputHelpers.Button activationButton;

    private bool isActive = false;
    [SerializeField] private float ItemSpacing = 400;
    public XRController leftController, rightController;

    private void Awake()
    {
        inventoryItems = GetComponentsInChildren<InventorySlot>();
        foreach (var itemSlot in inventoryItems)
            itemSlot.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        UpdateSpacing();
    }


    private void UpdateSpacing()
    {
        TopItem.transform.localPosition = new Vector3(0, ItemSpacing, 0);
        BottomItem.transform.localPosition = new Vector3(0, -ItemSpacing, 0);
        LeftItem.transform.localPosition = new Vector3(-ItemSpacing, 0, 0);
        RightItem.transform.localPosition = new Vector3(ItemSpacing, 0, 0);
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
    }

    private void ToggleInventoryItems(bool state, GameObject sender)
    {
        foreach (var itemSlot in inventoryItems)
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

        // TopItem.SetActive(state);
        // BottomItem.SetActive(state);
        // LeftItem.SetActive(state);
        // RightItem.SetActive(state);
    }

    private void SetPositionAndRotation(GameObject sender)
    {
        transform.position = sender.transform.position;
        transform.localEulerAngles = Vector3.zero;
        transform.LookAt(Camera.main.transform);
    }
}