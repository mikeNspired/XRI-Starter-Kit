using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class Target : MonoBehaviour, IDamageable
    {
        public UnityEventFloat onHit;
        public bool canActivate;
        public AnimateTransform animator;
        public AnimateBounce bounceAnimation;
        public AudioSource hitSoundEffect;
        public bool canTakeDamage;

        private void Start()
        {
            animator.OnFinishedAnimatingTowards.AddListener(() => canActivate = true);
        }

        public void TakeDamage(float damage, GameObject damager)
        {
            if (!canTakeDamage) return;
            canTakeDamage = false;
            canActivate = false;
            hitSoundEffect.Play();
            onHit.Invoke(damage);
            animator.AnimateTo();
            bounceAnimation.Stop();
        }

        public void Activate()
        {
            canTakeDamage = true;
            canActivate = false;
            animator.AnimateReturn();
        }

        public void StartSideToSideAnimation()
        {
            bounceAnimation.StartAnimation();
        }

        public void SetToDeactivatedInstant()
        {
            canTakeDamage = false;
            animator.SetToEndPosition();
            bounceAnimation.Stop();

        }

        public void SetToDeactivatedPosition()
        {
            canTakeDamage = false;
            animator.AnimateTo();
            bounceAnimation.Stop();

        }

        public void SetToActivatedPosition()
        {
            animator.AnimateReturn();

        }

    }
}