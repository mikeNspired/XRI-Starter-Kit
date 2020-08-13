using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopParticlesystemAfterTime : MonoBehaviour
{
    [SerializeField] private new ParticleSystem particleSystem = null;

    [SerializeField] private float timeTillStop = 5;

    // Start is called before the first frame update
    void Start()
    {
        OnValidate();
        Invoke(nameof(StopParticles), timeTillStop);
    }

    // Update is called once per frame
    void StopParticles()
    {
        particleSystem.Stop();
    }

    private void OnValidate()
    {
        if (!particleSystem) particleSystem = GetComponent<ParticleSystem>();
    }
}