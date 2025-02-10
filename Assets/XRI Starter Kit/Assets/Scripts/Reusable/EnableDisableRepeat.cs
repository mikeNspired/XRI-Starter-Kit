using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class EnableDisableRepeat : MonoBehaviour
    {
        [SerializeField] private float activeTime;
        [SerializeField] private float deactiveTime = .25f;
        [SerializeField] private GameObject[] objectToActive = null;
        [SerializeField] private bool activeForSingleFrame = false;

        private float currentActivateTimer, currentDeactivateTimer;
        private bool isActive;


        private void Update()
        {
            if (activeForSingleFrame)
                activeTime = Time.deltaTime;

            currentActivateTimer += Time.deltaTime;
            currentDeactivateTimer += Time.deltaTime;

            if (isActive && currentActivateTimer >= activeTime)
            {
                currentActivateTimer = 0;
                currentDeactivateTimer = 0;
                Deactivate();
            }

            if (!isActive && currentDeactivateTimer >= deactiveTime)
            {
                currentActivateTimer = 0;
                currentDeactivateTimer = 0;
                Activate();
            }
        }

        private void Activate()
        {
            isActive = true;
            foreach (var objToActivate in objectToActive)
            {
                objToActivate.SetActive(true);
            }
        }

        private void Deactivate()
        {
            isActive = false;

            foreach (var objToActivate in objectToActive)
            {
                objToActivate.SetActive(false);
            }
        }
    }
}