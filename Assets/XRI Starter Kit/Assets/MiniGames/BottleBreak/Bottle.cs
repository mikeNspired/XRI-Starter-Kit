using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace MikeNspired.XRIStarterKit
{
    public class Bottle : MonoBehaviour, IDamageable
    {
        public ParticleSystem particleSystemSplash;
        public GameObject SmashedObject;
        public GameObject Liquid;
        public GameObject Mesh;
        public float glassExplodeForce = 500;
        public float explodeUpwardModifier = 1.5f;
        AudioSource m_AudioSource;
        public UnityEventFloat onHit;


        void OnEnable()
        {
            if (particleSystemSplash)
                particleSystemSplash.Stop();
        }

        public void TakeDamage(float damage, GameObject damager)
        {
            onHit.Invoke(damage);
            GetComponent<AudioRandomize>().Play();
            particleSystemSplash.transform.parent = null;
            particleSystemSplash.gameObject.SetActive(true);
            particleSystemSplash.Play();
            SmashedObject.SetActive(true);
            Liquid.SetActive(false);
            Mesh.SetActive(false);

            Rigidbody[] rbs = SmashedObject.GetComponentsInChildren<Rigidbody>();
            Transform camera = Camera.main.transform;
            var position = transform.position - camera.position;
            foreach (Rigidbody rb in rbs)
            {
                rb.AddExplosionForce(glassExplodeForce, SmashedObject.transform.position - position.normalized * .25f, 2.0f, explodeUpwardModifier);
            }

            SmashedObject.transform.parent = null;
            ;

            Destroy(gameObject, 3);
        }
    }
}