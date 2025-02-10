using System.Collections;
using UnityEngine;
using static Unity.Mathematics.math;

namespace MikeNspired.XRIStarterKit
{
    public class VehicleAudio : MonoBehaviour
    {
        [SerializeField] private float idleVolume, idlePitch, maxPitch, maxVolume, maxVelocity, timeTillOff = .25f;
        [SerializeField] private AudioSource engineAudioSource, backupAudioSource;
        private float currentVelocity, currentVolume, currentPitch;

        public void TurnOn()
        {
            StopAllCoroutines();
            engineAudioSource.volume = idleVolume;
            engineAudioSource.pitch = idlePitch;
            engineAudioSource.Play();
        }

        public void TurnOff()
        {
            backupAudioSource.Stop();
            StartCoroutine(nameof(TurnOffEngine));
        }

        private IEnumerator TurnOffEngine()
        {
            var time = 0f;
            var startingVolume = engineAudioSource.volume;
            while (time <= timeTillOff + Time.deltaTime)
            {
                time += Time.deltaTime;
                engineAudioSource.volume = Mathf.Lerp(startingVolume, 0f, time / timeTillOff);
                yield return null;
            }

            engineAudioSource.Stop();
        }

        public void AdjustAudio(float velocity)
        {
            if (!engineAudioSource.isPlaying) return;

            currentVelocity = velocity;
            var mainInput = remap(0, maxVelocity, 0, 1, velocity);

            if (Mathf.Abs(mainInput) < 0.1f)
            {
                engineAudioSource.volume = idleVolume;
                engineAudioSource.pitch = idlePitch;
            }
            else
            {
                engineAudioSource.volume = Mathf.Lerp(idleVolume, maxVolume, Mathf.Abs(mainInput));
                engineAudioSource.pitch = Mathf.Lerp(idlePitch, maxPitch, Mathf.Abs(mainInput));
            }
        }

        public void AdjustAudio(float movementInput, float velocityMagnitude)
        {
            if (!engineAudioSource.isPlaying) return;

            currentVelocity = velocityMagnitude;
            var mainInput = remap(0, maxVelocity, 0, 1, velocityMagnitude);

            if (Mathf.Abs(movementInput) < 0.1f)
            {
                engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, idleVolume, Time.deltaTime);
                engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, idlePitch, Time.deltaTime);
            }
            else
            {
                engineAudioSource.volume = Mathf.Lerp(idleVolume, maxVolume, Mathf.Abs(mainInput));
                engineAudioSource.pitch = Mathf.Lerp(idlePitch, maxPitch, Mathf.Abs(mainInput));
            }
        }

        public void PlayReverseSound(bool state)
        {
            if (!backupAudioSource) return;
            if (state)
                backupAudioSource.Play();
            else
                backupAudioSource.Stop();
        }
    }
}