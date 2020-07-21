using System;
using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;

public class SecondaryGrabFollowTest : MonoBehaviour
{
    public Transform currentHand;
    public Transform mainGripHand;

    public Quaternion fromToAtStart;

    private void Start()
    {
        SetStartingFromToRotation();
    }

    private void SetStartingFromToRotation()
    {
        Vector3 oldForward = mainGripHand.transform.forward;
        Quaternion oldRotation = mainGripHand.transform.rotation;

        mainGripHand.LookAt(currentHand);
        Vector3 newForward = mainGripHand.transform.forward;

        mainGripHand.rotation = oldRotation;
        fromToAtStart = Quaternion.FromToRotation(oldForward, newForward);
    }

    private void Update()
    {
        SetRotation();
    }


    //Credit to "VR with Andrew" on youTube for this method 
    private void SetRotation()
    {
        mainGripHand.transform.LookAt(currentHand.transform.position);
        mainGripHand.rotation = mainGripHand.rotation * Quaternion.Inverse(fromToAtStart);
    }
}