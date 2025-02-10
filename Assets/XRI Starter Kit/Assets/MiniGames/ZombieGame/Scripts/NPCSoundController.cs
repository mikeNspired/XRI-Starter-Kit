using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MikeNspired.XRIStarterKit
{
    public class NPCSoundController : MonoBehaviour
    {
        [Header("Audio Randomizers")] [SerializeField]
        private AudioRandomize footstepAudio;

        [SerializeField] private AudioRandomize vocalAudio;
        [SerializeField] private AudioRandomize painAudio;
        [SerializeField] private AudioRandomize impactAudio;
        [SerializeField] private AudioRandomize spawnAudio;
        [SerializeField] private AudioRandomize screamAudio;
        [SerializeField] private AudioRandomize deathAudio;

        [Header("Random Vocal Settings")]
        [Tooltip("If true, the NPC will randomly play a vocal sound in the background.")]
        [SerializeField]
        private bool enableRandomVocal = false;

        [Tooltip("Minimum time before the next random vocal.")] [SerializeField]
        private float minTimeBetweenVocal = 5f;

        [Tooltip("Maximum time before the next random vocal.")] [SerializeField]
        private float maxTimeBetweenVocal = 10f;

        private void Start()
        {
            // If random vocal is enabled, start the background routine
            if (enableRandomVocal)
            {
                StartCoroutine(RandomVocalRoutine());
            }
        }

        public void PlaySpawn()
        {
            if (spawnAudio != null)
                spawnAudio.Play();
        }


        public void PlayFootstep()
        {
            if (footstepAudio != null)
                footstepAudio.Play();
        }


        public void PlayScream()
        {
            if (footstepAudio != null)
                screamAudio.Play();
        }

        public void PlayVocal()
        {
            if (vocalAudio != null)
                vocalAudio.Play();
        }

        public void PlayPain()
        {
            if (painAudio != null)
                painAudio.Play();
        }

        public void PlayDeath()
        {
            if (deathAudio != null)
                deathAudio.Play();
        }


        public void PlayImpact()
        {
            if (impactAudio != null)
                impactAudio.Play();
        }

        /// <summary>
        /// Continuously waits a random time between [minTimeBetweenVocal, maxTimeBetweenVocal],
        /// then plays a vocal sound.
        /// </summary>
        private IEnumerator RandomVocalRoutine()
        {
            while (enableRandomVocal)
            {
                float waitTime = Random.Range(minTimeBetweenVocal, maxTimeBetweenVocal);
                yield return new WaitForSeconds(waitTime);

                if (vocalAudio != null)
                {
                    vocalAudio.Play();
                }
            }
        }

        /// <summary>
        /// Enable or disable random background vocal at runtime.
        /// </summary>
        /// <param name="enable">True to enable random vocal, false to disable.</param>
        public void SetRandomVocalEnabled(bool enable)
        {
            enableRandomVocal = enable;
            if (enable)
            {
                StartCoroutine(RandomVocalRoutine());
            }
            else
            {
                vocalAudio.Stop();
            }
        }
    }
}