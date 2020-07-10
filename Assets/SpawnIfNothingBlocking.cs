﻿// Copyright (c) MikeNspired. All Rights Reserved.

using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class SpawnIfNothingBlocking : MonoBehaviour
    {
        public bool isActive = true;
        [SerializeField] private GameObject Prefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnTimer = 5;

        private bool hitDetect;
        private float currentTimer = 0;

        private void FixedUpdate()
        {
            if (!isActive) return;

            if (hitDetect)
                currentTimer = 0;
            else
            {
                currentTimer += Time.deltaTime;
                if (currentTimer >= spawnTimer)
                {
                    Spawn();
                    currentTimer = 0;
                }
            }
        }

        private void Spawn() => Instantiate(Prefab, spawnPoint.position, spawnPoint.rotation);

        private void OnTriggerStay(Collider other) => hitDetect = true;

        private void OnTriggerExit(Collider other) => hitDetect = false;
    }
}