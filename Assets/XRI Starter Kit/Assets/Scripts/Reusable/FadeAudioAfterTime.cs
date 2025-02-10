using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class FadeAudioAfterTime : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        [SerializeField] private float timeTillStop = 5;
        [SerializeField] private float fadeLength = 2;

        private float currentFadeTimer, currentTimer, startingVolume;

        private bool fadeStarted;

        private void Start() => OnValidate();


        private void OnValidate()
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            currentTimer += Time.deltaTime;

            if (currentTimer >= timeTillStop)
            {
                if (!fadeStarted)
                {
                    startingVolume = audioSource.volume;
                    fadeStarted = true;
                }

                currentFadeTimer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startingVolume, 0, currentFadeTimer / fadeLength);
            }
        }
    }
}