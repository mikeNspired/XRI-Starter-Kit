using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class ImpactSurfaceType : MonoBehaviour, IImpactType
    {
        [SerializeField] private ImpactType impactType;
        public ImpactType GetImpactType() => impactType;
        [SerializeField] private bool shouldReparent; 
        public bool ShouldReparent => shouldReparent;
    }
}