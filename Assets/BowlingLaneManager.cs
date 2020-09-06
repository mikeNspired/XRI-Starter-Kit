using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BowlingLaneManager : MonoBehaviour
{
    [SerializeField] private int ballCounter;
    [SerializeField] private int pinCounter;
    [SerializeField] private GameObject startingPins;
    [SerializeField] private GameObject pinSpawningLocation;
    [SerializeField] private Vector3 startingPinsLocation;
    [SerializeField] private GameObject newPins;
    [SerializeField] private Animator pinRemover;
    [SerializeField] private ScoreCard scoreCard;
    [SerializeField] private Transform spawnRespawnPoint;
    private bool paused;

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

    int pinScoreCounter = 0;

    private void PinHit()
    {
        if (paused) return;

        pinCounter++;
        pinScoreCounter++;
        if (pinCounter >= 10)
        {
            scoreCard.PinsHit(pinScoreCounter);
            pinScoreCounter = 0;
            ResetPins();
        }
    }

    private GameObject currentCollider;

    private void OnTriggerEnter(Collider other)
    {
        if (paused) return;
        if (!other.attachedRigidbody) return;
        if (other.attachedRigidbody.gameObject == currentCollider) return;

        var name = other.attachedRigidbody.name.ToLower();
        if (!name.Contains("ball")) return;

        currentCollider = other.attachedRigidbody.gameObject;

        Invoke(nameof(RespawnBall),1);
        
        scoreCard.PinsHit(pinScoreCounter);
        pinScoreCounter = 0;
        
        ballCounter++;
        if (ballCounter >= 2)
            ResetPins();
    }
    private void RespawnBall()
    {
        currentCollider.transform.position = spawnRespawnPoint.transform.position;
        currentCollider.transform.rotation = spawnRespawnPoint.transform.rotation;
        currentCollider.GetComponent<Rigidbody>().velocity = currentCollider.transform.forward;

    }
    private void ResetPins()
    {
        pinRemover.SetTrigger("Activate");
        StartCoroutine(move());
    }

    private float animationTime = 1;
    private float currentTimer = 0;

    private IEnumerator move()
    {
        paused = true;
        yield return new WaitForSeconds(4);

        var pins = Instantiate(newPins);
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
        {
            r.isKinematic = state;
        }
    }
}