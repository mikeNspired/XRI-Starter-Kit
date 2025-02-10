using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MikeNspired.XRIStarterKit
{
    public class TargetManager : MonoBehaviour
    {
        public Transform targetParent;
        public List<Target> targets;
        public Transform levelZero;
        public List<Transform> targetPositionsLevelZero;
        public Transform levelOne;
        public List<Transform> targetPositionsLevelOne;
        public Transform levelTwo;
        public List<Transform> targetPositionsLevelTwo;


        public CanvasGroup headsUpDisplay;
        public float timer = 60;

        public bool isGameActive = false;
        public int difficulty = 0;
        private float movePositionAnimationTime = 1;

        public FloatSO gameTimer;
        public FloatSO totalTargetsHit;

        private void Start()
        {
            targetParent.GetComponentsInChildren<Target>(targets);

            levelZero.GetComponentsInChildren<Transform>(targetPositionsLevelZero);
            targetPositionsLevelZero.Remove(targetPositionsLevelZero[0]);

            levelOne.GetComponentsInChildren<Transform>(targetPositionsLevelOne);
            targetPositionsLevelOne.Remove(targetPositionsLevelOne[0]);

            levelTwo.GetComponentsInChildren<Transform>(targetPositionsLevelTwo);
            targetPositionsLevelTwo.Remove(targetPositionsLevelTwo[0]);

            foreach (var target in targets)
            {
                target.onHit.AddListener(TargetHit);
            }

            headsUpDisplay.alpha = 0;
        }

        public void ChangeGame(int x)
        {
            difficulty = x;

            //End currentGame
            if (isGameActive)
            {
                foreach (var target in targets)
                {
                    target.canActivate = false;
                    target.SetToDeactivatedPosition();
                }

                isGameActive = false;
            }

            StopAllCoroutines();
            switch (x)
            {
                case 0:
                    MoveToPositions(targetPositionsLevelZero);
                    break;
                case 1:
                    MoveToPositions(targetPositionsLevelOne);
                    break;
                default:
                    MoveToPositions(targetPositionsLevelTwo);
                    break;
            }
        }

        private void MoveToPositions(List<Transform> list)
        {
            GetComponent<AudioSource>().Play();
            for (var i = 0; i < list.Count; i++)
            {
                StartCoroutine(MoveToPosition(targets[i].transform, list[i]));
            }
        }

        private IEnumerator MoveToPosition(Transform mover, Transform goalPosition)
        {
            var startingPosition = mover.position;
            float timer = 0;
            while (timer <= movePositionAnimationTime)
            {
                var newPosition = Vector3.Lerp(startingPosition, goalPosition.position, timer / movePositionAnimationTime);

                mover.position = newPosition;

                timer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }
        }

        public void StartGame()
        {
            StopAllCoroutines();
            headsUpDisplay.alpha = 1;
            timer = 60;
            totalTargetsHit.SetValue(0);
            gameTimer.SetValue(timer);
            isGameActive = true;

            foreach (var target in targets)
            {
                target.canActivate = true;
                target.SetToDeactivatedInstant();
            }

            StartCoroutine(ActivateAnotherTarget());
            StartCoroutine(ActivateAnotherTarget());
        }

        private Coroutine activateTarget;

        private void Update()
        {
            if (!isGameActive) return;

            timer -= Time.deltaTime;
            gameTimer.SetValue(timer);

            if (timer <= 0)
            {
                gameTimer.SetValue(0);
                StopAllCoroutines();
                StartCoroutine(CheckGameOver());
            }
        }

        private void TargetHit(float damage)
        {
            totalTargetsHit.SetValue(totalTargetsHit.GetValue() + damage);
            StartCoroutine(ActivateAnotherTarget());
        }

        private IEnumerator ActivateAnotherTarget()
        {
            int random = Random.Range(0, targets.Count);
            while (!targets[random].canActivate)
            {
                random = Random.Range(0, targets.Count);
                yield return null;
            }

            targets[random].Activate();

            if (difficulty == 2)
                targets[random].StartSideToSideAnimation();
        }

        private IEnumerator CheckGameOver()
        {
            isGameActive = false;

            foreach (var target in targets)
            {
                target.canActivate = false;
                target.SetToDeactivatedPosition();
            }

            yield return new WaitForSeconds(5);
            headsUpDisplay.alpha = 0;

        }
    }
}
