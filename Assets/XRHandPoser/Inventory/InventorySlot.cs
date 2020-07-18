using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] [Tooltip("Optional Starting item")]
    private XRBaseInteractable startingItem = null;

    [SerializeField] [Tooltip("Display used when holding slot is holding an item")]
    private GameObject slotDisplayWhenContainsItem = null;

    [SerializeField] [Tooltip("Display used when slot is empty and can add an item")]
    private GameObject slotDisplayToAddItem = null;

    [SerializeField] [Tooltip("Transform to hold the viewing model of the current Inventory Slot Item.")]
    private Transform itemModelHolder = null;

    [SerializeField] [Tooltip("Transform of back image that rotates during animations, used to attach ItemModelHolder to after positioning model")]
    private Transform backImagesThatRotate = null;

    [SerializeField] [Tooltip("Item will be scaled down to size to fit inside this box collider")]
    private BoxCollider inventorySize = null;

    [SerializeField] private new Collider collider = null;
    [SerializeField] private AudioSource grabAudio = null, releaseAudio = null;


    public UnityEvent inventorySlotUpdated;

    private XRBaseInteractable currentSlotItem;
    public XRBaseInteractable CurrentSlotItem => currentSlotItem;

    private int Disable;
    private int Enable;

    private Transform boundCenterTransform, itemSlotMeshClone;
    private XRInteractionManager interactionManager;
    private PlayerInventory inventory;

    //Animation
    private bool isBusy, isDisabling;
    private Animator addItemAnimator, hasItemAnimator;
    private TransformStruct startingTransformFromHand;
    private Vector3 goalSizeToFitInSlot;
    private const float AnimationDisableLength = .5f, AnimationLengthItemToSlot = .15f;


    private void Awake()
    {
        OnValidate();

        //Create currentSlotItem from prefab
        if (!currentSlotItem && startingItem)
        {
            currentSlotItem = Instantiate(startingItem, transform, true);
            currentSlotItem.gameObject.SetActive(false);
            currentSlotItem.transform.localPosition = Vector3.zero;
            currentSlotItem.transform.localEulerAngles = Vector3.zero;
            SetupNewMeshClone(currentSlotItem);
            startingTransformFromHand.SetTransformStruct(Vector3.zero, Quaternion.Euler(new Vector3(0, 90, 0)), startingTransformFromHand.scale * .1f);
        }

        Disable = Animator.StringToHash("Disable");
        Enable = Animator.StringToHash("Enable");
    }

    private void OnValidate()
    {
        if (!inventory)
            inventory = GetComponentInParent<PlayerInventory>();
        if (!interactionManager)
            interactionManager = FindObjectOfType<XRInteractionManager>();
        if (!addItemAnimator)
            addItemAnimator = slotDisplayToAddItem.GetComponent<Animator>();
        if (!hasItemAnimator)
            hasItemAnimator = slotDisplayWhenContainsItem.GetComponent<Animator>();
    }

    private void OnEnable()
    {
        isBusy = false;
        isDisabling = false;
        startingTransformFromHand.SetTransformStruct(Vector3.zero, Quaternion.Euler(new Vector3(0, 90, 0)), startingTransformFromHand.scale * .1f);

        StartCoroutine(AnimateIcon());

        if (currentSlotItem)
        {
            boundCenterTransform.gameObject.SetActive(false);
            Invoke(nameof(SetNewItemModel), .25f);
        }

        inventorySlotUpdated.Invoke();
    }

    private void OnDisable() => CancelInvoke();

    public void TryInteractWithSlot(XRBaseInteractor controller)
    {
        if (isBusy) return;
        InteractWithSlot(controller);
    }


    private void InteractWithSlot(XRBaseInteractor controller)
    {
        if (animateItemToSlotCoroutine != null) StopCoroutine(animateItemToSlotCoroutine);

        var itemHandIsHolding = controller.selectTarget;

        if (currentSlotItem)
        {
            DisableItemInHand(controller);
            GetNewItemFromSlot(controller);
        }
        else
        {
            DisableItemInHand(controller);
            releaseAudio.Play();
        }

        //Enable Inventory Slot
        currentSlotItem = itemHandIsHolding;

        StartCoroutine(AnimateIcon());

        SetNewItemModel();
        inventorySlotUpdated.Invoke();
    }

    private bool CheckIfCanAddItemToSlot(XRBaseInteractable itemHandIsHolding)
    {
        InventoryItemHelper helper = itemHandIsHolding.GetComponent<InventoryItemHelper>();
        return helper.canInventory;
    }

    private IEnumerator AnimateIcon()
    {
        isBusy = true;
        if (currentSlotItem) //If has item show item
        {
            if (animateItemToSlotCoroutine != null) StopCoroutine(animateItemToSlotCoroutine);

            addItemAnimator.SetTrigger(Disable);
            slotDisplayWhenContainsItem.gameObject.SetActive(true);
            yield return new WaitForSeconds(AnimationDisableLength / 2);
            slotDisplayToAddItem.gameObject.SetActive(false);
        }
        else //Show add item display
        {
            if (boundCenterTransform) Destroy(boundCenterTransform.gameObject);
            hasItemAnimator.SetTrigger(Disable);
            slotDisplayToAddItem.gameObject.SetActive(true);
            yield return new WaitForSeconds(AnimationDisableLength / 2);
            slotDisplayWhenContainsItem.gameObject.SetActive(false);
        }

        //Better user experience  after waiting to enable collider after some visuals start appearing
        collider.enabled = true;

        // yield return new WaitForSeconds(.25f);
        isBusy = false;
    }


    public void DisableSlot()
    {
        //Disable hand from adding item when animating to disable slot
        collider.enabled = false;
        if (!isDisabling)
            StartCoroutine(DisableAfterAnimation(AnimationDisableLength));
    }

    public void EnableSlot()
    {
        StopAllCoroutines();
        OnEnable();
        //Start animations if the gameobject is turned on
        addItemAnimator.SetTrigger(Enable);
        hasItemAnimator.SetTrigger(Enable);
    }


    private IEnumerator DisableAfterAnimation(float seconds)
    {
        addItemAnimator.SetTrigger(Disable);
        hasItemAnimator.SetTrigger(Disable);

        isDisabling = true;
        float timer = 0;
        float animationLength = .75f;
        while (timer < animationLength + Time.deltaTime)
        {
            if (boundCenterTransform)
                boundCenterTransform.localScale = Vector3.Lerp(boundCenterTransform.localScale, Vector3.zero, timer / animationLength);
            yield return new WaitForSeconds(Time.deltaTime);
            timer += Time.deltaTime;
        }

        isDisabling = false;
        gameObject.SetActive(false);
    }

    private void DisableItemInHand(XRBaseInteractor controller)
    {
        var itemHandIsHolding = controller.selectTarget;
        if (!itemHandIsHolding) return;

        //Release current item
        itemHandIsHolding.GetComponent<ColliderSwitchOnGrab>()?.TurnOnAllCollidersToRemoveXRFromManager();
        ReleaseItemFromHand(controller, itemHandIsHolding);

        var itemHolderTransform = itemModelHolder.transform;
        itemHolderTransform.parent = transform;
        itemHolderTransform.localScale = Vector3.one;
        itemHolderTransform.localPosition = new Vector3(0, 0, 4.3f);
        itemHolderTransform.localEulerAngles = Vector3.zero;

        SetupNewMeshClone(itemHandIsHolding);

        //Disable current item
        StartCoroutine(DisableItem(itemHandIsHolding));

        itemHandIsHolding.transform.parent = transform;
    }

    private void GetNewItemFromSlot(XRBaseInteractor controller)
    {
        currentSlotItem.transform.localPosition = Vector3.zero;

        //Enable Current Item
        currentSlotItem.gameObject.SetActive(true);
        currentSlotItem.transform.parent = null;

        //Set controller to hold interactable
        GrabNewItem(controller, currentSlotItem);
        grabAudio.Play();
    }

    private IEnumerator DisableItem(XRBaseInteractable item)
    {
        item.transform.position += Vector3.one * 9999;
        //Lets physics respond to collider disappearing before disabling object phyics update needs to run twice
        yield return new WaitForSeconds(Time.fixedDeltaTime * 2);
        item.gameObject.SetActive(false);
    }


    private void ReleaseItemFromHand(XRBaseInteractor interactor, XRBaseInteractable interactable) =>
        interactionManager.SelectExit_public(interactor, interactable);

    private void GrabNewItem(XRBaseInteractor interactor, XRBaseInteractable interactable) =>
        interactionManager.SelectEnter_public(interactor, interactable);


    private void SetupNewMeshClone(XRBaseInteractable itemHandIsHolding)
    {
        if (itemSlotMeshClone)
            Destroy(itemSlotMeshClone.gameObject);

        itemSlotMeshClone = Instantiate(itemHandIsHolding, itemModelHolder, true).transform;
        itemSlotMeshClone.gameObject.SetActive(true);

        //Match clone to original object transform
        itemSlotMeshClone.transform.SetPositionAndRotation(itemHandIsHolding.transform.position, itemHandIsHolding.transform.rotation);

        //Disable clone from interacting with world
        DestroyComponentsOnClone(itemSlotMeshClone);

        //Create a new parent for the item at the center of the mesh's
        Bounds bounds = GetBoundsOfAllMeshes(itemSlotMeshClone.transform);
        boundCenterTransform = new GameObject("Bound Center Transform").transform;

        //Match rotation of item in hand to setup starting animation point
        boundCenterTransform.rotation = itemHandIsHolding.transform.rotation;

        //Set position to be the center of the bounds 
        boundCenterTransform.position = bounds.center;

        //Organize hiearchy
        boundCenterTransform.parent = itemModelHolder;

        //Place model as child of center point to act as the new pivot point
        itemSlotMeshClone.transform.parent = boundCenterTransform;

        //Set starting transform to animate object from hand into the inventory slot
        startingTransformFromHand.SetTransformStruct(boundCenterTransform.localPosition, boundCenterTransform.localRotation, boundCenterTransform.localScale);
        boundCenterTransform.localEulerAngles = new Vector3(0, 90, 0);

        //Shrink item to fit within inventorySizeCollider
        inventorySize.enabled = true;
        Vector3 parentSize = inventorySize.bounds.size;
        while (bounds.size.x > parentSize.x || bounds.size.y > parentSize.y || bounds.size.z > parentSize.z)
        {
            bounds = GetBoundsOfAllMeshes(boundCenterTransform.transform);
            boundCenterTransform.transform.localScale *= 0.9f;
        }

        inventorySize.enabled = false;


        goalSizeToFitInSlot = boundCenterTransform.transform.localScale;
    }

    private void DestroyComponentsOnClone(Transform clone)
    {
        //Destroy almost all components besides models - Could not foreach through Components because it destroys out of order causing issues
        var monoBehaviors = clone.GetComponentsInChildren<MonoBehaviour>();
        foreach (var t in monoBehaviors) Destroy(t);

        var rigidBodies = clone.GetComponentsInChildren<Rigidbody>();
        foreach (var t in rigidBodies) Destroy(t);

        var colliders = clone.GetComponentsInChildren<Collider>();
        foreach (var t in colliders) Destroy(t);
    }

    private void SetNewItemModel()
    {
        if (!currentSlotItem)
            return;

        //Create a clone of the new item
        if (!itemSlotMeshClone)
            SetupNewMeshClone(currentSlotItem);
        animateItemToSlotCoroutine = StartCoroutine(AnimateItemToSlot());
    }

    private Coroutine animateItemToSlotCoroutine;

    private IEnumerator AnimateItemToSlot()
    {
        Vector3 goalScale = goalSizeToFitInSlot;
        float timer = 0;
        boundCenterTransform.localPosition = startingTransformFromHand.position;
        boundCenterTransform.localScale = startingTransformFromHand.scale;
        boundCenterTransform.localRotation = startingTransformFromHand.rotation;
        boundCenterTransform.gameObject.SetActive(true);
        while (timer < AnimationLengthItemToSlot + Time.deltaTime)
        {
            boundCenterTransform.localPosition = Vector3.Lerp(boundCenterTransform.localPosition, Vector3.zero, timer / AnimationLengthItemToSlot);
            boundCenterTransform.localScale = Vector3.Lerp(boundCenterTransform.localScale, goalScale, timer / AnimationLengthItemToSlot);
            boundCenterTransform.localRotation = Quaternion.Lerp(boundCenterTransform.localRotation, Quaternion.Euler(new Vector3(0, 90, 0)), timer / AnimationLengthItemToSlot);

            yield return new WaitForSeconds(Time.deltaTime);
            timer += Time.deltaTime;
        }

        itemModelHolder.transform.parent = backImagesThatRotate;
    }


    private Bounds GetBoundsOfAllMeshes(Transform item)
    {
        Bounds bounds = new Bounds();
        Renderer[] rends = itemSlotMeshClone.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in rends)
        {
            if (rend.GetComponent<ParticleSystem>()) continue;

            if (bounds.extents == Vector3.zero)
                bounds = rend.bounds;

            bounds.Encapsulate(rend.bounds);
        }

        return bounds;
    }

    private void OnDrawGizmos()
    {
        if (!itemSlotMeshClone) return;
        Bounds tempBounds = GetBoundsOfAllMeshes(itemSlotMeshClone);
        Gizmos.DrawWireCube(tempBounds.center, tempBounds.size);
        Gizmos.DrawSphere(tempBounds.center, .01f);
    }

    public InventorySlot(TransformStruct startingTransformFromHand)
    {
        this.startingTransformFromHand = startingTransformFromHand;
    }
}

public static class BoundsExtension
{
    public static Bounds GrowBounds(this Bounds a, Bounds b)
    {
        Vector3 max = Vector3.Max(a.max, b.max);
        Vector3 min = Vector3.Min(a.min, b.min);

        a = new Bounds((max + min) * 0.5f, max - min);
        return a;
    }
}