// Author MikeNspired. 

using System;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class SimpleCollisionDamage : MonoBehaviour
    {
        [SerializeField] protected float damage = 10;
        [SerializeField] private GameObject metalDecal = null;
        [SerializeField] private GameObject fleshDecal = null;
        [SerializeField] private GameObject woodDecal = null;
        [SerializeField] private bool destroyOnCollision = true, triggerDamage = false;
        
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody?.GetComponent<SimpleCollisionDamage>()) return;

            var damageable = collision.transform.GetComponentInParent<IDamageable>();
            if (damageable != null)
                Damage(damageable);
            
            CheckForImpactDecalType(collision);

            if (destroyOnCollision)
                Destroy(gameObject);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!triggerDamage) return;
            
            var damageable = other.transform.GetComponentInParent<IDamageable>();
            if (damageable != null)
                Damage(damageable);
            
            if (destroyOnCollision)
                Destroy(gameObject);
        }

        protected virtual void Damage(IDamageable damageable) => damageable.TakeDamage(damage,gameObject);

        private void CheckForImpactDecalType(Collision collision)
        {
            var impact = collision.transform.GetComponentInParent<IImpactType>();

            if (impact != null)
            {
                var impactType = impact.GetImpactType();
                var shouldReparent = impact.ShouldReparent;
                switch (impactType)
                {
                    case ImpactType.Flesh:
                        SpawnDecal(collision, fleshDecal,shouldReparent);
                        break;
                    case ImpactType.Metal:
                        SpawnDecal(collision, metalDecal,shouldReparent);
                        break;
                    case ImpactType.Wood:
                        SpawnDecal(collision, woodDecal,shouldReparent);
                        break;
                    case ImpactType.Neutral:
                        SpawnDecal(collision, null,shouldReparent);
                        break;
                    default:
                        SpawnDecal(collision, metalDecal,shouldReparent);
                        break;
                }
            }
            else
                SpawnDecal(collision, metalDecal, false);
        }

        private static void SpawnDecal(Collision hit, GameObject decalPrefab, bool shouldReparent)
        {
            if (!decalPrefab) return;
            
            var spawnedDecal = Instantiate(decalPrefab, null, true);
            
            if(shouldReparent)
                spawnedDecal.transform.SetParent(hit.transform);
            
            var contact = hit.contacts[0];
            spawnedDecal.transform.position = contact.point;
            spawnedDecal.transform.forward = contact.normal;
        }
    }
}