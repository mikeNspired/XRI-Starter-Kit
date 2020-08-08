using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(A_EnemyController))]
[DefaultExecutionOrder(1)]
public class EnemyMobile : MonoBehaviour
{
    public enum AIState
    {
        Patrol,
        Follow,
        Attack,
    }
    [SerializeField] private DetectionModule detectionModule;

    public Animator animator;

    [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")] [Range(0f, 1f)]
    public float attackStopDistanceRatio = 0.5f;

    [Tooltip("The random hit damage effects")]
    public ParticleSystem[] randomHitSparks;

    public ParticleSystem[] onDetectVFX;
    public AudioClip onDetectSFX;

    [Header("Sound")] public AudioClip MovementSound;
    public MinMaxFloat PitchDistortionMovementSpeed;

    public AIState aiState { get; private set; }
    A_EnemyController m_EnemyController;
    AudioSource m_AudioSource;

    const string k_AnimMoveSpeedParameter = "MoveSpeed";
    const string k_AnimAttackParameter = "Attack";
    const string k_AnimAlertedParameter = "Alerted";

    void Start()
    {
        detectionModule = GetComponentInChildren<DetectionModule>();
        m_EnemyController = GetComponent<A_EnemyController>();
        m_EnemyController.onAttack += OnAttack;
        m_EnemyController.onDetectedTarget += OnDetectedTarget;
        m_EnemyController.onLostTarget += OnLostTarget;
        m_EnemyController.SetPathDestinationToClosestNode();
        m_EnemyController.onDamaged += OnDamaged;

        // Start patrolling
        aiState = AIState.Patrol;

        // adding a audio source to play the movement sound on it
        m_AudioSource = GetComponent<AudioSource>();
        if (!m_AudioSource) return;
        m_AudioSource.clip = MovementSound;
        m_AudioSource.Play();
    }

    void Update()
    {
        UpdateAIStateTransitions();
        UpdateCurrentAIState();

        float moveSpeed = m_EnemyController.navMeshAgent.velocity.magnitude;
        moveSpeed = Remap(moveSpeed, 0, m_EnemyController.navMeshAgent.speed, 0, 1);
        // Update animator speed parameter
        animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);

        if (m_AudioSource)
            // changing the pitch of the movement sound depending on the movement speed
            m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.min, PitchDistortionMovementSpeed.max, moveSpeed / m_EnemyController.navMeshAgent.speed);
    }

    public float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    void UpdateAIStateTransitions()
    {
        // Handle transitions 
        switch (aiState)
        {
            case AIState.Follow:
                // Transition to attack when there is a line of sight to the target
                if (m_EnemyController.isSeeingTarget && m_EnemyController.isTargetInAttackRange)
                {
                    aiState = AIState.Attack;
                    m_EnemyController.SetNavDestination(transform.position);
                }

                break;
            case AIState.Attack:
                // Transition to follow when no longer a target in attack range
                if (!m_EnemyController.isTargetInAttackRange)
                {
                    aiState = AIState.Follow;
                }

                break;
        }
    }

    void UpdateCurrentAIState()
    {
        // Handle logic 
        switch (aiState)
        {
            case AIState.Patrol:
                m_EnemyController.UpdatePathDestination();
                m_EnemyController.SetNavDestination(m_EnemyController.GetDestinationOnPath());
                break;
            case AIState.Follow:
                m_EnemyController.SetNavDestination(m_EnemyController.knownDetectedTarget.transform.position);
                m_EnemyController.OrientTowards(m_EnemyController.knownDetectedTarget.transform.position);
                break;
            case AIState.Attack:

                if (Vector3.Distance(m_EnemyController.knownDetectedTarget.transform.position, detectionModule.detectionSourcePoint.position)
                    >= (attackStopDistanceRatio * m_EnemyController.attackRange))
                {
                    m_EnemyController.SetNavDestination(m_EnemyController.knownDetectedTarget.transform.position);
                }
                else
                {
                    m_EnemyController.SetNavDestination(transform.position);
                }

                m_EnemyController.OrientTowards(m_EnemyController.knownDetectedTarget.transform.position);
                m_EnemyController.TryAtack(m_EnemyController.knownDetectedTarget.transform.position);
                break;
        }
    }

    void OnAttack()
    {
        animator.SetTrigger(k_AnimAttackParameter);
    }

    void OnDetectedTarget()
    {
        if (aiState == AIState.Patrol)
        {
            aiState = AIState.Follow;
        }

        if (onDetectSFX && onDetectSFX.length > 0)
        {
            for (int i = 0; i < onDetectVFX.Length; i++)
            {
                onDetectVFX[i].Play();
            }

            if (onDetectSFX)
            {
                m_AudioSource.PlayOneShot((onDetectSFX));
            }

            animator.SetBool(k_AnimAlertedParameter, true);
        }
    }

    void OnLostTarget()
    {
        if (aiState == AIState.Follow || aiState == AIState.Attack)
        {
            aiState = AIState.Patrol;
        }

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Stop();
        }
        animator.SetBool(k_AnimAlertedParameter, false);
    }

    void OnDamaged()
    {
        if (randomHitSparks.Length > 0)
        {
            int n = Random.Range(0, randomHitSparks.Length - 1);
            randomHitSparks[n].Play();
        }
    }
}