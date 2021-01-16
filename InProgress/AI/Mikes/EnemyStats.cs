// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Object = UnityEngine.Object;
//
// public class EnemyStats : MonoBehaviour, IOnDamage
// {
//     public float MaxHealth = 10;
//     public float score = 10;
//     public float CurrentSpeed = 50;
//     public float ThrustSpeed = 50;
//     public float CoastSpeed = 5;
//
//     public GameObject ExplosionPrefab;
//     public GameObject EngineParticle;
//     public bool ShowUnityEvent = false;
//     public UnityCustomEvents.UnityEventFloat OnDamaged;
//     public UnityCustomEvents.UnityEventEnemyStats OnDestroyed;
//     public UnityCustomEvents.UnityEventFloat OnCurrentHealthChanged;
//
//     [SerializeField] private float CurrentHealth;
//
//     public float currentHealth
//     {
//         get => CurrentHealth;
//         set
//         {
//             CurrentHealth = value;
//             OnCurrentHealthChanged.Invoke(CurrentHealth);
//         }
//     }
//
//     private void Start()
//     {
//         CurrentHealth = MaxHealth;
//         CurrentSpeed = CoastSpeed;
//     }
//
//     public void TakeDamage(float damage, GameObject owner)
//     {
//         if (currentHealth <= 0) return;
//
//         if (damage > 0) OnDamaged.Invoke(damage);
//
//         currentHealth -= damage;
//         if (currentHealth <= 0)
//             Destroy();
//     }
//
//
//     private void Destroy()
//     {
//         OnDestroyed.Invoke(this);
//         
//         Destroy(this.gameObject, Time.deltaTime);
//     }
//
//     
//
//     public void DamageEnemyText()
//     {
//         TakeDamage(1, gameObject);
//     }
//
// }
//
// public interface
//     IOnDamage
// {
//     void TakeDamage(float damage, GameObject owner);
// }