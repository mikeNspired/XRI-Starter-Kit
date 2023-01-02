using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AudioRandomize : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource = null;
    public List<AudioClip> audioClips;
    public float minPitch = -.1f, maxPitch = .1f;
    public float minVolume = -.1f, maxVolume = .1f;
    public bool randomize = true, playOnAwake = false, playOnlyIfClipFinished = false, playAsOneShot = false, destroyAfterPlaying;
    public AudioClip CurrentClipPlayed => currentClipPlayed;
    private AudioClip currentClipPlayed;

    private void Awake()
    {
        OnValidate();
        GetStartingValues();
        Randomize();
        if (playOnAwake)
            PlaySound();
    }

    private void OnValidate()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    private void Randomize()
    {
        audioSource.pitch += Random.Range(minPitch, maxPitch);
        audioSource.volume += Random.Range(minVolume, maxVolume);

        if (audioClips.Count > 0)
        {
            var i = Random.Range(0, audioClips.Count);
            currentClipPlayed = audioClips[i];
            audioSource.clip = currentClipPlayed;
        }
    }

    private float originalPitch;
    private float originalVolume;

    void GetStartingValues()
    {
        originalPitch = audioSource.pitch;
        originalVolume = audioSource.volume;
    }

    public void PlaySound()
    {
        if (!audioSource.enabled) return;
        
        audioSource.volume = originalVolume;
        audioSource.pitch = originalPitch;

        if (playOnlyIfClipFinished && audioSource.isPlaying)
            return;

        if (randomize)
            Randomize();

        if (playAsOneShot)
            audioSource.PlayOneShot(currentClipPlayed);

        audioSource.Play();
        if (destroyAfterPlaying)
            Destroy(gameObject, currentClipPlayed.length);
    }
}