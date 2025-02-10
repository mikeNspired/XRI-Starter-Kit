using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    public class ClimbingStamina : MonoBehaviour
    {
        public float stamina, maxStamina;
        public float drainRate = .9f, regainRate = 1;
        public List<MeshRenderer> staminaBlocks;
        public Material hasStaminaMat, noStaminaMat;
        public bool isDraining, isActive;
        public UnityEvent OutOfStamina;

        private void Start() => Deactivate();

        private void Update()
        {
            if (!isDraining)
                RegainStamina();
            else
                DrainStamina();

            SetStaminaColor();
        }

        public void Activate()
        {
            isActive = true;
            isDraining = true;
            StopAllCoroutines();
            StartCoroutine(ActivateDisplay());
        }

        public void Deactivate()
        {
            isActive = false;
            isDraining = false;
            StartCoroutine(RegainAndDeactivateDisplay());
        }

        public void StartDraining() => isDraining = true;

        public void StopDraining() => isDraining = false;

        private void DrainStamina()
        {
            stamina -= drainRate * Time.deltaTime;
            stamina = Mathf.Clamp(stamina, 0, Mathf.Infinity);
            if (stamina <= 0)
                OutOfStamina?.Invoke();
        }

        private void RegainStamina()
        {
            stamina += regainRate * Time.deltaTime;
            stamina = Mathf.Clamp(stamina, 0, maxStamina);
        }

        private void SetStaminaColor()
        {
            for (int i = 0; i < staminaBlocks.Count; i++)
                staminaBlocks[i].material = stamina > i ? hasStaminaMat : noStaminaMat;
        }

        public void HideDisplay()
        {
            foreach (var t in staminaBlocks) t.gameObject.SetActive(false);
        }

        public void ShowDisplay()
        {
            foreach (var t in staminaBlocks) t.gameObject.SetActive(true);
        }

        private IEnumerator ActivateDisplay()
        {
            for (var index = staminaBlocks.Count - 1; index >= 0; index--)
            {
                var t = staminaBlocks[index];
                t.gameObject.SetActive(true);
                yield return null;
            }
        }

        private IEnumerator RegainAndDeactivateDisplay()
        {
            while (stamina < maxStamina)
            {
                yield return null;
                RegainStamina();
            }

            foreach (var t in staminaBlocks)
            {
                t.gameObject.SetActive(false);
                yield return null;
            }
        }
    }
}