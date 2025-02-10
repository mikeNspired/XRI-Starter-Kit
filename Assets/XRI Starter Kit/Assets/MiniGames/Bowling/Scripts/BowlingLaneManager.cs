using System.Collections;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class BowlingLaneManager : MonoBehaviour
    {
        [SerializeField] private GameObject startingPins = null, pinSpawningLocation = null, newPins = null;
        [SerializeField] private Animator pinRemover = null;
        [SerializeField] private ScoreCard scoreCard = null;
        [SerializeField] private Transform spawnRespawnPoint = null;
        private Vector3 startingPinsLocation;
        private int ballCounter, pinCounter, pinScoreCounter;
        private bool paused;
        private float animationTime = 1, ballSpawnSpeed = 3f;

        private void Start()
        {
            startingPinsLocation = startingPins.transform.localPosition;

            RegisterToPins(startingPins.transform);
        }

        private void RegisterToPins(Transform pinParent)
        {
            var pins = pinParent.GetComponentsInChildren<Pin>();
            foreach (var pin in pins)
            {
                pin.pinKnockedOver.AddListener(PinHit);
                pin.isActive = true;
            }
        }

        public void Reset()
        {
            pinScoreCounter = 0;
            ballCounter = 0;
            pinCounter = 0;
            scoreCard.Reset();
            StopAllCoroutines();
            ResetPins();
        }

        private void PinHit()
        {
            if (paused) return;

            pinCounter++;
            pinScoreCounter++;
            // if (pinCounter >= 10) //Remove this. Let the scorecard handle it when it detects a ball 
            // {
            //     scoreCard.PinsHit(pinScoreCounter);
            //     pinScoreCounter = 0;
            //     ResetPins();
            // }
        }

        private GameObject currentCollider;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (!other.attachedRigidbody.GetComponent<BowlingBall>()) return;
            if (other.attachedRigidbody.gameObject == currentCollider) return;

            currentCollider = other.attachedRigidbody.gameObject;
            StartCoroutine(RespawnBall(currentCollider));

            if (paused) return;

            scoreCard.PinsHit(pinScoreCounter);
            pinScoreCounter = 0;

            ballCounter++;
            if (ballCounter >= 2)
                ResetPins();
        }

        private IEnumerator RespawnBall(GameObject ball)
        {
            yield return new WaitForSeconds(1);
            ball.transform.position = spawnRespawnPoint.transform.position;
            ball.transform.rotation = spawnRespawnPoint.transform.rotation;
            ball.GetComponent<Rigidbody>().linearVelocity = ball.transform.forward * ballSpawnSpeed;
        }

        private void ResetPins()
        {
            pinRemover.SetTrigger("Activate");
            StopCoroutine(AnimatePinResetter());
            StartCoroutine(AnimatePinResetter());
        }

        private float currentTimer = 0;

        private GameObject pins;

        private IEnumerator AnimatePinResetter()
        {
            paused = true;
            yield return new WaitForSeconds(4);

            DestroyImmediate(pins);
            pins = Instantiate(newPins);
            pins.transform.parent = transform.parent;
            pins.transform.localPosition = startingPinsLocation;
            DisableRigidBody(pins.transform, true);

            currentTimer = 0;

            while (currentTimer <= animationTime)
            {
                pins.transform.localPosition = Vector3.Lerp(pinSpawningLocation.transform.localPosition, startingPinsLocation, currentTimer / animationTime);
                currentTimer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }

            DisableRigidBody(pins.transform, false);
            yield return new WaitForSeconds(1);

            RegisterToPins(pins.transform);


            ballCounter = 0;
            pinCounter = 0;
            paused = false;
        }


        private void DisableRigidBody(Transform pinsParent, bool state)
        {
            var rbs = pinsParent.GetComponentsInChildren<Rigidbody>();
            foreach (var r in rbs)
                r.isKinematic = state;
        }
    }
}