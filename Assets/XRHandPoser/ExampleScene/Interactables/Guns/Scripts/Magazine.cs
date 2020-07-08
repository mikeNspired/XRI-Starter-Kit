using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Magazine : MonoBehaviour
{

    public int AmmoCount = 10;
    public GunType gunType;
    public bool isBeingGrabbed;

    private void Start()
    {
        GetComponent<XRGrabInteractable>().onSelectEnter.AddListener(x => isBeingGrabbed = true);
        GetComponent<XRGrabInteractable>().onSelectExit.AddListener(x => isBeingGrabbed = false);
    }

    public bool UseAmmo()
    {
        if (AmmoCount <= 0) return false;
        
        AmmoCount--;
        return true;
    }
}

//Ammo attachpoint
//Ammo magazine contains ammo count
//Ammo get in range - Ammo auto attaches = Plays sound
//Ammo has collider to grab


//Magazine pick up
//Turn on small collider on attachment point
//Dot product to see if they are alligning the ammo properly

//Magazine hits attachpoint collider
//Magazine unAttaches from hand
//Magazine disables all colliders
//Magazine lerps towards insertion point
//Magazine Animates inside
//Magazine updates gun ammo?
//Turn on magazine collider to grab and remove


