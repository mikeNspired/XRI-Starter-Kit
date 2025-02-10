using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class ArrowSpawnObjectInHandOnGrab : AutoSpawnObjectInHandOnGrab
    {
        [SerializeField] private PullInteraction pullInteraction;

        public override void TrySpawn()
        {
            if (pullInteraction.NotchedArrow) return;
            base.TrySpawn();
        }
    }
}