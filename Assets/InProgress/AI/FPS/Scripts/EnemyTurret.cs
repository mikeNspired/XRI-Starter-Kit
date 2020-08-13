using System.Collections;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;

[RequireComponent(typeof(RobotEnemyController))]
public class EnemyTurret : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Attack,
    }

    public Transform turretPivot;
    public Transform turretAimPoint;
    public Animator animator;
    public float aimRotationSharpness = 5f;
    public float lookAtRotationSharpness = 2.5f;
    public float detectionFireDelay = 1f;
    public float aimingTransitionBlendTime = 1f;
    public WeaponController weapon;

    [Tooltip("The random hit damage effects")]
    public ParticleSystem[] randomHitSparks;

    public ParticleSystem[] onDetectVFX;
    public AudioClip onDetectSFX;
    public AudioSource audioSource;
    public Collider damageHitBox;

    public AIState aiState { get; private set; }

    RobotEnemyController mRobotEnemyController;

    Health m_Health;
    Quaternion m_RotationWeaponForwardToPivot;
    float m_TimeStartedDetection;
    float m_TimeLostDetection;
    Quaternion m_PreviousPivotAimingRotation;
    Quaternion m_PivotAimingRotation;

    const string k_AnimOnDamagedParameter = "OnDamaged";
    const string k_AnimIsActiveParameter = "IsActive";

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        weapon = GetComponentInChildren<WeaponController>();
        weapon.onShoot += OnShoot;
        m_Health = GetComponent<Health>();
        m_Health.onDamaged += OnDamaged;
        m_Health.onDamaged += CheckIfPlayerTryingToActivate;

        mRobotEnemyController = GetComponent<RobotEnemyController>();

        mRobotEnemyController.onDetectedTarget += OnDetectedTarget;
        mRobotEnemyController.onLostTarget += OnLostTarget;

        // Remember the rotation offset between the pivot's forward and the weapon's forward
        m_RotationWeaponForwardToPivot = Quaternion.Inverse(weapon.weaponMuzzle.rotation) * turretPivot.rotation;

        // Start with idle
        aiState = AIState.Idle;

        m_TimeStartedDetection = Mathf.NegativeInfinity;
        m_PreviousPivotAimingRotation = turretPivot.rotation;
    }

    private void CheckIfPlayerTryingToActivate(float arg0, GameObject bullet)
    {
        if (!bullet.GetComponent<SimpleCollisionDamage>() || isRecovering) return;

        animator.SetTrigger("Activate");
        isDeactivated = false;

        if (mRobotEnemyController.knownDetectedTarget)
            OnDetectedTarget();
    }

    public float currentEnergy = 100, maxEnergy = 100, timeTillRecovery;
    public bool isDeactivated, isRecovering;

    private void OnShoot()
    {
        currentEnergy--;
        if (currentEnergy <= 0)
        {
            animator.SetTrigger("Deactivate");
            isDeactivated = true;
            isRecovering = true;
            OnLostTarget();
            StartCoroutine(Recovery());
            damageHitBox.enabled = false;
        }
    }

    IEnumerator Recovery()
    {
        float currentTimer = 0;
        while (currentTimer <= timeTillRecovery + Time.deltaTime)
        {
            yield return new WaitForSeconds(Time.deltaTime);
            currentEnergy = Mathf.Lerp(0, maxEnergy, currentTimer / timeTillRecovery);
            currentTimer += Time.deltaTime;
        }
        CanActivate();
    }

    private void CanActivate()
    {
        damageHitBox.enabled = true;
        isRecovering = false;
        currentEnergy = maxEnergy;
    }

    void Update()
    {
        if (isDeactivated) return;
        UpdateCurrentAIState();
    }

    void LateUpdate()
    {
        if (isDeactivated) return;
        UpdateTurretAiming();
    }

    void UpdateCurrentAIState()
    {
        if (!mRobotEnemyController.isTargetInAttackRange) return;

        // Handle logic 
        switch (aiState)
        {
            case AIState.Attack:
                bool mustShoot = Time.time > m_TimeStartedDetection + detectionFireDelay;
                // Calculate the desired rotation of our turret (aim at target)
                Vector3 directionToTarget = (mRobotEnemyController.knownDetectedTarget.transform.position - turretAimPoint.position).normalized;
                Quaternion offsettedTargetRotation = Quaternion.LookRotation(directionToTarget) * m_RotationWeaponForwardToPivot;
                m_PivotAimingRotation = Quaternion.Slerp(m_PreviousPivotAimingRotation, offsettedTargetRotation, (mustShoot ? aimRotationSharpness : lookAtRotationSharpness) * Time.deltaTime);

                // shoot
                if (mustShoot)
                {
                    Vector3 correctedDirectionToTarget = (m_PivotAimingRotation * Quaternion.Inverse(m_RotationWeaponForwardToPivot)) * Vector3.forward;
                    mRobotEnemyController.TryAtack(turretAimPoint.position + correctedDirectionToTarget);
                }

                break;
        }
    }

    void UpdateTurretAiming()
    {
        switch (aiState)
        {
            case AIState.Attack:
                turretPivot.rotation = m_PivotAimingRotation;
                break;
            default:
                // Use the turret rotation of the animation
                turretPivot.rotation = Quaternion.Slerp(m_PivotAimingRotation, turretPivot.rotation, (Time.time - m_TimeLostDetection) / aimingTransitionBlendTime);
                break;
        }

        m_PreviousPivotAimingRotation = turretPivot.rotation;
    }

    void OnDamaged(float dmg, GameObject source)
    {
        if (randomHitSparks.Length > 0)
        {
            int n = Random.Range(0, randomHitSparks.Length - 1);
            randomHitSparks[n].Play();
        }

        animator.SetTrigger(k_AnimOnDamagedParameter);
    }

    void OnDetectedTarget()
    {
        if (isDeactivated) return;

        if (aiState == AIState.Idle)
        {
            aiState = AIState.Attack;
        }

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Play();
        }

        if (onDetectSFX && audioSource)
        {
            audioSource.PlayOneShot(onDetectSFX);
        }

        animator.SetBool(k_AnimIsActiveParameter, true);
        m_TimeStartedDetection = Time.time;
    }

    void OnLostTarget()
    {
        if (aiState == AIState.Attack)
        {
            aiState = AIState.Idle;
        }

        for (int i = 0; i < onDetectVFX.Length; i++)
        {
            onDetectVFX[i].Stop();
        }

        animator.SetBool(k_AnimIsActiveParameter, false);
        m_TimeLostDetection = Time.time;
    }
}