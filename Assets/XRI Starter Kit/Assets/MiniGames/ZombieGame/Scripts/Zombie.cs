using UnityEngine;
using System;

using System.Collections;
using Unity.XR.CoreUtils;

namespace MikeNspired.XRIStarterKit
{
    public class Zombie : MonoBehaviour, IEnemy
    {
        #region Fields

        [Header("Health & Effects")] [Tooltip("Enemy health component.")] [SerializeField]
        private EnemyHealth enemyHealth;

        [Tooltip("Sound controller for NPCs.")] [SerializeField]
        private NPCSoundController soundController;

        [Tooltip("Damage text prefab.")] [SerializeField]
        private DamageText damageText;

        [Tooltip("Spawn point for damage text.")] [SerializeField]
        private Transform damageTextSpawn;

        [Header("Movement Settings")] [Tooltip("Maximum movement speed of the zombie.")] [SerializeField]
        private float maxSpeed = 1f;

        [Tooltip("Time it takes to reach max speed.")] [SerializeField]
        private float timeToMaxSpeed = 2f;

        [Header("Emerge & Sink Settings")] [Tooltip("Duration for emerging from the ground.")] [SerializeField]
        private float emergeDuration = 2f;

        [Tooltip("Duration for sinking into the ground on death.")] [SerializeField]
        private float sinkDuration = 2f;

        [Tooltip("Vertical distance for sinking/emerging.")] [SerializeField]
        private float sinkDistance = 2f;

        [Tooltip("Delay before starting the emerge animation.")] [SerializeField]
        private float startAnimationDelay = 1f;

        [Tooltip("Particle system played on spawn.")] [SerializeField]
        private ParticleSystem spawnParticles;

        [Tooltip("Animator component for controlling animations.")] [SerializeField]
        private Animator animator;

        [Tooltip("Renderer for dissolve effect.")] [SerializeField]
        private Renderer mRenderer;

        [Header("Other Settings")] [Tooltip("Distance at which the zombie can attack the player.")] [SerializeField]
        private float attackRange = 0.5f;

        [Tooltip("Chance for the zombie to scream during its approach.")] [SerializeField]
        private float screamChance = 0.05f;

        [Tooltip("Chance for the zombie to react to hits.")] [SerializeField]
        private float hitAnimationChance = 0.1f;

        private bool willScream;
        private bool hasScreamed;
        private float initialDistanceToPlayer;

        private Transform player;
        private float accelerateTimer;
        private bool isDead;
        private bool isAttacking;

        private bool isEmerging;
        private bool isSinking;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Scream = Animator.StringToHash("Scream");
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int Start1 = Animator.StringToHash("Start");
        private static readonly int DissolveAmountHash = Shader.PropertyToID("_DissolveAmount");

        public static event Action<Zombie> OnZombieDied;
        public static event Action OnZombieAttacked;

        #endregion

        #region Unity Methods

        private void Start()
        {
            player = FindFirstObjectByType<XROrigin>().transform;

            if (enemyHealth != null)
            {
                enemyHealth.OnTakeDamage += _ => soundController.PlayImpact();
                enemyHealth.OnTakeDamage += OnEnemyTakeDamage;
            }
        }

        private void Update()
        {
            if (isEmerging || isSinking || isDead)
                return;

            if (!isAttacking)
                ChasePlayer();
        }

        #endregion

        #region Initialization & Emergence

        /// <summary>
        /// Initializes the zombie's movement settings and scream decision.
        /// </summary>
        public void Initialize(float speed, float timeToSpeed)
        {
            maxSpeed = speed;
            timeToMaxSpeed = timeToSpeed;

            willScream = UnityEngine.Random.value <= screamChance;
            hasScreamed = false;
            isAttacking = false;

            Vector3 belowGroundPosition = transform.position;
            belowGroundPosition.y -= sinkDistance;
            transform.position = belowGroundPosition;

            StartCoroutine(EmergeRoutine());
        }

