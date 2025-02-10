using UnityEngine;
using Random = UnityEngine.Random;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Will trigger SFX through the SFXPlayer when the object on which this is added trigger a collision enter event
    /// </summary>
    public class CollisionSound : MonoBehaviour
    {
        [SerializeField] private AudioClip[] Clips = null;
        [SerializeField] private AudioSource audioSource = null;
        [SerializeField] private AnimationCurve volumeCurve = null;
        [SerializeField] private bool randomizePitch = false, useVolumeCurve;
        [SerializeField] private float minPitchChange = -.1f, maxPitchChange = .1f;
        [SerializeField] private float maxVolume = 1;
        [SerializeField] private float timeTillCanPlayAgain = .1f;
        [SerializeField] private float maxVelocity = 5;
        private float originalPitch;
        private float timer;


        private void Awake()
        {
            OnValidate();
            CheckValid();

            originalPitch = audioSource.pitch;
        }

        private void OnValidate()
        {
            if (!audioSource)
                audioSource = GetComponent<AudioSource>();
        }

        private void OnCollisionEnter(Collision other)
        {
            //avoid playing hit sound when all physic object settle at the load of the level.
            if (Time.timeSinceLevelLoad < 1.0f)
                return;

            //Check if time has elapsed to play sound again
            if (Time.time - timer < timeTillCanPlayAgain) return;
            timer = Time.time;
            
            if (randomizePitch)
            {
                audioSource.pitch = originalPitch;
                audioSource.pitch += Random.Range(minPitchChange, maxPitchChange);
            }

            //Remap velocity to 0 to 1 for volume
            var volume = Remap(Mathf.Clamp(other.relativeVelocity.magnitude, 0, maxVelocity), 0, maxVelocity, 0, 1);
            
            volume = useVolumeCurve ? 
                Remap(volumeCurve.Evaluate(volume), 0, 1, 0, maxVolume) :
                Remap(volume, 0, 1, 0, maxVolume);

            AudioClip randomClip = Clips[Random.Range(0, Clips.Length)];
            audioSource.clip = randomClip;
            audioSource.volume = volume;
            
            audioSource.Play();
        }

        private float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        private void CheckValid()
        {
            if (audioSource != null) return;
            Debug.LogWarning("Collision sound does not have audio source on : " + gameObject);
            enabled = false;
        }
    }
}