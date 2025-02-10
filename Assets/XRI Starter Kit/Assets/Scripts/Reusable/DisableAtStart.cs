using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class DisableAtStart : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(false);
        }

    }
}