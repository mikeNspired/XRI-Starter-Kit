using System;

using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 5;
        private float currentHealth;
        private IEnemy enemy;
        public event Action<float> OnTakeDamage;

        public float MaxHealth => maxHealth;

        [SerializeField] private float damageCooldown = 0.1f; // Cooldown between damage instances
        private float lastDamageTime;

        private void Awake()
        {
            currentHealth = maxHealth;
            enemy = GetComponent<IEnemy>();
            lastDamageTime = -damageCooldown; // Ensures first hit is not delayed
        }

        
        public void TestDamage()
        {
            TakeDamage(5, gameObject);
        }

        public void TakeDamage(float damage, GameObject damager)
        {
            if (Time.time - lastDamageTime < damageCooldown)
            {
                return; // Ignore damage if within cooldown period
            }

            lastDamageTime = Time.time;
            OnTakeDamage?.Invoke(damage);
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            if (currentHealth <= 0) enemy.Die();
        }

        public void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = Mathf.Max(0, newMaxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        public void SetCurrentHealth(float newHealth)
        {
            currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        }

        public void AddHealth(float amount)
        {
            currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        }
    }
}