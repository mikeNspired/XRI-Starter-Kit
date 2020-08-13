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
    [SerializeField] private Animator animator;
    [SerializeField] private float moveToStartAnimationLength;

    [Tooltip("Start position of animation to align object before switching")] [SerializeField]
    private float startPositionY = 1.45f;
    //[SerializeField] private Vector3 scale;

    public bool checkForCollision;
    public bool canActivate;
    private static readonly int KAnimAwake = Animator.StringToHash("Awake");


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
        if (!animator)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (canActivate && checkForCollision)
        {
            canActivate = false;
            checkForCollision = false;
            SetupCharacter();
        }
    }

    private void SetupCharacter()
    {
        Destroy(collider);
        Destroy(interactable);
        Destroy(rigidBody);

        StartCoroutine(MoveToStartPosition());
    }

    private IEnumerator MoveToStartPosition()
    {
        float currentTimer = 0;
        Vector3 startRotation = transform.eulerAngles;
        Vector3 startPosition = transform.position;
        Vector3 goalRotation = new Vector3(0, startRotation.y, 0);
        Vector3 goalPosition = new Vector3(startPosition.x, startPosition.y + startPositionY, startPosition.z);

        while (currentTimer <= moveToStartAnimationLength + Time.deltaTime)
        {
            transform.position = Vector3.Slerp(startPosition, goalPosition, currentTimer / moveToStartAnimationLength);
            //transform.localScale = Vector3.Slerp(transform.localScale, scale, currentTimer / moveToStartAnimationLength);
            transform.rotation = Quaternion.Slerp(Quaternion.Euler(startRotation),
                Quaternion.Euler(goalRotation), currentTimer / moveToStartAnimationLength);

            yield return new WaitForSeconds(Time.deltaTime);
            currentTimer += Time.deltaTime;
        }

        controller.gameObject.SetActive(true);
        controller.transform.parent = null;
        Physics.Raycast(controller.transform.position, Vector3.down, out RaycastHit hit, 5);
        var distance = Vector3.Distance(controller.transform.position, hit.point);
        controller.gameObject.transform.position -= Vector3.up * distance;
        Destroy(gameObject);
    }

    private void Activate(XRBaseInteractor interactor)
    {
        canActivate = true;
            animator.SetBool(KAnimAwake, true);
        // if (canActivate)
        // if (!canActivate)
        //     animator.SetBool(KAnimAwake, false);
    }
}