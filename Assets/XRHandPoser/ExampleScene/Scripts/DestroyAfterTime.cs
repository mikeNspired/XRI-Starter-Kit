// Copyright (c) MikeNspired. All Rights Reserved.

using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class DestroyAfterTime : MonoBehaviour
    {
        public float timeTillDestroy = 1f;
        [SerializeField] private bool destroyAfterFrame = false;

        private void Start()
        {
            if (!destroyAfterFrame)
                Invoke(nameof(DestroyThis), timeTillDestroy);
            else
                Invoke(nameof(DestroyThis), Time.deltaTime);
        }

        private void DestroyThis() => DestroyImmediate(gameObject);
    }
}