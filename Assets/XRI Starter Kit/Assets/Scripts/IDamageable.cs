using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public interface IDamageable
    {
        void TakeDamage(float damage, GameObject damager);
    }

    public interface IImpactType
    {
        ImpactType GetImpactType();
        bool ShouldReparent { get; }
    }

    public enum ImpactType
    {
        Metal,
        Flesh,
        Wood,
        Neutral
    }
}