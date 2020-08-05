using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(Health), typeof(NavMeshAgent))]
public class RobotEnemyController : A_EnemyController
{
    [Header("Eye color")] [Tooltip("Material for the eye color")]
    public Material eyeColorMaterial;

    [Tooltip("The default color of the bot's eye")] [ColorUsageAttribute(true, true)]
    public Color defaultEyeColor;

    [Tooltip("The attack color of the bot's eye")] [ColorUsageAttribute(true, true)]
    public Color attackEyeColor;

    RendererIndexData eyeRenderer;
    MaterialPropertyBlock eyeColorMaterialPropertyBlock;
    WeaponController weapon;
    const string k_AnimOnDamagedParameter = "OnDamaged";

    protected override void Start()
    {
        base.Start();
        weapon = GetComponentInChildren<WeaponController>();
        weapon.owner = gameObject;
        weapon.ShowWeapon(true);
        

        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (renderer.sharedMaterials[i] == eyeColorMaterial)
                {
                    eyeRenderer = new RendererIndexData(renderer, i);
                }
            }
        }

        // Check if we have an eye renderer for this enemy
        if (eyeRenderer.renderer != null)
        {
            eyeColorMaterialPropertyBlock = new MaterialPropertyBlock();
            eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", defaultEyeColor);
            eyeRenderer.renderer.SetPropertyBlock(eyeColorMaterialPropertyBlock, eyeRenderer.materialIndex);
        }
    }

    protected override void OnLostTarget()
    {
        base.OnLostTarget();

        // Set the eye attack color and property block if the eye renderer is set
        if (eyeRenderer.renderer != null)
        {
            eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", defaultEyeColor);
            eyeRenderer.renderer.SetPropertyBlock(eyeColorMaterialPropertyBlock, eyeRenderer.materialIndex);
        }
    }

    protected override void OnDetectedTarget()
    {
        base.OnDetectedTarget();

        // Set the eye default color and property block if the eye renderer is set
        if (eyeRenderer.renderer != null)
        {
            eyeColorMaterialPropertyBlock.SetColor("_EmissionColor", attackEyeColor);
            eyeRenderer.renderer.SetPropertyBlock(eyeColorMaterialPropertyBlock, eyeRenderer.materialIndex);
        }
    }


    public override Vector3 GetDestinationOnPath()
    {
        if (IsPathValid())
        {
            return patrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
        }
        else
        {
            return transform.position;
        }
    }

    public override bool TryAtack(Vector3 enemyPosition)
    {
        Vector3 weaponForward = (enemyPosition - weapon.weaponRoot.transform.position).normalized;
        weapon.transform.forward = weaponForward;

        // Shoot the weapon
        bool didFire = weapon.HandleShootInputs(false, true, false);

        if (didFire && onAttack != null)
        {
            onAttack.Invoke();
        }

        return didFire;
    }

    protected void OnDamaged(float damage, GameObject damageSource)
    {
        // pursue the player
        detectionModule.OnDamaged(damageSource);

        if (onDamaged != null)
        {
            onDamaged.Invoke();
        }

        m_LastTimeDamaged = Time.time;

        // play the damage tick sound
        hitAudio.PlaySound();

        m_WasDamagedThisFrame = true;

        animator.SetTrigger(k_AnimOnDamagedParameter);
    }

    protected override void OnDeath(GameObject whatKilledEnemy)
    {
        base.OnDeath(whatKilledEnemy);

        // spawn a particle system when dying
        var vfx = Instantiate(deathVFX, deathVFXSpawnPoint.position, Quaternion.identity);
        Destroy(vfx, 5f);

        // tells the game flow manager to handle the enemy destuction
        enemyManager.UnregisterEnemy(this);

        // this will call the OnDestroy function
        Destroy(gameObject, deathDuration);
    }
}