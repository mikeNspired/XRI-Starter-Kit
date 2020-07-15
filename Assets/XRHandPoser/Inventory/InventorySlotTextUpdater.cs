using System;
using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class InventorySlotTextUpdater : MonoBehaviour
{
    public TextMeshProUGUI currentCount;
    public TextMeshProUGUI maxCount;

    private InventorySlot inventorySlot;

    void Start()
    {
        OnValidate();
        inventorySlot.inventorySlotUpdated.AddListener(CheckTypes);
        CheckTypes();
    }

    private void CheckTypes()
    {
        if (!inventorySlot.currentSlotItem) return;

        var projectile = inventorySlot.currentSlotItem.GetComponent<ProjectileWeapon>();
        if (projectile)
        {
            CheckAmmo(projectile);
            return;
        }
        else
        {
            HideText();
        }
    }

    private void CheckAmmo(ProjectileWeapon projectile)
    {
        Magazine magazine = projectile.magazineAttach?.Magazine;
        if (!magazine)
        {
            SetTextToInfinity();
        }
        else
            SetText(magazine.AmmoCount.ToString(), magazine.MaxAmmo.ToString());
    }

    private void SetText(string currentValue, string maxValue)
    {
        currentCount.text = currentValue;
        maxCount.text = "/" + maxValue;
    }

    private void HideText()
    {
        currentCount.text = "";
        maxCount.text = "";
    }

    private void SetTextToInfinity()
    {
        currentCount.text = "";
        maxCount.text = "∞";
    }


    private void OnValidate()
    {
        if (!inventorySlot)
            inventorySlot = GetComponent<InventorySlot>();
    }
}