        private IEnumerator EmergeRoutine()
        {
            isEmerging = true;
            FacePlayerInstantly();

            Vector3 startPos = transform.position;
            Vector3 endPos = new Vector3(startPos.x, startPos.y + sinkDistance, startPos.z);

            spawnParticles.transform.SetParent(null);
            spawnParticles.transform.position = endPos;
            soundController.PlaySpawn();

            float elapsed = 0f;

            while (elapsed < emergeDuration)
            {
                float t = elapsed / emergeDuration;
                float easeOutT = t * (2f - t);
                transform.position = Vector3.Lerp(startPos, endPos, easeOutT);

                if (elapsed > startAnimationDelay)
                    animator.SetBool(Start1, true);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos;

            if (player)
                initialDistanceToPlayer = Vector3.Distance(transform.position, player.position);

            isEmerging = false;
            accelerateTimer = 0f;

            if (animator)
                animator.SetFloat(Speed, 0f);
        }

        #endregion

        #region Movement & Chase

        private void ChasePlayer()
        {
            if (!player) return;

            accelerateTimer += Time.deltaTime;
            float normalizedSpeed = Mathf.Clamp(accelerateTimer / timeToMaxSpeed, 0, maxSpeed);

            Vector3 direction = player.position - transform.position;
            float distance = direction.magnitude;

            Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z);

            if (willScream && !hasScreamed && initialDistanceToPlayer > 0f && distance <= initialDistanceToPlayer * 0.95f)
            {
                animator.SetTrigger(Scream);
                hasScreamed = true;
                maxSpeed += .4f;
                enemyHealth.SetMaxHealth(enemyHealth.MaxHealth * 2);
                enemyHealth.AddHealth(enemyHealth.MaxHealth / 2);
            }

            if (distance <= attackRange)
            {
                normalizedSpeed = 0f;
                if (!isAttacking)
                    AttemptAttack();
            }

            if (horizontalDirection.sqrMagnitude > 0.001f & !isAttacking)
                transform.rotation = Quaternion.LookRotation(horizontalDirection);

            if (animator)
                animator.SetFloat(Speed, normalizedSpeed);
        }

        private void FacePlayerInstantly()
        {
            if (!player) return;

            Vector3 dirToPlayer = player.position - transform.position;
            dirToPlayer.y = 0;

            if (dirToPlayer.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dirToPlayer);
        }

        #endregion

        #region Attack

        private void AttemptAttack()
        {
            if (isDead) return;

            isAttacking = true;
            animator.SetTrigger(Attack);
        }

        /// <summary>
        /// Called via an animation event at the end of the attack animation.
        /// Resets the attack state and checks if the attack connects.
        /// </summary>
        public void AttackCompleted()
        {
            if (player && Vector3.Distance(player.position, transform.position) <= attackRange + 0.02f)
                OnZombieAttacked?.Invoke();

            isAttacking = false;
            accelerateTimer = 0f;
        }

        #endregion

        #region Death & Damaged

        
        public void Die()
        {
            if (isDead) return;

            isDead = true;
            OnZombieDied?.Invoke(this);

            if (animator)
                animator.SetTrigger(UnityEngine.Random.Range(0, 2) == 0 ? "Death1" : "Death2");

            soundController.PlayDeath();
            soundController.SetRandomVocalEnabled(false);
            StartCoroutine(SinkRoutine());
        }

        private void OnEnemyTakeDamage(float x)
        {
            if (isDead) return;

            Instantiate(damageText, damageTextSpawn.position, Quaternion.identity, damageTextSpawn)
                .SetText(x.ToString("f1"));


            if (UnityEngine.Random.value <= hitAnimationChance)
                animator.SetTrigger(Hit);
        }

        private IEnumerator SinkRoutine()
        {
            yield return new WaitForSeconds(3f);

            isSinking = true;

            Vector3 startPos = transform.position;
            Vector3 endPos = new Vector3(startPos.x, startPos.y - sinkDistance, startPos.z);

            float elapsed = 0f;

            while (elapsed < sinkDuration)
            {
                float t = elapsed / sinkDuration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos;
            DestroyZombie();
        }

        public void FadeAndDestroy()
        {
            StopAllCoroutines();
            StartCoroutine(AnimateAndDestroy());
        }

        private IEnumerator AnimateAndDestroy()
        {
            float duration = 2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                mRenderer.material.SetFloat(DissolveAmountHash, Mathf.Lerp(0, 1, t));
                elapsed += Time.deltaTime;
                yield return null;
            }

            mRenderer.material.SetFloat(DissolveAmountHash, 1);
            DestroyZombie();
        }

        private void DestroyZombie()
        {
            spawnParticles.transform.SetParent(transform);
            Destroy(gameObject);
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
#endif

        #endregion
    }

    public interface IEnemy
    {
        void Die();
    }
}