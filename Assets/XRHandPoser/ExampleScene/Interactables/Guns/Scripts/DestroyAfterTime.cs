// Copyright (c) MikeNspired. All Rights Reserved.
using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class DestroyAfterTime : MonoBehaviour
    {
        public float Time = 1f;
        private void Start() => Invoke(nameof(DestroyThis), Time);
        private void DestroyThis() => DestroyImmediate(gameObject);

    }
}