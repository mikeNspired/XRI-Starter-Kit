using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class DisableAtStart : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(false);
        }

    }
}