using UnityEngine;
using UnityEngine.Serialization;

namespace MikeNspired.XRIStarterKit
{
    public class TargetPoints : MonoBehaviour, IDamageable
    {
        public UnityEventFloat onHit;

        [FormerlySerializedAs("damageMultiplier")]
        public float points = 1;

        public AudioRandomize hitSoundEffect;
        public bool canTakeDamage;

        public void TakeDamage(float damage, GameObject damager)
        {
            if (!canTakeDamage) return;
            if (hitSoundEffect)
                hitSoundEffect.Play();
            onHit.Invoke(points);
        }

        public void Hit()
        {
            if (!canTakeDamage) return;
            if (hitSoundEffect)
                hitSoundEffect.Play();
            onHit.Invoke(points);
        }

        public void Activate() => canTakeDamage = true;
        public void Deactivate() => canTakeDamage = false;
    }
}