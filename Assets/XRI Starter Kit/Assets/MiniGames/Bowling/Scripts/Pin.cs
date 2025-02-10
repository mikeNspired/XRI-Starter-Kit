using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    public class Pin : MonoBehaviour
    {
        [SerializeField] private float distance = 0.04f;
        public UnityEvent pinKnockedOver;

        public bool isActive;

        private void Update()
        {
            Debug.DrawRay(transform.position + transform.up * .02f, -transform.up * distance * 2, Color.yellow);
            if (!isActive) return;

            if (Physics.Raycast(transform.position + transform.up * .02f, -transform.up, out RaycastHit hit, distance * 2))
            {
                if (!hit.transform.GetComponent<Lane>())
                    Trigger();
            }
            else
            {
                Trigger();
            }
        }

        private void Trigger()
        {
            isActive = false;
            pinKnockedOver.Invoke();
            Destroy(gameObject, 10);
        }
    }
}