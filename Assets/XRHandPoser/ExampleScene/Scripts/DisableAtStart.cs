using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableAtStart : MonoBehaviour
{
    private void Start()
    {
        gameObject.SetActive(false);
    }

}
