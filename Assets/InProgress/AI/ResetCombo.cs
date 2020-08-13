using MikeNspired.UnityXRHandPoser;
using UnityEngine;

public class ResetCombo : MonoBehaviour
{
    private WaveManager waveManager;

    // Start is called before the first frame update
    void Start()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (!waveManager)
            waveManager = FindObjectOfType<WaveManager>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.transform.GetComponent<SimpleCollisionDamage>())
            waveManager.comboTracker = 1;
    }
}