using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class ListActivator : MonoBehaviour
    {
        public List<GameObject> objectList;

        [SerializeField] private bool SetStateOnStart = false;
        [SerializeField] private bool isActivateAtStart = false;

        private void Start()
        {
            if (!SetStateOnStart) return;

            if (isActivateAtStart)
                Activate();
            else
                Deactivate();
        }

        public void Activate()
        {
            foreach (var gameObject in objectList)
            {
                gameObject.SetActive(true);
            }
        }

        public void Deactivate()
        {
            foreach (var gameObject in objectList)
            {
                gameObject.SetActive(false);
            }
        }

        public void SetStateInt(int x)
        {
            if (x == 0)
                Activate();
            else
                Deactivate();
        }
    }
}