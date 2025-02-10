using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    public class UnityEventHelper : MonoBehaviour
    {
        public UnityEvent[] Events;

        public void IntEvent(int value)
        {
            if (Events.Length > value)
                Events[value]?.Invoke();
        }
    }
}