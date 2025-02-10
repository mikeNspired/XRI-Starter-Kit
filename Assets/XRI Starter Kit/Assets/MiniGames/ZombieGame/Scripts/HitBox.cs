using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class HitBox : MonoBehaviour, IDamageable
    {
        [SerializeField] private float damageMultiplier = 1;
        private EnemyHealth damageable;

        private void Awake() => damageable = GetComponentInParent<EnemyHealth>();

        public void TakeDamage(float damage, GameObject damager) =>
            damageable?.TakeDamage(damage * damageMultiplier, gameObject);
    }
}