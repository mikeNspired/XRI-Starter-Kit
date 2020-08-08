using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;

public class RobotTest : MonoBehaviour
{
    [SerializeField] private RobotEnemyController controller;
    [SerializeField] private XRGrabInteractable interactable;
    [SerializeField] private Collider collider;
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Animator animator;

    public bool checkForCollision;
    public bool canActivate;
    private static readonly int  KAnimAwake =  Animator.StringToHash("Awake"); 
    private static readonly int KAnimActivate = Animator.StringToHash("Activate"); 


    // Start is called before the first frame update
    void Start()
    {
        OnValidate();
        interactable = GetComponent<XRGrabInteractable>();
        interactable.onActivate.AddListener(Activate);
        // interactable.onSelectEnter.AddListener(SetupRecoilVariables);
        interactable.onSelectExit.AddListener(CheckIfCanActivate);
    }

    private void CheckIfCanActivate(XRBaseInteractor arg0)
    {
        //Ensures collision is not detected while being held in hand
        if (canActivate)
            checkForCollision = true;
    }

    private void OnValidate()
    {
        if (!controller)
            controller = GetComponent<RobotEnemyController>();
        if (!interactable)
            interactable = GetComponent<XRGrabInteractable>();
        if (!collider)
            collider = GetComponent<Collider>();
        if (!rigidBody)
            rigidBody = GetComponent<Rigidbody>();
        if (!navMeshAgent)
            navMeshAgent = GetComponent<NavMeshAgent>();
        if (!animator)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (canActivate && checkForCollision)
        {
            SetupCharacter();
            
        }
    }

    private void SetupCharacter()
    {
        Destroy(collider);
        Destroy(interactable);
        Destroy(rigidBody);
        navMeshAgent.enabled = true;
        controller.enabled = true;
        animator.SetBool(KAnimActivate, true);
    }

    private void Activate(XRBaseInteractor interactor)
    {
        canActivate = !canActivate;
        if(canActivate)
            animator.SetBool(KAnimAwake, true);
        if (!canActivate)
            animator.SetBool(KAnimAwake, false);
    }
}