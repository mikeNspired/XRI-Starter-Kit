using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class ShellExplosion : MonoBehaviour
    {
        public LayerMask m_TankMask;
        public ParticleSystem m_ExplosionParticles;
        public AudioSource m_ExplosionAudio;
        public AreaDamage AreaDamage;
        public float m_ExplosionForce = 1000f,m_MaxLifeTime = 2f,m_ExplosionRadius = 5f;

        private void Start() => Destroy(gameObject, m_MaxLifeTime);

        private void OnCollisionEnter(Collision collision)
        {
            AreaDamage.TriggerDamage();
            // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
            Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();

                if (!targetRigidbody)
                    continue;

                targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);
            }

            m_ExplosionParticles.transform.parent = null;
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();

            // Once the particles have finished, destroy the gameobject they are on.
            ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
            Destroy(m_ExplosionParticles.gameObject, mainModule.duration);
            Destroy(gameObject);
        }
    }
}