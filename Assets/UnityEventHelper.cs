using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UnityEventHelper : MonoBehaviour
{
    public UnityEvent[] Events;
    
    public void IntEvent(int value)
    {
        if(Events.Length > value)
            Events[value]?.Invoke();
    }
}
