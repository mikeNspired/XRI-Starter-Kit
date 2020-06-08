using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioRandomize : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource = null;
    public List<AudioClip> audioClips;
    public float minPitch = -.1f, maxPitch = .1f;
    public float minVolume = -.1f, maxVolume = .1f;
    public bool randomize = true;

    private void Awake()
    {
        GetStartingValues();
        Randomize();
    }

    private void Randomize()
    {
        audioSource.pitch += Random.Range(minPitch, maxPitch);
        audioSource.volume += Random.Range(minVolume, maxVolume);

        if (audioClips.Count > 0)
        {
            var i = Random.Range(0, audioClips.Count);
            audioSource.clip = audioClips[i];
        }
    }

    private float originalPitch;
    private float originalVolume;

    void GetStartingValues()
    {
        audioSource = GetComponent<AudioSource>();
        originalPitch = audioSource.pitch;
        originalVolume = audioSource.volume;
    }


    public void PlaySound()
    {
        audioSource.volume = originalVolume;
        audioSource.pitch = originalPitch;

        if (randomize)
            Randomize();
        audioSource.Play();
    }
}