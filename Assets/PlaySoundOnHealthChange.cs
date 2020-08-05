using UnityEngine;

public class PlaySoundOnHealthChange : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private AudioRandomize audioRandomizer;


    void Start()
    {
        OnValidate();
        health.onDamaged += PlaySound;
    }

    private void PlaySound(float arg0, GameObject arg1)
    {
        audioRandomizer.PlaySound();
    }

    private void OnValidate()
    {
        if (!health)
            health = GetComponent<Health>();
        if (!audioRandomizer)
            audioRandomizer = GetComponent<AudioRandomize>();
    }
}