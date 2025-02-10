using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class BottleBreakGame : MonoBehaviour
    {
        public Transform targetParent;
        public List<BottleTargetSpawner> targets;
        public Transform levelZero;
        public List<Transform> targetPositionsLevelZero;
        public Transform levelOne;
        public List<Transform> targetPositionsLevelOne;
        public Transform levelTwo;
        public List<Transform> targetPositionsLevelTwo;
        private Light[] spotLights;

        public CanvasGroup headsUpDisplay;
        public float timer = 60;


        public bool isGameActive = false;
        private float movePositionAnimationTime = 1;

        public FloatSO gameTimer;
        public FloatSO totalTargetsHit;

        private void Start()
        {
            targetParent.GetComponentsInChildren<BottleTargetSpawner>(targets);
            foreach (var target in targets) target.OnBottleBroke.AddListener(TargetHit);

            spotLights = GetComponentsInChildren<Light>();
            foreach (var light in spotLights) light.enabled = false;

            headsUpDisplay.alpha = 0;

            //Remove parent holder from lists
            levelZero.GetComponentsInChildren<Transform>(targetPositionsLevelZero);
            targetPositionsLevelZero.Remove(targetPositionsLevelZero[0]);

            levelOne.GetComponentsInChildren<Transform>(targetPositionsLevelOne);
            targetPositionsLevelOne.Remove(targetPositionsLevelOne[0]);

            levelTwo.GetComponentsInChildren<Transform>(targetPositionsLevelTwo);
            targetPositionsLevelTwo.Remove(targetPositionsLevelTwo[0]);
        }

        //Called from slider unity event
        public void ChangeGame(int x)
        {
            //End currentGame
            if (isGameActive)
            {
                isGameActive = false;
                foreach (var light in spotLights) light.enabled = false;
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
            foreach (var light in spotLights) light.enabled = true;
            //TODO add sound effect for lights turning on/off
        }

        private void Update()
        {
            if (!isGameActive) return;

            timer -= Time.deltaTime;
            gameTimer.SetValue(timer);

            if (timer <= 0)
            {
                isGameActive = false;
                gameTimer.SetValue(0);
                StopAllCoroutines();
                StartCoroutine(CheckGameOver());
                foreach (var light in spotLights) light.enabled = false;
            }
        }

        private void TargetHit()
        {
            if (!isGameActive) return;
            float totalTargets = totalTargetsHit.GetValue() + 1;
            totalTargetsHit.SetValue(totalTargets);
        }


        private IEnumerator CheckGameOver()
        {
            yield return new WaitForSeconds(5);
            headsUpDisplay.alpha = 0;
        }
    }
}