// Author MikeNspired. 

using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class ObjectSpawner : MonoBehaviour
    {
        public bool isActive = true;
        [SerializeField] private bool onlySpawnIfRoom = true;
        [SerializeField] private GameObject Prefab = null;
        [SerializeField] private Transform spawnPoint = null;
        [SerializeField] private float spawnTimer = 5;

        private bool hitDetect;
        private float currentTimer = 0;

        private void FixedUpdate()
        {
            if (!isActive) return;

            if (!onlySpawnIfRoom)
            {
                TickTimerAndSpawn();
                return;
            }

            if (hitDetect)
                currentTimer = 0;
            else
                TickTimerAndSpawn();
        }

        private void TickTimerAndSpawn()
        {
            currentTimer += Time.deltaTime;
            if (!(currentTimer >= spawnTimer)) return;
            Spawn();
            currentTimer = 0;
        }
        private void Spawn() => Instantiate(Prefab, spawnPoint.position, spawnPoint.rotation);

        private void OnTriggerStay(Collider other) => hitDetect = true;

        private void OnTriggerExit(Collider other) => hitDetect = false;
    }
}