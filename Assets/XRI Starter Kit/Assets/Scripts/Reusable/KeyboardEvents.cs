using System;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class KeyboardEvents : MonoBehaviour
    {
        [SerializeField] private string Title;
        [SerializeField] private KeyCodeEvent[] keyCodeEvents;

        private void Update()
        {
            foreach (var keyCode in keyCodeEvents)
            {
                switch (keyCode.KeyType)
                {
                    case KeyType.GetKey:
                    {
                        if (Input.GetKey(keyCode.KeyCode))
                            keyCode.UnityEvent.Invoke(keyCode.value);
                        break;
                    }
                    case KeyType.GetKeyDown:
                    {
                        if (Input.GetKeyDown(keyCode.KeyCode))
                            keyCode.UnityEvent.Invoke(keyCode.value);
                        break;
                    }
                    case KeyType.GetKeyUp:
                    {
                        if (Input.GetKeyUp(keyCode.KeyCode))
                            keyCode.UnityEvent.Invoke(keyCode.value);
                        break;
                    }
                }
            }
        }

        [Serializable]
        private enum KeyType
        {
            GetKey,
            GetKeyDown,
            GetKeyUp
        }

        [Serializable]
        private struct KeyCodeEvent
        {
            public string name;
            public UnityEventFloat UnityEvent;
            public KeyCode KeyCode;
            public KeyType KeyType;
            public float value;
        }
    }
}