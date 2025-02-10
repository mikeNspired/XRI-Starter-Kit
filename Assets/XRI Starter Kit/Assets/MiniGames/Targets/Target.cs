using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace MikeNspired.XRIStarterKit
{
    public class Target : MonoBehaviour
    {
        public UnityEventFloat onHit;
        public bool canActivate;
        public AnimateTransform animator;
        public AnimateBounce bounceAnimation;

        [FormerlySerializedAs("canTakeDamage")]
        public bool isActive;

        [SerializeField] private TargetPoints[] targetPoints;
        [SerializeField] private Animator textAnimator;

        private void Start()
        {
            animator.OnFinishedAnimatingTowards.AddListener(() => canActivate = true);
            foreach (var target in targetPoints) target.onHit.AddListener(TargetHit);
            textAnimator.gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            targetPoints = GetComponentsInChildren<TargetPoints>();
        }

        public void TestHit()
        {
            TargetHit(1);
        }

        private void TargetHit(float damage)
        {
            if (!isActive) return;
            isActive = false;
            SetTargetPointsState(false);
            canActivate = false;
            onHit.Invoke(damage);
            animator.AnimateTo();
            bounceAnimation.Stop();
            SetDamageText(damage);
        }

        public void Activate()
        {
            SetTargetPointsState(true);
            isActive = true;
            canActivate = false;
            animator.AnimateReturn();
        }

        public void StartSideToSideAnimation()
        {
            bounceAnimation.StartAnimation();
        }

        public void SetToDeactivatedInstant()
        {
            SetTargetPointsState(false);
            isActive = false;
            animator.SetToEndPosition();
            bounceAnimation.Stop();
        }

        public void SetToDeactivatedPosition()
        {
            SetTargetPointsState(false);
            isActive = false;
            animator.AnimateTo();
            bounceAnimation.Stop();
        }

        private void SetTargetPointsState(bool state)
        {
            foreach (var target in targetPoints) target.canTakeDamage = state;
        }

        public void SetToActivatedPosition()
        {
            animator.AnimateReturn();
        }

        private void SetDamageText(float damage)
        {
            textAnimator.gameObject.SetActive(false);
            textAnimator.gameObject.SetActive(true);
            textAnimator.GetComponent<TextMeshPro>().text = damage.ToString(CultureInfo.InvariantCulture);
        }
    }
}