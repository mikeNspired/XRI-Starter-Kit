using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    public class AnimateTransform : MonoBehaviour
    {
        [SerializeField] private Transform MovingObject = null;
        [SerializeField] private Transform endPosition = null;
        [SerializeField] private bool animatePosition = true;
        [SerializeField] private bool animateRotation = true;
        private TransformStruct startingTransform;
        private TransformStruct endingTransform;
        public bool startAtEndPoint;
        public float animateTowardsTime = .1f;
        public float animateReturnTime = .2f;
        public UnityEvent OnFinishedAnimatingTowards;
        public UnityEvent OnFinishedAnimatingReturn;

        private void Start()
        {
            startingTransform.position = MovingObject.localPosition;
            startingTransform.rotation = MovingObject.localRotation;
            endingTransform.position = endPosition.localPosition;
            endingTransform.rotation = endPosition.localRotation;
            if (startAtEndPoint)
            {
                SetToEndPosition();
            }
        }

        public void SetToStartPosition()
        {
            MovingObject.localPosition = startingTransform.position;
            MovingObject.localRotation = startingTransform.rotation;
        }

        public void SetToEndPosition()
        {
            MovingObject.localPosition = endingTransform.position;
            MovingObject.localRotation = endingTransform.rotation;
        }

        public void AnimateTo()
        {
            StopAllCoroutines();
            StartCoroutine(Animate(endingTransform, animateTowardsTime, OnFinishedAnimatingTowards));
        }

        public void AnimateReturn()
        {
            StopAllCoroutines();
            StartCoroutine(Animate(startingTransform, animateReturnTime, OnFinishedAnimatingReturn));
        }

        private IEnumerator Animate(TransformStruct endingPosition, float time, UnityEvent finishedEvent)
        {
            TransformStruct startingPosition;
            startingPosition.position = MovingObject.localPosition;
            startingPosition.rotation = MovingObject.localRotation;
            float timer = 0;
            while (timer <= time + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(startingPosition.position, endingPosition.position, timer / time);
                var newRotation = Quaternion.Lerp(startingPosition.rotation, endingPosition.rotation, timer / time);

                if (animatePosition)
                    MovingObject.localPosition = newPosition;
                if (animateRotation)
                    MovingObject.localRotation = newRotation;

                timer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }

            finishedEvent.Invoke();
        }
    }
}