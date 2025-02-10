// Author MikeNspired. 

using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class DestroyAfterTime : MonoBehaviour
    {
        public float timeTillDestroy = 1f;
        [SerializeField] private bool destroyAfterFrame = false;
        [SerializeField] private bool startTimerOnAwake = true;

        private void Start()
        {
            if(startTimerOnAwake) StartTimerToDestroy();
        }

        public void StartTimerToDestroy() => Invoke(nameof(DestroyThis), !destroyAfterFrame ? timeTillDestroy : Time.deltaTime);

        private void DestroyThis() => DestroyImmediate(gameObject);
    }
}