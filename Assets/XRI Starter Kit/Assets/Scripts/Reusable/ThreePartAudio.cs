using System.Collections;
using UnityEngine;
using static Unity.Mathematics.math;

namespace MikeNspired.XRIStarterKit
{
    public class ThreePartAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip beginning, end, loop;
        [SerializeField] private AudioSource audioSource;
        private float startingVolume;
        private bool isPlaying;

        private void Start() => startingVolume = audioSource.volume;

        public void Play()
        {
            if (isPlaying) return;
            StartCoroutine(PlayAudio());
        }

        public void Stop()
        {
            if (!isPlaying) return;
            StopAllCoroutines();
            audioSource.clip = end;
            audioSource.loop = false;
            audioSource.Play();
            isPlaying = false;
        }

        private IEnumerator PlayAudio()
        {
            audioSource.clip = beginning;
            audioSource.loop = false;
            audioSource.Play();

            isPlaying = true;

            var waitTime = beginning.length;
            while (waitTime >= Time.deltaTime)
            {
                waitTime -= Time.deltaTime;
                yield return null;
            }

            audioSource.clip = loop;
            audioSource.loop = true;
            audioSource.Play();
        }

        public void Play(float volume)
        {
            audioSource.volume = remap(0, 1, 0, startingVolume, Mathf.Abs(volume));
            Play();
        }
    }
}