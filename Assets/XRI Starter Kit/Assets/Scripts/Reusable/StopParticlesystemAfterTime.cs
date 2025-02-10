using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class StopParticlesystemAfterTime : MonoBehaviour
    {
        [SerializeField] private new ParticleSystem particleSystem = null;

        [SerializeField] private float timeTillStop = 5;

        private void Start()
        {
            OnValidate();
            Invoke(nameof(StopParticles), timeTillStop);
        }

        private void StopParticles()
        {
            particleSystem.Stop();
        }

        private void OnValidate()
        {
            if (!particleSystem) particleSystem = GetComponent<ParticleSystem>();
        }
    }
}