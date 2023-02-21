using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public interface IDamageable
    {
        void TakeDamage(float damage, GameObject damager);
    }

    public interface IImpactType
    {
        ImpactType GetImpactType();
    }

    public enum ImpactType
    {
        Metal,
        Flesh,
        Wood,
        Neutral
    }
}