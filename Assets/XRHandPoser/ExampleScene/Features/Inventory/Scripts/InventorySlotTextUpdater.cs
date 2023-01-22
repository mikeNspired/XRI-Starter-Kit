using TMPro;
using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
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
            if (!inventorySlot.CurrentSlotItem) return;

            var projectile = inventorySlot.CurrentSlotItem.GetComponent<ProjectileWeapon>();
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


        private void OnValidate()
        {
            if (!inventorySlot)
                inventorySlot = GetComponent<InventorySlot>();
        }
    }
}