using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MikeNspired.XRIStarterKit
{
    public class AreaDamage : MonoBehaviour
    {
        [Tooltip("If true, the damage is triggered as soon as the object is enabled.")] [SerializeField]
        private bool damageOnEnabled;

        [Header("Initial Explosion Damage Settings")]
        [Tooltip("Damage dealt immediately upon trigger.")]
        [SerializeField]
        private float initialDamageAmount = 100f;

        [Tooltip("Radius for the initial explosion damage.")] [SerializeField]
        private float radius = 5f;

        [Header("Damage Over Time (DOT) Settings")]
        [Tooltip("Toggle for applying a residual DOT effect after the initial explosion.")]
        [SerializeField]
        private bool damageOverTime = false;

        [Tooltip("Starting damage amount per tick for the DOT effect.")] [SerializeField]
        private float dotDamageAmount = 10f;

        [Tooltip("Total duration (in seconds) of the DOT effect.")] [SerializeField]
        private float duration = 5f;

        [Tooltip("Time interval (in seconds) between each DOT damage tick.")] [SerializeField]
        private float tickInterval = 1f;

        [Tooltip("Option to gradually reduce DOT damage over time.")] [SerializeField]
        private bool weakenOverTime = false;

        [Tooltip("DOT damage at the end of the effect (if weakening is enabled).")] [SerializeField]
        private float finalDotDamageAmount = 0f;

        [Tooltip("Option to gradually reduce the effective radius over time during the DOT effect.")] [SerializeField]
        private bool shrinkOverTime = false;

        [Tooltip("Effective radius at the end of the DOT effect if shrinkOverTime is enabled.")] [SerializeField]
        private float finalRadius = 0f;

        [Header("Other")] [Tooltip("Option to reduce damage based on distance")] [SerializeField]
        private bool damageFallOff = false;

        [Tooltip("Animation curve used to scale damage based on distance. " +
                 "X-axis is the normalized distance (0 = center, 1 = edge), " +
                 "Y-axis is the damage multiplier. Default: 1 at center, 0.5 at edge.")]
        [SerializeField]
        private AnimationCurve damageFalloffCurve = new(
            new Keyframe(0f, 1f), // At zero distance: 100% damage
            new Keyframe(1f, 0.5f) // At full normalized distance (edge): 50% damage
        );

        [Tooltip("Layers that will be affected by the damage.")] [SerializeField]
        private LayerMask damageableLayers;

        private void OnEnable()
        {
            if (damageOnEnabled)
                TriggerDamage();
        }

        /// <summary>
        /// Triggers the damage effect.
        /// Always applies an initial explosion damage, then optionally starts the DOT effect.
        /// </summary>
        public void TriggerDamage()
        {
            // Apply the initial explosion damage.
            DealDamageTick(initialDamageAmount, radius);

            // If damage-over-time is enabled, start the DOT coroutine.
            if (damageOverTime)
            {
                StartCoroutine(DamageOverTimeRoutine());
            }
        }

        /// <summary>
        /// Coroutine that applies DOT damage ticks over time.
        /// It optionally weakens the damage or shrinks the effective radius over the duration.
        /// </summary>
        private IEnumerator DamageOverTimeRoutine()
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                // For the DOT ticks, use dotDamageAmount as the starting value.
                float currentDamage = dotDamageAmount;
                float currentRadius = radius;

                if (weakenOverTime)
                {
                    // Lerp from the starting DOT damage to the final DOT damage over time.
                    currentDamage = Mathf.Lerp(dotDamageAmount, finalDotDamageAmount, elapsedTime / duration);
                }

                if (shrinkOverTime)
                {
                    // Lerp from the initial radius to the final radius over time.
                    currentRadius = Mathf.Lerp(radius, finalRadius, elapsedTime / duration);
                }

                DealDamageTick(currentDamage, currentRadius);

                // Wait for the next tick and update elapsed time.
                yield return new WaitForSeconds(tickInterval);
                elapsedTime += tickInterval;
            }
        }

        /// <summary>
        /// Applies damage to all damageable objects within the given radius.
        /// Damage is scaled based on distance using the provided AnimationCurve.
        /// </summary>
        /// <param name="currentDamage">Damage amount for this tick.</param>
        /// <param name="currentRadius">Effective radius for this tick.</param>
        private HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        private void DealDamageTick(float currentDamage, float currentRadius)
        {
            // Retrieve all colliders within the current radius that are on the specified layers.
            Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, damageableLayers);

            foreach (Collider hit in hits)
            {
                // Try to get the IDamageable component (either directly or via a parent).
                IDamageable damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable == null)
                    continue;

                // Skip if we've already hit this target in a previous tick.
                if (damagedTargets.Contains(damageable))
                    continue;

                // Determine a multiplier based on how far the target is from the effect center.
                Vector3 closestPoint = hit.ClosestPoint(transform.position);
                float distance = Vector3.Distance(transform.position, closestPoint);

                float multiplier = 1;
                if (damageFallOff)
                {
                    float normalizedDistance = (currentRadius > 0f) ? distance / currentRadius : 0f;
                    multiplier = damageFalloffCurve.Evaluate(normalizedDistance);
                }

                // Damage the target and record that we've hit it.
                damagedTargets.Add(damageable);
                damageable.TakeDamage(currentDamage * multiplier, gameObject);
            }

            damagedTargets.Clear();
        }


        private void OnDrawGizmosSelected()
        {
            // Visualize the initial explosion radius in the Scene view.
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
