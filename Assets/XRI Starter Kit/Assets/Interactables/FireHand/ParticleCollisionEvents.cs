using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleCollisionEvents : MonoBehaviour
    {
        [Header("Collision Options")] [Tooltip("If true, collisions will invoke the UnityEvent.")] [SerializeField]
        private bool invokeUnityEvent = true;

        [Tooltip("If true, collisions will invoke the C# event.")] [SerializeField]
        private bool invokeCSharpEvent = false;

        [Tooltip("If true, particles will be destroyed after a collision.")] [SerializeField]
        private bool killParticleOnCollision = false;

        [Tooltip("UnityEvent called when a collision occurs.")] [SerializeField]
        private UnityEvent<GameObject> onCollisionUnityEvent;

        public event Action<GameObject, Vector3> OnParticleCollisionEvent;

        private ParticleSystem particleSystemInstance;
        private List<ParticleCollisionEvent> collisionEvents = new(16);

        private void Awake()
        {
            particleSystemInstance = GetComponent<ParticleSystem>() ??
                                     throw new NullReferenceException(
                                         $"{nameof(ParticleCollisionEvents)} requires a ParticleSystem.");
        }

        private void OnParticleCollision(GameObject other)
        {
            // Get all collision events for this 'other'
            int collisionCount = particleSystemInstance.GetCollisionEvents(other, collisionEvents);
            if (collisionCount == 0)
                return;

            // Process each collision event
            foreach (var e in collisionEvents)
            {
                // Invoke Unity and C# events
                if (invokeUnityEvent)
                    onCollisionUnityEvent?.Invoke(other);
                if (invokeCSharpEvent)
                    OnParticleCollisionEvent?.Invoke(other, e.intersection);

                // Kill particle if the option is enabled
                if (killParticleOnCollision)
                    KillParticleAtCollision(e.intersection);
            }
        }

        private void KillParticleAtCollision(Vector3 collisionPosition)
        {
            int particleCount = particleSystemInstance.particleCount;
            var particles = new ParticleSystem.Particle[particleCount];
            particleSystemInstance.GetParticles(particles);

            for (int i = 0; i < particleCount; i++)
            {
                if (!(Vector3.Distance(particles[i].position, collisionPosition) < 0.1f)) continue;
                particles[i].remainingLifetime = 0f;
                break;
            }

            particleSystemInstance.SetParticles(particles, particleCount);
        }
    }
}