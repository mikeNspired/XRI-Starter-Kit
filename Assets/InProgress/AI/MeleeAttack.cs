using System;
using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [Tooltip("LifeTime of the VFX before being destroyed")]
    public float impactVFXLifetime = 5f;

    [Tooltip("Offset along the hit normal where the VFX will be spawned")]
    public float impactVFXSpawnOffset = 0.1f;
    
    [Tooltip("Layers this projectile can collide with")]
    public LayerMask hittableLayers = -1;

    [Header("Damage")] [Tooltip("Damage of the projectile")]
    public float damage = 40f;

    [Tooltip("VFX prefab to spawn upon impact")]
    public bool impactVFX;

    [SerializeField] private GameObject metalDecal = null;
    [SerializeField] private GameObject fleshDecal = null;
    [SerializeField] private GameObject woodDecal = null;
    [SerializeField] private AudioRandomize audio = null;

    [SerializeField] private bool disableAtStart = true;
    private GameObject owner;
    Vector3 m_LastRootPosition;
    Vector3 m_Velocity;
    bool m_HasTrajectoryOverride;
    float m_ShootTime;
    Vector3 m_TrajectoryCorrectionVector;
    Vector3 m_ConsumedTrajectoryCorrectionVector;
    List<Collider> m_IgnoredColliders;

    private int affiliation;

    private void Start()
    {
        m_IgnoredColliders = new List<Collider>();
        owner = GetComponentInParent<Health>().gameObject;
        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        m_IgnoredColliders.AddRange(ownerColliders);
        affiliation = GetComponentInParent<Actor>().affiliation;
        audio = GetComponent<AudioRandomize>();
        if (disableAtStart)
            gameObject.SetActive(false);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (IsHitValid(other))
            OnHit(transform.position, transform.forward, other);
    }


    bool IsHitValid(Collider collider)
    {
        // ignore hits with an ignore component
        if (collider.GetComponent<IgnoreHitDetection>())
        {
            return false;
        }

        // ignore hits with triggers that don't have a Damageable component
        if (collider.isTrigger && collider.GetComponent<Damageable>() == null)
        {
            return false;
        }

        // ignore hits with specific ignored colliders (self colliders, by default)
        if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(collider))
        {
            return false;
        }

        return true;
    }

    void OnHit(Vector3 point, Vector3 normal, Collider collider)
    {
        // point damage
        Damageable damageable = collider.GetComponent<Damageable>();
        if (!damageable) return;

        var enemyAffiliation = collider.GetComponentInParent<Actor>()?.affiliation;
        if (enemyAffiliation == affiliation) return;

        if (damageable)
        {
            damageable.InflictDamage(damage, false, owner);
        }


        // impact vfx
        if (impactVFX)
        {
            SpawnDecalType(damageable, point + (normal * impactVFXSpawnOffset), Quaternion.LookRotation(normal));
        }

        // impact sfx
        if (audio)
        {
            audio.PlaySound();
        }
    }


    private void SpawnDecalType(Damageable damageable, Vector3 impactVfxSpawnOffset, Quaternion lookRotation)
    {
        GameObject decalPrefab;
        var impactType = damageable.GetImpactType();

        switch (impactType)
        {
            case ImpactType.Flesh:
                decalPrefab = fleshDecal;
                break;
            case ImpactType.Metal:
                decalPrefab = metalDecal;
                break;
            case ImpactType.Wood:
                decalPrefab = woodDecal;
                break;
            case ImpactType.Neutral:
                decalPrefab = metalDecal;
                break;
            default:
                decalPrefab = metalDecal;
                break;
        }

        GameObject spawnedDecal = Instantiate(decalPrefab, impactVfxSpawnOffset, lookRotation);
        spawnedDecal.transform.SetParent(damageable.transform);
    }
}