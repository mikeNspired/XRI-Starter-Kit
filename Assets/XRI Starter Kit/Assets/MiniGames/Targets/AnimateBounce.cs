using System.Collections;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class AnimateBounce : MonoBehaviour
    {
        [SerializeField] private Transform MovingObject = null;
        [SerializeField] private Transform firstPosition = null;
        [SerializeField] private Transform secondPosition = null;
        private TransformStruct endingTransform;
        public bool animatePosition = true;
        public bool animateRotation = true;
        public float animateTime = .1f;

        public void Stop()
        {
            StopAllCoroutines();
        }


        public void StartAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(Animate());
        }


        private IEnumerator Animate()
        {
            TransformStruct startingPosition;
            startingPosition.position = MovingObject.localPosition;
            startingPosition.rotation = MovingObject.localRotation;
            float timer = 0;
            while (timer <= animateTime)
            {
                var newPosition = Vector3.Lerp(startingPosition.position, firstPosition.localPosition, timer / animateTime);
                var newRotation = Quaternion.Lerp(startingPosition.rotation, firstPosition.localRotation, timer / animateTime);

                if (animatePosition)
                    MovingObject.localPosition = newPosition;
                if (animateRotation)
                    MovingObject.localRotation = newRotation;

                timer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }

            startingPosition.position = MovingObject.localPosition;
            startingPosition.rotation = MovingObject.localRotation;
            timer = 0;
            while (timer <= animateTime)
            {
                var newPosition = Vector3.Lerp(startingPosition.position, secondPosition.localPosition, timer / animateTime);
                var newRotation = Quaternion.Lerp(startingPosition.rotation, secondPosition.localRotation, timer / animateTime);

                if (animatePosition)
                    MovingObject.localPosition = newPosition;
                if (animateRotation)
                    MovingObject.localRotation = newRotation;

                timer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }

            StartCoroutine(Animate());
        }
    }
}