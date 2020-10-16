using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasGetEventCamera : MonoBehaviour
{
    private Camera mainCamera;

    private void Awake()
    {
        StartCoroutine(EnableCorrectRig());
    }

    private IEnumerator EnableCorrectRig()
    {
        while (!mainCamera)
        {
            yield return new WaitForEndOfFrame();
            try
            {
                mainCamera = Camera.main;
            }
            catch
            {
            }
        }

        GetComponent<Canvas>().worldCamera = mainCamera;
    }
}

