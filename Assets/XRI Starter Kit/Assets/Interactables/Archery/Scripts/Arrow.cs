using System.Collections;
using MikeNspired.XRIStarterKit;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public class Arrow : MonoBehaviour
    {
        [SerializeField] private XRGrabInteractable xrGrabInteractable;
        [SerializeField] private ArrowCollisionDamage arrowCollisionDamage;
        [SerializeField] private float speed = 1;
        [SerializeField] private Transform tip;

        [Header("Particles")] [SerializeField] private float glintActivateTime;
        [SerializeField] private ParticleSystem glint;

        [Header("Sound")] [SerializeField] private AudioRandomize hitClip, bounceClip;

        private bool inAir = false;
        private Rigidbody rb;
        private Collider[] colliders;

        protected void Awake()
        {
            OnValidate();
            colliders = GetComponentsInChildren<Collider>(true);
            xrGrabInteractable.selectExited.AddListener(x => rb.isKinematic = false);
        }

        private void OnValidate()
        {
            if (!xrGrabInteractable) xrGrabInteractable = GetComponent<XRGrabInteractable>();
            if (!arrowCollisionDamage) arrowCollisionDamage = GetComponent<ArrowCollisionDamage>();
            if (!rb) rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!inAir) return;

            var impact = collision.transform.GetComponentInParent<IImpactType>();
            if (impact != null && impact.GetImpactType() == ImpactType.Metal)
            {
                if (bounceClip)
                    bounceClip.Play();
                return;
            }

            transform.parent = collision.transform;

            if (collision.transform.TryGetComponent(out Rigidbody body))
                body.AddForce(rb.linearVelocity, ForceMode.Impulse);

            Stop();
        }

        private void Stop()
        {
            inAir = false;
            SetPhysics(false);
            xrGrabInteractable.enabled = true;
            glint.Stop();
            hitClip.Play();
        }

        public void Release(float pullPower, Collider[] colliders)
        {
            inAir = true;
            arrowCollisionDamage.AdjustDamage(pullPower);
            IgnoreColliders(colliders);
            SetPhysics(true);
            rb.AddForce(transform.forward * pullPower * speed, ForceMode.Impulse);
            StartCoroutine(RotateWithVelocity());
            Invoke(nameof(ActivateGlint), glintActivateTime);
        }

        private void ActivateGlint()
        {
            if (inAir)
                glint.Play();
        }

        private void SetPhysics(bool usePhysics)
        {
            rb.useGravity = usePhysics;
            rb.isKinematic = !usePhysics;
        }

        private void IgnoreColliders(Collider[] bowColliders)
        {
            foreach (var c in colliders)
            foreach (var c2 in bowColliders)
                Physics.IgnoreCollision(c, c2);
        }

        private IEnumerator RotateWithVelocity()
        {
            yield return new WaitForFixedUpdate();
            while (inAir)
            {
                Quaternion newRotation = Quaternion.LookRotation(rb.linearVelocity, transform.up);
                transform.rotation = newRotation;
                yield return null;
            }
        }
    }
}