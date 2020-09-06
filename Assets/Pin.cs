using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Pin : MonoBehaviour
{
    [SerializeField] private float distance;
    public UnityEvent pinKnockedOver;

    public bool isActive;

    private void Update()
    {
        Debug.DrawRay(transform.position + transform.up * .02f, -transform.up * distance * 2, Color.yellow);
        if (!isActive) return;

        if (Physics.Raycast(transform.position + transform.up * .02f, -transform.up, out RaycastHit hit2, distance * 2))
        {
            var name = hit2.collider.transform.name.ToLower();

            if (!name.Contains("lane"))
            {
                Trigger();
            }
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