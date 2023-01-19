using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
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

        public XRBaseInteractable CurrentSlotItem => currentSlotItem;
        public UnityEvent inventorySlotUpdated;

        private XRBaseInteractable currentSlotItem;
        private Transform boundCenterTransform, itemSlotMeshClone;
        private XRInteractionManager interactionManager;
        private InventoryManager inventoryManager;

        //Animation
        private int disableAnimatorHash, enableAnimatorHash, onHoverAnimatorHash, resetAnimatorHash;

        private bool isBusy, isDisabling;
        private Animator addItemAnimator, hasItemAnimator;
        private TransformStruct startingTransformFromHand;
        private Vector3 goalSizeToFitInSlot;
        private const float AnimationDisableLength = .5f, AnimationLengthItemToSlot = .15f;

        private void Awake()
        {
            OnValidate();

            disableAnimatorHash = Animator.StringToHash("Disable");
            enableAnimatorHash = Animator.StringToHash("Enable");
            onHoverAnimatorHash = Animator.StringToHash("OnHover");
            resetAnimatorHash = Animator.StringToHash("Reset");
        }

        public IEnumerator CreateStartingItemAndDisable()
        {
            //Called from PlayerInventory, to give a frame for the start methods to be called on currentSlotItem
            if (startingItem)
            {
                currentSlotItem = Instantiate(startingItem, transform, true);
                yield return null;
                currentSlotItem.gameObject.SetActive(false);
                currentSlotItem.transform.localPosition = Vector3.zero;
                currentSlotItem.transform.localEulerAngles = Vector3.zero;
                startingTransformFromHand.SetTransformStruct(
                    Vector3.zero, Quaternion.Euler(new Vector3(0, 90, 0)), startingTransformFromHand.scale * .1f);
                SetupNewMeshClone(currentSlotItem);
            }

            gameObject.SetActive(false);
        }


        private void OnValidate()
        {
            if (!inventoryManager)
                inventoryManager = GetComponentInParent<InventoryManager>();
            if (!interactionManager)
                interactionManager = FindObjectOfType<XRInteractionManager>();
            if (!addItemAnimator)
                addItemAnimator = slotDisplayToAddItem.GetComponent<Animator>();
            if (!hasItemAnimator)
                hasItemAnimator = slotDisplayWhenContainsItem.GetComponent<Animator>();
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
            ResetAnimationState(hasItemAnimator, true);
            ResetAnimationState(addItemAnimator, true);
            //Start animations if the gameobject is turned on
            addItemAnimator.SetTrigger(enableAnimatorHash);
            hasItemAnimator.SetTrigger(enableAnimatorHash);
        }

        private void ResetAnimationState(Animator anim, bool setToStartingAnimState)
        {
            anim.ResetTrigger(enableAnimatorHash);
            anim.ResetTrigger(disableAnimatorHash);
            anim.SetBool(onHoverAnimatorHash, false);
            if (setToStartingAnimState)
                hasItemAnimator.SetTrigger(resetAnimatorHash);
        }


        private void OnEnable()
        {
            isBusy = false;
            isDisabling = false;
            startingTransformFromHand.SetTransformStruct(
                Vector3.zero, Quaternion.Euler(new Vector3(0, 90, 0)), startingTransformFromHand.scale * .1f);

            StartCoroutine(AnimateIcon());

            if (currentSlotItem)
            {
                if (boundCenterTransform)
                    boundCenterTransform.gameObject.SetActive(false);
                Invoke(nameof(SetNewItemModel), .25f);
            }

            inventorySlotUpdated.Invoke();
        }

        private void OnDisable() => CancelInvoke(nameof(SetNewItemModel));

        public void TryInteractWithSlot(XRDirectInteractor controller)
        {
            if (isBusy || isDisabling) return;
            InteractWithSlot(controller);
        }


        private void InteractWithSlot(XRDirectInteractor controller)
        {
            if (animateItemToSlotCoroutine != null) StopCoroutine(animateItemToSlotCoroutine);
            
            XRBaseInteractable itemHandIsHolding = null;
            if(controller.hasSelection) itemHandIsHolding = controller.selectTarget;

            //Check if item is allowed to be added to inventory
            if (itemHandIsHolding)
            {
                var itemData = itemHandIsHolding.GetComponent<InteractableItemData>();
                if (!itemData || !itemData.canInventory) return;
            }

            if (currentSlotItem)
            {
                DisableItemInHand(controller);
                GetNewItemFromSlot(controller);
            }
            else
            {
                DisableItemInHand(controller);
            }

            //Enable Inventory Slot
            currentSlotItem = itemHandIsHolding;

            StartCoroutine(AnimateIcon());
            SetNewItemModel();
            inventorySlotUpdated.Invoke();
        }

        private bool CheckIfCanAddItemToSlot(XRBaseInteractable itemHandIsHolding)
        {
            // Itemda helper = itemHandIsHolding.GetComponent<InventoryItemHelper>();
            // return helper.canInventory;
            return true;
        }

        private IEnumerator AnimateIcon()
        {
            isBusy = true;
            if (currentSlotItem) //If has item show item
            {
                if (animateItemToSlotCoroutine != null) StopCoroutine(animateItemToSlotCoroutine);

                addItemAnimator.SetTrigger(disableAnimatorHash);
                slotDisplayWhenContainsItem.gameObject.SetActive(true);
                yield return new WaitForSeconds(AnimationDisableLength / 2);
                slotDisplayToAddItem.gameObject.SetActive(false);
            }
            else //Show add item display
            {
                if (boundCenterTransform) Destroy(boundCenterTransform.gameObject);
                hasItemAnimator.SetTrigger(disableAnimatorHash);
                slotDisplayToAddItem.gameObject.SetActive(true);
                yield return new WaitForSeconds(AnimationDisableLength / 2);
                slotDisplayWhenContainsItem.gameObject.SetActive(false);
            }

            //Better user experience  after waiting to enable collider after some visuals start appearing
            collider.enabled = true;

            // yield return new WaitForSeconds(.25f);
            isBusy = false;
        }


        private IEnumerator DisableAfterAnimation(float seconds)
        {
            ResetAnimationState(addItemAnimator, false);
            ResetAnimationState(hasItemAnimator, false);
            
            addItemAnimator.SetTrigger(disableAnimatorHash);
            hasItemAnimator.SetTrigger(disableAnimatorHash);
            
            isDisabling = true;
            float timer = 0;
            float animationLength = .75f;
            while (timer < animationLength + Time.deltaTime)
            {
                if (boundCenterTransform)
                    boundCenterTransform.localScale = Vector3.Lerp(boundCenterTransform.localScale, Vector3.zero, timer / animationLength);
                yield return null;
                timer += Time.deltaTime;
            }
            
            isDisabling = false;
            gameObject.SetActive(false);
        }

        private void DisableItemInHand(XRBaseInteractor controller)
        {
            var itemHandIsHolding = controller.selectTarget;
            if (!itemHandIsHolding) return;

            releaseAudio.Play();

            //Release current item
            ReleaseItemFromHand(controller, itemHandIsHolding);

            var itemHolderTransform = itemModelHolder.transform;
            itemHolderTransform.parent = transform;
            itemHolderTransform.localScale = Vector3.one;
            itemHolderTransform.localPosition = new Vector3(0, 0, 4.3f);
            itemHolderTransform.localEulerAngles = Vector3.zero;

            //Disable current item
            StartCoroutine(DisableItem(itemHandIsHolding));

            itemHandIsHolding.transform.parent = transform;
        }

        //Lets physics respond to collider disappearing before disabling object physics update needs to run twice
        private IEnumerator DisableItem(XRBaseInteractable item)
        {
            item.gameObject.SetActive(true); //Force gameObject on to get collision events
            yield return null;

            item.GetComponent<OnGrabEnableDisable>()?.EnableAll(); //Force collider on to get collision events
            item.transform.position = Vector3.down * 9999;

            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);

            currentSlotItem.transform.localPosition = Vector3.zero; //Return to position
            item.gameObject.SetActive(false);

            yield return new WaitForSeconds(Time.fixedDeltaTime);

            SetupNewMeshClone(item);
        }

        private void GetNewItemFromSlot(XRBaseInteractor controller)
        {
            //Enable Current Item
            currentSlotItem.gameObject.SetActive(true);
            currentSlotItem.transform.parent = null;

            //Set controller to hold interactable
            GrabNewItem(controller, currentSlotItem);
            grabAudio.Play();
        }

        private void ReleaseItemFromHand(XRBaseInteractor interactor, XRBaseInteractable interactable) =>
            interactionManager.SelectExit(interactor, interactable);

        private void GrabNewItem(XRBaseInteractor interactor, XRBaseInteractable interactable) =>
            interactionManager.SelectEnter(interactor, interactable);


        private void SetupNewMeshClone(XRBaseInteractable itemHandIsHolding)
        {
            if (itemSlotMeshClone)
                Destroy(itemSlotMeshClone.gameObject);

            itemSlotMeshClone = Instantiate(itemHandIsHolding, itemModelHolder, true).transform;

            //Disable clone from interacting with world
            DestroyComponentsOnClone(itemSlotMeshClone);

            //Destroy components and then activating object does not work, must be called a tick after destroy or awakes get called
            Invoke(nameof(ActivateItemSlotMeshClone), 0);

            //Match clone to original object transform
            itemSlotMeshClone.transform.SetPositionAndRotation(itemHandIsHolding.transform.position, itemHandIsHolding.transform.rotation);

            //Create a new parent for the item at the center of the mesh's
            Bounds bounds = GetBoundsOfAllMeshes(itemSlotMeshClone.transform);
            if (!boundCenterTransform)
                boundCenterTransform = new GameObject("Bound Center Transform").transform;

            //Match rotation of item in hand to setup starting animation point
            boundCenterTransform.rotation = itemHandIsHolding.transform.rotation;

            //Set position to be the center of the bounds 
            boundCenterTransform.position = bounds.center;

            //Organize hierarchy
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

            animateItemToSlotCoroutine = StartCoroutine(AnimateItemToSlot());
        }

        private void ActivateItemSlotMeshClone() => itemSlotMeshClone.gameObject.SetActive(true);

        private void DestroyComponentsOnClone(Transform clone)
        {
            var movedColliders = clone.GetComponentsInChildren<IReturnMovedColliders>(true);
            foreach (var t in movedColliders) t.ReturnMovedColliders();

            //Destroy almost all components - Could not foreach through Components because it destroys out of order causing issues
            var monoBehaviors = clone.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var t in monoBehaviors) Destroy(t);

            var rigidBodies = clone.GetComponentsInChildren<Rigidbody>(true);
            foreach (var t in rigidBodies) Destroy(t);

            var colliders = clone.GetComponentsInChildren<Collider>(true);
            foreach (var t in colliders) Destroy(t);

            var lights = clone.GetComponentsInChildren<Light>(true);
            foreach (var t in lights) Destroy(t);
        }

        private void SetNewItemModel()
        {
            if (!currentSlotItem)
                return;

            //Create a clone of the new item
            if (!itemSlotMeshClone)
                SetupNewMeshClone(currentSlotItem);
            else
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

                yield return null;
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

        private void OnTriggerEnter(Collider other)
        {
            var controller = other.GetComponent<ActionBasedController>();
            if (controller)
            {
                slotDisplayToAddItem.GetComponent<Animator>().SetBool(onHoverAnimatorHash, true);
                slotDisplayWhenContainsItem.GetComponent<Animator>().SetBool(onHoverAnimatorHash, true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var controller = other.GetComponent<ActionBasedController>();
            if (controller)
            {
                slotDisplayToAddItem.GetComponent<Animator>().SetBool(onHoverAnimatorHash, false);
                slotDisplayWhenContainsItem.GetComponent<Animator>().SetBool(onHoverAnimatorHash, false);
            }
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
}