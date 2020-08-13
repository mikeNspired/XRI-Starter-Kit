using System;
using System.Collections.Generic;
using Gamekit3D;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
public class EnemyOrganicController : A_EnemyController
{
    [SerializeField] private MeleeAttack meleeAttack;

    private bool isOnRandomPosition;
    private Vector3 currentRandomPosition;

    public override Vector3 GetDestinationOnPath()
    {
        if (IsPathValid())
        {
            if (isNewDestinationSpot)
            {
                isNewDestinationSpot = false;
                Vector3 pos = patrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
                var randomPosition = Random.insideUnitSphere * 2 + pos;
                NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, 3, 1);
                currentRandomPosition = hit.position;
                // Debug.Log("isNewDestinationSpot: Getting new spot near patrol point " + currentRandomPosition);
                return currentRandomPosition;
            }

            //Debug.Log("Same position" + currentRandomPosition);

            return currentRandomPosition;

            // return patrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
        }

        if (currentRandomPosition == Vector3.zero || (transform.position - currentRandomPosition).magnitude <= pathReachingRadius)
        {
            //  Debug.Log("Going to random position");

            var randomPosition = Random.insideUnitSphere * pathReachingRadius * 4 + transform.position;
            NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, pathReachingRadius * 4, 1);
            currentRandomPosition = hit.position;
        }

        return currentRandomPosition;
    }


    protected override void OnDamaged(float damage, GameObject damageSource)
    {
        base.OnDamaged(damage, damageSource);
        // pursue the player
        // detectionModule.OnDamaged(damageSource);

        if (onDamaged != null)
        {
            onDamaged.Invoke();
        }

        m_LastTimeDamaged = Time.time;

        m_WasDamagedThisFrame = true;
        if (Random.Range(0, 7) == 1)
        {
            GetComponent<Animator>().SetTrigger("Hit");
            hitAudio.PlaySound();
        }
    }


    protected override void OnDeath(GameObject whatKilledEnemy)
    {
        base.OnDeath(whatKilledEnemy);
        if (deathVFX)
        {
            // spawn a particle system when dying
            var vfx = Instantiate(deathVFX, deathVFXSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        // tells the game flow manager to handle the enemy destuction
        enemyManager.UnregisterEnemy(this);

        deathAudio.PlaySound();

        // this will call the OnDestroy function
        var ragDoll = GetComponent<ReplaceWithRagdoll>();
        if (ragDoll)
            ragDoll.Replace();
        else
            Destroy(gameObject, deathDuration);
    }

    //ATTACK!!!
    public override bool TryAtack(Vector3 enemyPosition)
    {

        base.TryAtack(enemyPosition);
        onAttack.Invoke();

        return false;
    }


    public void PlayStep(int frontFoot)
    {
        if (frontStepAudio != null && frontFoot == 1)
            frontStepAudio.PlaySound();
        else if (backStepAudio != null && frontFoot == 0)
            backStepAudio.PlaySound();
    }

    public void AttackBegin()
    {
        // navMeshAgent.velocity = Vector3.zero;
        // navMeshAgent.isStopped = true;

        attackAudio.PlaySound();
        meleeAttack.gameObject.SetActive(true);
    }

    public void AttackEnd()
    {
        // navMeshAgent.isStopped = false;
        meleeAttack.gameObject.SetActive(false);
    }

    public void Grunt()
    {
        gruntAudio.PlaySound();
    }
}

[DefaultExecutionOrder(-1)]
public class A_EnemyController : MonoBehaviour
{
    protected PatrolPath patrolPath;

    [Header("Parameters")] [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
    public float selfDestructYHeight = -20f;

    [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
    public float deathDuration = 0f;

    [Header("Flash on hit")] [Tooltip("The material used for the body of the hoverbot")]
    public Material bodyMaterial;

    [Tooltip("The gradient representing the color of the flash on hit")] [GradientUsageAttribute(true)]
    public Gradient onHitBodyGradient;

    [Tooltip("The duration of the flash on hit")]
    public float flashOnHitDuration = 0.5f;

    [Header("VFX")] [Tooltip("The VFX prefab spawned when the enemy dies")]
    public GameObject deathVFX;

    [Tooltip("The point at which the death VFX is spawned")]
    public Transform deathVFXSpawnPoint;

    [Header("Pathing")] [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
    public float pathReachingRadius = 2f;

    public float orientationSpeed = 10f;
    public NavMeshAgent navMeshAgent { get; private set; }

    public bool getRandomPathIndex;
    public bool isNewDestinationSpot = true;
    protected int m_PathDestinationNodeIndex;
    public bool isTargetInAttackRange { get; private set; }

    public UnityAction onAttack = delegate { };
    public UnityAction onDetectedTarget = delegate { };
    public UnityAction onLostTarget = delegate { };
    public UnityAction onDamaged = delegate { };
    public UnityAction<Actor, GameObject> onDeath = delegate { };

    [SerializeField] protected DetectionModule detectionModule;

    public GameObject knownDetectedTarget => detectionModule.knownDetectedTarget;
    public bool isSeeingTarget => detectionModule.isSeeingTarget;
    public bool hadKnownTarget => detectionModule.hadKnownTarget;

    [Header("Audio")] [SerializeField] protected AudioRandomize frontStepAudio;
    [SerializeField] protected AudioRandomize backStepAudio;
    [SerializeField] protected AudioRandomize deathAudio;
    [SerializeField] protected AudioRandomize gruntAudio;
    [SerializeField] protected AudioRandomize attackAudio;
    [SerializeField] protected AudioRandomize hitAudio;


    protected List<RendererIndexData> m_BodyRenderers = new List<RendererIndexData>();
    protected MaterialPropertyBlock bodyFlashMaterialPropertyBlock;
    protected float m_LastTimeDamaged = float.NegativeInfinity;
    protected EnemyManager enemyManager;
    protected ActorsManager actorsManager;
    protected Health health;
    protected Collider[] selfColliders;
    protected bool m_WasDamagedThisFrame;
    protected Animator animator;
    const string k_AnimAttackParameter = "Attack";
    const string k_AnimOnDamagedParameter = "OnDamaged";

    [Header("Debug Display")] [Tooltip("Color of the sphere gizmo representing the path reaching range")]
    public Color pathReachingRangeColor = Color.yellow;

    [Tooltip("Color of the sphere gizmo representing the attack range")]
    public Color attackRangeColor = Color.red;

    [Tooltip("Color of the sphere gizmo representing the detection range")]
    public Color detectionRangeColor = Color.blue;

    [Tooltip("The max distance at which the enemy can attack its target")]
    public float attackRange = 10f;

    protected virtual void Start()
    {
        OnValidate();
        enemyManager.RegisterEnemy(this);

        // Subscribe to damage & death actions
        health.onDie += OnDeath;
        health.onDamaged += OnDamaged;

        // Initialize detection module
        detectionModule.onDetectedTarget += OnDetectedTarget;
        detectionModule.onLostTarget += OnLostTarget;

        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (renderer.sharedMaterials[i] == bodyMaterial)
                {
                    m_BodyRenderers.Add(new RendererIndexData(renderer, i));
                }
            }
        }

        bodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();
    }

    private void OnValidate()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>();
        if (!enemyManager)
            enemyManager = FindObjectOfType<EnemyManager>();
        if (!actorsManager)
            actorsManager = FindObjectOfType<ActorsManager>();
        if (!health)
            health = GetComponent<Health>();
        if (!navMeshAgent)
            navMeshAgent = GetComponent<NavMeshAgent>();
        if (!detectionModule)
            detectionModule = GetComponentInChildren<DetectionModule>();
        selfColliders = GetComponentsInChildren<Collider>();
    }

    protected virtual void Update()
    {
        //EnsureIsWithinLevelBounds();

        detectionModule.HandleTargetDetection(selfColliders);

        Color currentColor = onHitBodyGradient.Evaluate((Time.time - m_LastTimeDamaged) / flashOnHitDuration);

        bodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
        foreach (var data in m_BodyRenderers)
        {
            data.renderer.SetPropertyBlock(bodyFlashMaterialPropertyBlock, data.materialIndex);
        }

        m_WasDamagedThisFrame = false;

        isTargetInAttackRange = knownDetectedTarget != null && Vector3.Distance(transform.position, knownDetectedTarget.transform.position) <= attackRange;
        if (knownDetectedTarget)
            distanceshit = Vector3.Distance(transform.position, knownDetectedTarget.transform.position);
    }

    public float distanceshit;

    private void EnsureIsWithinLevelBounds()
    {
        // at every frame, this tests for conditions to kill the enemy
        if (transform.position.y < selfDestructYHeight)
        {
            Destroy(gameObject);
            return;
        }
    }

    public virtual void SetNavDestination(Vector3 destination)
    {
        if (!navMeshAgent || !enabled) return;

        // Debug.Log("SetNavDestination: " + destination + "  " + patrolPath);
        navMeshAgent.SetDestination(destination);
    }

    public virtual void UpdatePathDestination(bool inverseOrder = false)
    {
        if (IsPathValid())
        {
//            Debug.Log((transform.position - GetDestinationOnPath()).magnitude + " " + pathReachingRadius);

            // Check if reached the path destination
            if ((transform.position - GetDestinationOnPath()).magnitude <= pathReachingRadius)
            {
                // Debug.Log("Got new position!!");

                isNewDestinationSpot = true;
                if (getRandomPathIndex)
                    m_PathDestinationNodeIndex = Random.Range(0, patrolPath.pathNodes.Count);
                else
                    // increment path destination index
                    m_PathDestinationNodeIndex = (m_PathDestinationNodeIndex + 1);

                // if (m_PathDestinationNodeIndex >= patrolPath.pathNodes.Count)
                // {
                //     m_PathDestinationNodeIndex -= patrolPath.pathNodes.Count;
                // }
            }
        }
    }

    protected virtual void OnLostTarget()
    {
        onLostTarget.Invoke();
        animator.ResetTrigger(k_AnimAttackParameter);
    }

    protected virtual void OnDetectedTarget()
    {
        onDetectedTarget.Invoke();
    }

    public virtual Vector3 GetDestinationOnPath()
    {
        return Vector3.zero;
    }

    public virtual void OrientTowards(Vector3 lookPosition)
    {
        Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
        if (lookDirection.sqrMagnitude != 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * orientationSpeed);
        }
    }

    public virtual bool TryAtack(Vector3 position)
    {
        animator.SetTrigger(k_AnimAttackParameter);

        return false;
    }


    protected virtual void OnDamaged(float damage, GameObject damageSource)
    {
        animator.SetTrigger(k_AnimOnDamagedParameter);
    }

    protected virtual void OnDeath(GameObject whatKilledEnemy)
    {
        onDeath.Invoke(GetComponent<Actor>(), whatKilledEnemy);

        deathAudio.transform.SetParent(null, true);
        deathAudio.PlaySound();
    }

    protected bool IsPathValid()
    {
        return patrolPath && patrolPath.pathNodes.Count > 0 && m_PathDestinationNodeIndex < patrolPath.pathNodes.Count;
    }

    public void ResetPathDestination()
    {
        m_PathDestinationNodeIndex = 0;
    }

    public void SetNewPath(PatrolPath newPath)
    {
        patrolPath = newPath;
        isNewDestinationSpot = true;
    }

    public virtual void SetPathDestinationToClosestNode()
    {
        if (IsPathValid())
        {
            int closestPathNodeIndex = 0;
            for (int i = 0; i < patrolPath.pathNodes.Count; i++)
            {
                float distanceToPathNode = patrolPath.GetDistanceToNode(transform.position, i);
                if (distanceToPathNode < patrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                {
                    closestPathNodeIndex = i;
                }
            }

            m_PathDestinationNodeIndex = closestPathNodeIndex;
        }
        else
        {
            m_PathDestinationNodeIndex = 0;
        }
    }

    //Called from LeavingAnimationState
    public void StopNavMovement()
    {
        navMeshAgent.velocity = Vector3.zero;
        navMeshAgent.isStopped = true;
    }

    //Called from EnterAnimationState
    public void StartNavMovement()
    {
        navMeshAgent.isStopped = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Path reaching range
        Gizmos.color = pathReachingRangeColor;
        Gizmos.DrawWireSphere(transform.position, pathReachingRadius);
        // Attack range
        Gizmos.color = attackRangeColor;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (detectionModule != null)
        {
            // Detection range
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionModule.detectionRange);
        }
    }
}

[System.Serializable]
public struct RendererIndexData
{
    public Renderer renderer;
    public int materialIndex;

    public RendererIndexData(Renderer renderer, int index)
    {
        this.renderer = renderer;
        this.materialIndex = index;
    }
}