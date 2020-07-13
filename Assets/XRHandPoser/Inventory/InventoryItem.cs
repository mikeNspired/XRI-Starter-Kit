using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class InventoryItem : MonoBehaviour
{
    public XRBaseInteractable currentSlotItem;

    [SerializeField] [Tooltip("Optional Starting item")]
    private XRBaseInteractable startingItem = null;

    [SerializeField] [Tooltip("Display used when holding slot is holding an item")]
    private GameObject ArtWhenHoldingItem = null;

    [SerializeField] [Tooltip("Display used when slot is empty and can add an item")]
    private GameObject ArtWhenNoIteam = null;

    [SerializeField] [Tooltip("Transform to hold the viewing model of the current Inventory Slot Item.")]
    private Transform ItemModelHolder = null;

    [SerializeField] [Tooltip("Transform of back image that rotates durring animations, used to attach ItemModelHolder to after positioning model")]
    private Transform backImagesThatRotate = null;

    private XRInteractionManager interactionManager;
    private XRSimpleInteractable interactable;
    private PlayerInventory inventory;
    private Vector3 originalPosition;
    private Transform boundCenterTransform;
    private bool isBusy = false;
    private Animator addItem, withItem;
    private Transform itemSlotMeshClone;
    private TransformStruct startingTransformFromHand;
    private Vector3 goalSizeToFit;
    
    void Awake()
    {
        OnValidate();

        if (!currentSlotItem && startingItem)
        {
            currentSlotItem = Instantiate(startingItem, transform, true);
            currentSlotItem.gameObject.SetActive(false);
            currentSlotItem.transform.localPosition = Vector3.zero;
            currentSlotItem.transform.localEulerAngles = Vector3.zero;
            SetupNewMeshClone(currentSlotItem);
            startingTransformFromHand.SetTransformStruct(Vector3.zero, Quaternion.Euler(new Vector3(0, 90, 0)), startingTransformFromHand.scale * .1f);
        }


        addItem = ArtWhenNoIteam.GetComponent<Animator>();
        withItem = ArtWhenHoldingItem.GetComponent<Animator>();
        originalPosition = transform.localPosition;
    }

    private void OnValidate()
    {
        if (!inventory)
            inventory = GetComponentInParent<PlayerInventory>();
        if (!interactable)
            interactable = GetComponent<XRSimpleInteractable>();
        if (!interactionManager)
            interactionManager = FindObjectOfType<XRInteractionManager>();
    }

    private void OnEnable()
    {
        transform.localPosition = originalPosition;
        isBusy = false;
        StartCoroutine(AnimateIcon());

        if (currentSlotItem)
        {
            boundCenterTransform.gameObject.SetActive(false);
            Invoke(nameof(SetNewItemModel), .25f);
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (isBusy) return;
        var controller = other.GetComponent<XRBaseControllerInteractor>();

        if (!controller) return;

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
            DisableItemInHand(controller);


        //Enable Inventory Slot
        currentSlotItem = itemHandIsHolding;

        StartCoroutine(AnimateIcon());

        SetNewItemModel();
    }

    private float AnimationDisableLength = .5f;

    private IEnumerator AnimateIcon()
    {
        isBusy = true;
        if (currentSlotItem)
        {
            if (animateItemToSlotCoroutine != null) StopCoroutine(animateItemToSlotCoroutine);

            addItem.SetTrigger("Disable");
            ArtWhenHoldingItem.gameObject.SetActive(true);
            yield return new WaitForSeconds(AnimationDisableLength / 2);
            ArtWhenNoIteam.gameObject.SetActive(false);
        }
        else
        {
            if (boundCenterTransform) Destroy(boundCenterTransform.gameObject);
            withItem.SetTrigger("Disable");
            ArtWhenNoIteam.gameObject.SetActive(true);
            yield return new WaitForSeconds(AnimationDisableLength / 2);
            ArtWhenHoldingItem.gameObject.SetActive(false);
        }

        // yield return new WaitForSeconds(.25f);
        isBusy = false;
    }

    public void DisableSlot()
    {
        StartCoroutine(DisableAfterAnimation(AnimationDisableLength));
    }


    private IEnumerator DisableAfterAnimation(float seconds)
    {
        addItem.SetTrigger("Disable");
        withItem.SetTrigger("Disable");
        
        float timer = 0;
        float animationLength = .75f;
        while (timer < animationLength + Time.deltaTime)
        {
            boundCenterTransform.localScale = Vector3.Lerp(boundCenterTransform.localScale, Vector3.zero, timer / animationLength);
            yield return new WaitForSeconds(Time.deltaTime);
            timer += Time.deltaTime;
        }

        gameObject.SetActive(false);
    }

    private void DisableItemInHand(XRBaseInteractor controller)
    {
        var itemHandIsHolding = controller.selectTarget;
        if (!itemHandIsHolding || itemHandIsHolding == interactable) return;

        //Release current item
        ReleaseItemFromHand(controller, itemHandIsHolding);

        ItemModelHolder.transform.parent = transform;
        ItemModelHolder.transform.localScale = Vector3.one;
        ItemModelHolder.transform.localPosition = new Vector3(0, 0, 4.3f);
        ItemModelHolder.transform.localEulerAngles = Vector3.zero;

        SetupNewMeshClone(itemHandIsHolding);

        //Disable current item
        StartCoroutine(DisableItem(itemHandIsHolding.transform));

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
    }

    private IEnumerator DisableItem(Transform item) //Lets physics respond to collider disappearing before disabling object
    {
        // item.position = Vector3.one * 9999;
        Debug.Log("Set position to 9999");

        yield return null;
        item.gameObject.SetActive(false);
    }

    private void ReleaseItemFromHand(XRBaseInteractor interactor, XRBaseInteractable interactable) =>
        interactionManager.SelectExit_public(interactor, interactable);

    private void GrabNewItem(XRBaseInteractor interactor, XRBaseInteractable interactable) =>
        interactionManager.SelectEnter_public(interactor, interactable);


    private void SetupNewMeshClone(XRBaseInteractable itemHandIsHolding)
    {
        if(itemSlotMeshClone)
            Destroy(itemSlotMeshClone.gameObject);
        itemSlotMeshClone = Instantiate(itemHandIsHolding, ItemModelHolder, true).transform;
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
        boundCenterTransform.parent = ItemModelHolder;

        //Place model as child of center point to act as the new pivot point
        itemSlotMeshClone.transform.parent = boundCenterTransform;

        //Set starting transform to animate object from hand into the inventory slot
        startingTransformFromHand.SetTransformStruct(boundCenterTransform.localPosition, boundCenterTransform.localRotation, boundCenterTransform.localScale);

        boundCenterTransform.localEulerAngles = new Vector3(0, 90, 0);

        //Shrink item to fit within inventorySizeCollider
        Vector3 parentSize = inventorySize.bounds.size;
        while (bounds.size.x > parentSize.x || bounds.size.y > parentSize.y || bounds.size.z > parentSize.z)
        {
            bounds = GetBoundsOfAllMeshes(boundCenterTransform.transform);
            boundCenterTransform.transform.localScale *= 0.9f;
        }

        goalSizeToFit = boundCenterTransform.transform.localScale;
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
        Vector3 goalScale = goalSizeToFit;
        float timer = 0;
        boundCenterTransform.localPosition = startingTransformFromHand.position;
        boundCenterTransform.localScale = startingTransformFromHand.scale;
        boundCenterTransform.localRotation = startingTransformFromHand.rotation;
        boundCenterTransform.gameObject.SetActive(true);
        float animationLength = .15f;
        while (timer < animationLength + Time.deltaTime)
        {
            boundCenterTransform.localPosition = Vector3.Lerp(boundCenterTransform.localPosition, Vector3.zero, timer / animationLength);
            boundCenterTransform.localScale = Vector3.Lerp(boundCenterTransform.localScale, goalScale, timer / animationLength);
            boundCenterTransform.localEulerAngles = Vector3.Lerp(boundCenterTransform.localEulerAngles, new Vector3(0, 90, 0), timer / animationLength);

            yield return new WaitForSeconds(Time.deltaTime);
            timer += Time.deltaTime;
        }

        ItemModelHolder.transform.parent = backImagesThatRotate;
    }
    

    private Bounds GetBoundsOfAllMeshes(Transform item)
    {
        Renderer[] rends = item.GetComponentsInChildren<Renderer>();
        Bounds bounds = rends[0].bounds;
        foreach (Renderer rend in rends)
        {
            if(rend.GetComponent<ParticleSystem>()) continue;
            bounds = bounds.GrowBounds( rend.bounds );
        }
        return bounds;
    }

    private void OnDrawGizmos()
    {
        var bounds = GetBoundsOfAllMeshes(itemSlotMeshClone);
        Gizmos.DrawWireCube(bounds.center,bounds.size);
        Gizmos.DrawSphere(bounds.center,.01f);
    }

    public BoxCollider inventorySize;
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