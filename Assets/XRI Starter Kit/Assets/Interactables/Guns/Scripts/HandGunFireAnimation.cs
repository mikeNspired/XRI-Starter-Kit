using System.Collections;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class HandGunFireAnimation : MonoBehaviour
    {
        [SerializeField] private GunCocking gunCocking = null;
        [SerializeField] private float movePositionAnimationTime = 0.03f;

        [SerializeField] private Transform slider = null;
        [SerializeField] private Transform sliderGoalPosition = null;

        [SerializeField] private Transform hammer = null;
        [SerializeField] private Transform hammerOpen = null;
        private Vector3 hammerStartPosition;
        private Quaternion hammerStartRotation;

        private void Start()
        {
            hammerStartPosition = hammer.transform.localPosition;
            hammerStartRotation = hammer.transform.localRotation;
        }

        private IEnumerator MoveSlider(Transform mover, Transform goalPosition)
        {
            float timer = 0;

            SetKeyBangerClosed();
            while (timer <= movePositionAnimationTime)
            {
                var newPosition = Vector3.Lerp(gunCocking.GetStartPoint(), gunCocking.GetEndPoint(), timer / movePositionAnimationTime);

                mover.localPosition = newPosition;

                timer += Time.deltaTime;
                yield return null;
            }


            SetKeyBangerOpen();
            timer = 0;
            while (timer <= movePositionAnimationTime + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(gunCocking.GetEndPoint(), gunCocking.GetStartPoint(), timer / movePositionAnimationTime);

                mover.localPosition = newPosition;

                timer += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator OpenSlider(Transform mover, Transform goalPosition)
        {
            var startingPosition = mover.localPosition;
            float timer = 0;

            SetKeyBangerClosed();
            while (timer <= movePositionAnimationTime + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(startingPosition, gunCocking.GetEndPoint(), timer / movePositionAnimationTime);

                mover.localPosition = newPosition;

                timer += Time.deltaTime;
                yield return null;
            }
        }

        public void AnimateSliderOnFire() => StartCoroutine(MoveSlider(slider, sliderGoalPosition));

        public void SetSliderOpen()
        {
            gunCocking.Pause();
            StopAllCoroutines();
            StartCoroutine(OpenSlider(slider, sliderGoalPosition));
        }

        public void SetKeyBangerOpen()
        {
            hammer.transform.position = hammerOpen.transform.position;
            hammer.transform.rotation = hammerOpen.transform.rotation;
        }

        public void SetKeyBangerClosed()
        {
            hammer.transform.localPosition = hammerStartPosition;
            hammer.transform.localRotation = hammerStartRotation;
        }
    }
}