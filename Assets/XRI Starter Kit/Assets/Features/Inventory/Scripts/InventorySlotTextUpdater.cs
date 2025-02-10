using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public class InventorySlotTextUpdater : MonoBehaviour
    {
        public TextMeshProUGUI currentCount;
        public TextMeshProUGUI maxCount;

        private InventorySlot inventorySlot;

        void Awake()
        {
            if (!inventorySlot)
                inventorySlot = GetComponent<InventorySlot>();
            inventorySlot.onSlotUpdated += CheckTypes;
        }

        private void CheckTypes(XRBaseInteractable currentSlotItem)
        {
            if (!currentSlotItem) return;

            var projectile = currentSlotItem.GetComponent<ProjectileWeapon>();
            if (projectile)
                CheckAmmo(projectile);
            else
                HideText();
        }

        private void CheckAmmo(ProjectileWeapon projectile)
        {
            if (!projectile.magazineAttach)
            {
                SetTextToInfinity();
            }
            else
            {
                Magazine magazine = projectile.magazineAttach.Magazine;
                if (magazine)
                    SetText(magazine.CurrentAmmo.ToString(), magazine.MaxAmmo.ToString());
            }
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
    }
}