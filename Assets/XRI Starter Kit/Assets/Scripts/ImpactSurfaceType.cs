using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class ImpactSurfaceType : MonoBehaviour, IImpactType
    {
        [SerializeField] private ImpactType impactType;
        public ImpactType GetImpactType() => impactType;
    }
}