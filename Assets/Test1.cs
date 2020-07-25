using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test1 : MonoBehaviour
{
    public GameObject prefabThatPrints;
    private GameObject newObj;
    void Start()
    {
        prefabThatPrints.SetActive(false);
         newObj = Instantiate(prefabThatPrints);
        Destroy(newObj.GetComponent<Test2>());
        Invoke(nameof(DisableDumb),0);
    }

    void DisableDumb()
    {
        newObj.SetActive(true);
    }

}