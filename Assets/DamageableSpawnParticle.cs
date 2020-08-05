using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageableSpawnParticle : Damageable
{
    [SerializeField] private GameObject particleEffect;

    public override void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource)
    {
        var particle = Instantiate(particleEffect).gameObject;
        Debug.Log("Spawned");
        particle.transform.position = new Vector3(transform.position.x, damageSource.transform.position.y, transform.position.z);
        particle.transform.LookAt(damageSource.transform);

        base.InflictDamage(damage, isExplosionDamage, damageSource);
    }
}