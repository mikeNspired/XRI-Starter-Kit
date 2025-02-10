using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Applies damage on collision based on the impact force of a sword swing.
    /// The collision's relative velocity is used to determine damage.
    /// When the max impact force is reached, the sword deals the specified damage.
    /// The target must implement IDamageable to receive damage.
    /// </summary>
    public class CollisionDamage : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Minimum collision force required to apply damage.")]
        [SerializeField] private float minImpactForce = 1f;

        [Tooltip("Maximum collision force considered for damage scaling. At this force, the sword deals full damage.")]
        [SerializeField] private float maxImpactForce = 10f;

        [Tooltip("Damage dealt when max impact force is reached.")]
        [SerializeField] private float damage = 10f;

        [Tooltip("Optionally use a damage curve to modify the normalized force before calculating damage.")]
        [SerializeField] private bool useDamageCurve = false;

        [Tooltip("If using a damage curve, this curve is used to evaluate damage based on normalized force (0 to 1).")]
        [SerializeField] private AnimationCurve damageCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("Cooldown (in seconds) between consecutive damage applications to prevent multiple hits in quick succession.")]
        [SerializeField] private float timeBetweenDamage = 0.1f;

        [Header("Damageable Layers")]
        [Tooltip("Layers that can be damaged by this sword.")]
        [SerializeField] private LayerMask damageableLayers;

        [Header("Debug Options")]
        [Tooltip("If enabled, prints collision relative velocity and the object hit to the console for debugging purposes.")]
        [SerializeField] private bool debugVelocity = false;

        // Timer to enforce a cooldown between damage applications.
        private float lastDamageTime = 0f;

        private void OnCollisionEnter(Collision collision)
        {
            // Enforce a cooldown between consecutive damage applications.
            if (Time.time - lastDamageTime < timeBetweenDamage)
                return;

            // Only process collisions with objects on the specified layers.
            if ((damageableLayers.value & (1 << collision.gameObject.layer)) == 0)
                return;

            // Get the magnitude of the collision's relative velocity.
            float impactForce = collision.relativeVelocity.magnitude;

            // Debug print the velocity and the object hit if enabled.
            if (debugVelocity)
            {
                Debug.Log($"Collision relative velocity: {impactForce}, hit object: {collision.gameObject.name}", gameObject);
            }

            // If the impact force is below the minimum threshold, do not apply damage.
            if (impactForce < minImpactForce)
                return;

            // Clamp the impact force to the maximum allowed value.
            impactForce = Mathf.Clamp(impactForce, minImpactForce, maxImpactForce);

            // Normalize the force between 0 and 1.
            float normalizedForce = (impactForce - minImpactForce) / (maxImpactForce - minImpactForce);

            // Optionally modify the normalized force using a curve.
            if (useDamageCurve)
            {
                normalizedForce = damageCurve.Evaluate(normalizedForce);
            }

            // Calculate the final damage based on the normalized force.
            // At maxImpactForce, finalDamage will equal the defined damage value.
            float finalDamage = normalizedForce * damage;

            // Try to find an IDamageable component on the hit object or its parent.
            IDamageable damageable = collision.collider.GetComponent<IDamageable>()
                                     ?? collision.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(finalDamage, gameObject);
                lastDamageTime = Time.time;
            }
        }
    }
}
