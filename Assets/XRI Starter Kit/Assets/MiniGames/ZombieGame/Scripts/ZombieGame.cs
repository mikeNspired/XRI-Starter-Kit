using UnityEngine;
using System.Collections;

using TMPro;
using Random = UnityEngine.Random;

namespace MikeNspired.XRIStarterKit
{
    public class ZombieGame : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TextMeshPro gameOverText;

        [SerializeField] private TextMeshPro scoreText;
        [SerializeField] private AudioSource gameOverAudio;
        [SerializeField] private AudioSource gameAudio;

        [Header("Level Settings")] [SerializeField]
        private int spawnIncreasePerLevel = 2;

        [SerializeField] private float spawnDurationIncreasePerLevel = 2f;
        [SerializeField] private float timeBetweenWaves = 1.5f;

        [Header("Spawn Points")] [SerializeField]
        private Transform spawnPoint1;

        [SerializeField] private Transform spawnPoint2;
        [SerializeField] private GameObject zombiePrefab;

        [Header("Lighting")] [SerializeField] private SetWorldLighting worldLighting;
        [SerializeField] private ListActivator lightListActivator;

        [Header("Spawn Randomization")]
        [Tooltip("How wide to scatter zombies perpendicular to the line from spawnPoint1 to spawnPoint2.")]
        [SerializeField]
        private float spawnVariation = 1f;

        [Tooltip("Scale variation (e.g., 0.2 means ±20%).")] [SerializeField]
        private float scaleVariationAmount = 0.2f;

        [Header("Zombie Movement Randomization")] [SerializeField]
        private float minSpeed = 0.65f;

        [SerializeField] private float maxSpeed = 1f;
        [SerializeField] private float speedVariation = 0.1f;
        [SerializeField] private float timeToSpeedMin = 1f;
        [SerializeField] private float timeToSpeedMax = 3f;

        // Runtime state
        private bool gameRunning;
        private int currentLevel = 1;
        private int score;
        private int zombiesToSpawn;
        private int zombiesRemaining;
        private float currentSpawnDuration;
        private Camera playerCamera;

        private void Start()
        {
            playerCamera = Camera.main;
        }

        private void Awake()
        {
            Zombie.OnZombieDied += HandleZombieDeath;
            Zombie.OnZombieAttacked += GameOver;
        }

        
        public void StartGame()
        {
            gameOverText.transform.parent.gameObject.SetActive(false);
            scoreText.gameObject.SetActive(true);
            gameRunning = true;
            currentSpawnDuration = spawnDurationIncreasePerLevel;

            currentLevel = 1;
            score = 0;
            scoreText.text = "Score: 0";

            SetLighting(false);
            StartCoroutine(SpawnWave());
            gameAudio.Play();
        }

        private void SetLighting(bool isOn)
        {
            if (!isOn)
            {
                worldLighting.DarkenWorld();
                lightListActivator.Deactivate();
                return;
            }

            worldLighting.ReturnToStartingColor();
            lightListActivator.Activate();
        }

        IEnumerator SpawnWave()
        {
            yield return new WaitForSeconds(timeBetweenWaves);

            // Calculate how many zombies to spawn for this level
            zombiesToSpawn = spawnIncreasePerLevel + (currentLevel - 1) * 2;
            zombiesRemaining = zombiesToSpawn;

            // Spawn them one by one over currentSpawnDuration
            for (int i = 0; i < zombiesToSpawn; i++)
            {
                SpawnZombie();
                yield return new WaitForSeconds(currentSpawnDuration / zombiesToSpawn);
            }
        }

        private void SpawnZombie()
        {
            // Pick a random point along the line between spawnPoint1 and spawnPoint2
            float t = Random.Range(0f, 1f);
            Vector3 lineDirection = (spawnPoint2.position - spawnPoint1.position);
            Vector3 baseSpawnPos = spawnPoint1.position + lineDirection * t;

            // Apply a perpendicular offset
            Vector3 perpendicular = Vector3.Cross(lineDirection.normalized, Vector3.up).normalized;
            float offset = Random.Range(-spawnVariation, spawnVariation);
            Vector3 spawnPosition = baseSpawnPos + perpendicular * offset;

            // Instantiate the zombie
            GameObject zombieObj = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);

            // Immediately face the player
            FaceZombieTowardsPlayer(zombieObj);

            // Randomize its scale
            RandomizeZombieScale(zombieObj);

            // Get the script and initialize with random speed/time
            Zombie zombieScript = zombieObj.GetComponent<Zombie>();
            if (zombieScript)
            {
                // Calculate speed based on level
                float speedBasedOnLevelProgression = Mathf.Clamp01(currentLevel / 20f);
                float baseSpeed = Mathf.Lerp(minSpeed, maxSpeed, speedBasedOnLevelProgression);
                float randomizedSpeed = Mathf.Clamp(baseSpeed + Random.Range(-speedVariation, speedVariation), minSpeed, 1.0f);

                // Randomize time to accelerate to full speed
                float randomizedTimeToSpeed = Random.Range(timeToSpeedMin, timeToSpeedMax);

                // Initialize (no destination; they chase the player in the Zombie script)
                zombieScript.Initialize(randomizedSpeed, randomizedTimeToSpeed);
            }
        }

        private void FaceZombieTowardsPlayer(GameObject zombie)
        {
            if (!playerCamera) return;
            Vector3 directionToPlayer = (playerCamera.transform.position - zombie.transform.position);
            directionToPlayer.y = 0f; // Keep them level
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                zombie.transform.rotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            }
        }

        private void RandomizeZombieScale(GameObject zombie)
        {
            float randomScale = Random.Range(1f - scaleVariationAmount, 1f + scaleVariationAmount);
            zombie.transform.localScale *= randomScale;
        }

        private void HandleZombieDeath(Zombie zombie)
        {
            // Increase the score
            score += 10;
            scoreText.text = "Score: " + score.ToString();

            // Decrement how many are left in this wave
            zombiesRemaining--;

            // If this wave is complete, move to the next level
            if (zombiesRemaining <= 0)
            {
                currentLevel++;
                currentSpawnDuration += spawnDurationIncreasePerLevel;
                StartCoroutine(SpawnWave());
            }
        }

        private void GameOver()
        {
            if (!gameRunning) return;
            gameRunning = false;

            StopAllCoroutines();

            gameOverText.transform.parent.gameObject.SetActive(true);
            gameOverAudio.Play();
            gameAudio.Stop();

            SetLighting(true);
            DestroyAllZombies();
        }

        private void DestroyAllZombies()
        {
            // Clear all existing zombies from the scene
#if UNITY_2023_1_OR_NEWER
            Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);
#else
        Zombie[] zombies = FindObjectsOfType<Zombie>();
#endif
            foreach (Zombie z in zombies)
            {
                z.FadeAndDestroy();
            }
        }

        void OnDrawGizmosSelected()
        {
            // Draw the spawn line
            Gizmos.color = Color.green;
            if (spawnPoint1 && spawnPoint2)
            {
                Gizmos.DrawSphere(spawnPoint1.position, 0.2f);
                Gizmos.DrawSphere(spawnPoint2.position, 0.2f);
                Gizmos.DrawLine(spawnPoint1.position, spawnPoint2.position);
            }
        }
    }
}