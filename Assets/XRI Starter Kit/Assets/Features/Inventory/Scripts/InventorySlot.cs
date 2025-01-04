using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.UnityXRHandPoser
{
    [RequireComponent(typeof(Collider))]
    public class InventorySlot : MonoBehaviour
    {
        [SerializeField, Tooltip("Optional Starting item")]
        private XRBaseInteractable startingItem = null;

        [SerializeField, Tooltip("Display used when holding slot is holding an item")]
        private GameObject slotDisplayWhenContainsItem = null;

        [SerializeField, Tooltip("Display used when slot is empty and can add an item")]
        private GameObject slotDisplayToAddItem = null;

        [SerializeField, Tooltip("Transform to hold the viewing model of the current Inventory Slot Item.")]
        private Transform itemModelHolder = null;

        [SerializeField, Tooltip("Transform of back image that rotates during animations, used to attach ItemModelHolder to after positioning model")]
        private Transform backImagesThatRotate = null;

        [SerializeField, Tooltip("Item will be scaled down to size to fit inside this box collider")]
        private BoxCollider inventorySize = null;

        [SerializeField] private new Collider collider = null;
        [SerializeField] private AudioSource grabAudio = null, releaseAudio = null;

        public XRBaseInteractable CurrentSlotItem => currentSlotItem;
        public UnityEvent inventorySlotUpdated;

        private XRBaseInteractable currentSlotItem;
        private Transform boundCenterTransform, itemSlotMeshClone;
        private XRInteractionManager interactionManager;
        private InventoryManager inventoryManager;

        // Animation
        private int disableAnimatorHash, enableAnimatorHash, onHoverAnimatorHash, resetAnimatorHash;

        private bool isBusy, isDisabling;
        private Animator addItemAnimator, hasItemAnimator;
        private TransformStruct startingTransformFromHand;
        private Vector3 goalSizeToFitInSlot;
        private const float AnimationDisableLength = 0.5f;
        private const float AnimationLengthItemToSlot = 0.15f;

        private Coroutine animateItemToSlotCoroutine;

        private void Awake()
        {
            OnValidate();

            disableAnimatorHash = Animator.StringToHash("Disable");
            enableAnimatorHash = Animator.StringToHash("Enable");
            onHoverAnimatorHash = Animator.StringToHash("OnHover");
            resetAnimatorHash = Animator.StringToHash("Reset");
        }

        /// <summary>
        /// Called from PlayerInventory, to give a frame for the Start methods
        /// to be called on currentSlotItem before disabling.
        /// </summary>
        public IEnumerator CreateStartingItemAndDisable()
        {
            if (startingItem)
            {
                currentSlotItem = Instantiate(startingItem, transform, true);
                yield return null;
                currentSlotItem.gameObject.SetActive(false);
                currentSlotItem.transform.localPosition = Vector3.zero;
                currentSlotItem.transform.localEulerAngles = Vector3.zero;
                startingTransformFromHand.SetTransformStruct(
                    Vector3.zero,
                    Quaternion.Euler(new Vector3(0, 90, 0)),
                    startingTransformFromHand.scale * 0.1f);

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

            if (!addItemAnimator && slotDisplayToAddItem)
                addItemAnimator = slotDisplayToAddItem.GetComponent<Animator>();

            if (!hasItemAnimator && slotDisplayWhenContainsItem)
                hasItemAnimator = slotDisplayWhenContainsItem.GetComponent<Animator>();
        }

        public void DisableSlot()
        {
            // Disable slot collider so items can't be added while animating out
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
            // Start "Enable" animations
            addItemAnimator.SetTrigger(enableAnimatorHash);
            hasItemAnimator.SetTrigger(enableAnimatorHash);
        }

        private void ResetAnimationState(Animator anim, bool setToStartingAnimState)
        {
            anim.ResetTrigger(enableAnimatorHash);
            anim.ResetTrigger(disableAnimatorHash);
            anim.SetBool(onHoverAnimatorHash, false);
            if (setToStartingAnimState)
                anim.SetTrigger(resetAnimatorHash);
        }

        private void OnEnable()
        {
            isBusy = false;
            isDisabling = false;
            startingTransformFromHand.SetTransformStruct(
                Vector3.zero,
                Quaternion.Euler(new Vector3(0, 90, 0)),
                startingTransformFromHand.scale * 0.1f);

            StartCoroutine(AnimateIcon());

            if (currentSlotItem)
            {
                // Hide the bound center transform if we already have an item
                if (boundCenterTransform)
                    boundCenterTransform.gameObject.SetActive(false);
                Invoke(nameof(SetNewItemModel), 0.25f);
            }

            inventorySlotUpdated.Invoke();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(SetNewItemModel));
        }

        /// <summary>
        /// Checks if the slot can accept an item from the given XRDirectInteractor,
        /// then triggers the interaction logic.
        /// </summary>
        public void TryInteractWithSlot(XRDirectInteractor controller)
        {
            if (isBusy || isDisabling)
                return;

            InteractWithSlot(controller);
        }

        private void InteractWithSlot(XRDirectInteractor controller)
        {
            if (animateItemToSlotCoroutine != null)
                StopCoroutine(animateItemToSlotCoroutine);

            // In the newer XR Interaction Toolkit, items are stored in interactablesSelected
            XRBaseInteractable itemHandIsHolding = null;
            if (controller.hasSelection && controller.interactablesSelected.Count > 0)
                itemHandIsHolding = controller.interactablesSelected[0] as XRBaseInteractable;

            // Check if item can be stored in inventory
            if (itemHandIsHolding)
            {
                var itemData = itemHandIsHolding.GetComponent<InteractableItemData>();
                if (itemData != null && !itemData.canInventory)
                    return;
            }

            // If there's already an item in the slot, swap them.
            if (currentSlotItem)
            {
                DisableItemInHand(controller);
                GetNewItemFromSlot(controller);
            }
            else
            {
                // Otherwise, simply store the new item from the controller.
                DisableItemInHand(controller);
            }

            currentSlotItem = itemHandIsHolding;
            StartCoroutine(AnimateIcon());
            SetNewItemModel();
            inventorySlotUpdated.Invoke();
        }

        private IEnumerator AnimateIcon()
        {
            isBusy = true;

            if (currentSlotItem)
            {
                // We have an item in the slot
                if (animateItemToSlotCoroutine != null)
                    StopCoroutine(animateItemToSlotCoroutine);

                addItemAnimator.SetTrigger(disableAnimatorHash);
                slotDisplayWhenContainsItem.gameObject.SetActive(true);

                // Wait for half the disable animation
                yield return new WaitForSeconds(AnimationDisableLength * 0.5f);
                slotDisplayToAddItem.gameObject.SetActive(false);
            }
            else
            {
                // Show empty-slot display
                if (boundCenterTransform)
                    Destroy(boundCenterTransform.gameObject);

                hasItemAnimator.SetTrigger(disableAnimatorHash);
                slotDisplayToAddItem.gameObject.SetActive(true);

                // Wait for half the disable animation
                yield return new WaitForSeconds(AnimationDisableLength * 0.5f);
                slotDisplayWhenContainsItem.gameObject.SetActive(false);
            }

            // Re-enable collider after some animation has played
            collider.enabled = true;
            isBusy = false;
        }

        private IEnumerator DisableAfterAnimation(float seconds)
        {
            ResetAnimationState(addItemAnimator, false);
            ResetAnimationState(hasItemAnimator, false);

            addItemAnimator.SetTrigger(disableAnimatorHash);
            hasItemAnimator.SetTrigger(disableAnimatorHash);

            isDisabling = true;
            float timer = 0f;
            float animationLength = 0.75f;

            while (timer < animationLength + Time.deltaTime)
            {
                if (boundCenterTransform)
                {
                    boundCenterTransform.localScale = Vector3.Lerp(
                        boundCenterTransform.localScale,
                        Vector3.zero,
                        timer / animationLength
                    );
                }
                yield return null;
                timer += Time.deltaTime;
            }

            isDisabling = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Forces the item in the controller's hand to be de-selected and then disabled,
        /// so we can "store" it in the slot.
        /// </summary>
        private void DisableItemInHand(XRDirectInteractor controller)
        {
            XRBaseInteractable itemHandIsHolding = null;
            if (controller.hasSelection && controller.interactablesSelected.Count > 0)
                itemHandIsHolding = controller.interactablesSelected[0] as XRBaseInteractable;

            if (!itemHandIsHolding)
                return;

            releaseAudio?.Play();

            // Force the controller to deselect its current item
            ReleaseItemFromHand(controller, itemHandIsHolding);

            // Temporarily adjust the itemModelHolder transform
            var itemHolderTransform = itemModelHolder.transform;
            itemHolderTransform.SetParent(transform);
            itemHolderTransform.localScale = Vector3.one;
            itemHolderTransform.localPosition = new Vector3(0, 0, 4.3f);
            itemHolderTransform.localEulerAngles = Vector3.zero;

            // Move the actual item transform under this slot, then disable it
            itemHandIsHolding.transform.SetParent(transform);
            StartCoroutine(DisableItem(itemHandIsHolding));
        }

        /// <summary>
        /// Let physics/engine update once or twice before disabling, so collisions
        /// can respond to the disappearing collider.
        /// </summary>
        private IEnumerator DisableItem(XRBaseInteractable item)
        {
            // Make sure the GameObject is active so colliders exist
            item.gameObject.SetActive(true);
            yield return null;

            item.GetComponent<OnGrabEnableDisable>()?.EnableAll();
            item.transform.position = Vector3.down * 9999;

            // Wait a couple fixed updates so the engine processes collider changes
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);

            // Snap it back, then disable
            currentSlotItem.transform.localPosition = Vector3.zero;
            item.gameObject.SetActive(false);

            // Wait one more frame before cloning
            yield return new WaitForSeconds(Time.fixedDeltaTime);

            SetupNewMeshClone(item);
        }

        /// <summary>
        /// Pull the current slot item into the player's hand.
        /// </summary>
        private void GetNewItemFromSlot(XRDirectInteractor controller)
        {
            // Enable item and un-parent it
            currentSlotItem.gameObject.SetActive(true);
            currentSlotItem.transform.SetParent(null);

            GrabNewItem(controller, currentSlotItem);
            grabAudio?.Play();
        }

        private void ReleaseItemFromHand(XRDirectInteractor interactor, XRBaseInteractable interactable)
        {
            interactionManager.SelectExit(interactor, (IXRSelectInteractable) interactable);
        }

        private void GrabNewItem(XRDirectInteractor interactor, XRBaseInteractable interactable)
        {
            interactionManager.SelectEnter(interactor, (IXRSelectInteractable) interactable);
        }

        /// <summary>
        /// Creates a no-collider clone of the item’s mesh to display in the slot,
        /// then animates it into position.
        /// </summary>
        private void SetupNewMeshClone(XRBaseInteractable itemHandIsHolding)
        {
            if (itemSlotMeshClone)
                Destroy(itemSlotMeshClone.gameObject);

            // Instantiate a clone of the original item under itemModelHolder
            itemSlotMeshClone = Instantiate(itemHandIsHolding, itemModelHolder, true).transform;

            // Remove XR or physics components from the clone
            DestroyComponentsOnClone(itemSlotMeshClone);

            // Defer activation of the clone after we remove its scripts/components
            Invoke(nameof(ActivateItemSlotMeshClone), 0);

            // Match world pose
            itemSlotMeshClone.SetPositionAndRotation(
                itemHandIsHolding.transform.position,
                itemHandIsHolding.transform.rotation
            );

            // Create or reuse a center pivot transform
            Bounds bounds = GetBoundsOfAllMeshes(itemSlotMeshClone.transform);
            if (!boundCenterTransform)
                boundCenterTransform = new GameObject("Bound Center Transform").transform;

            // Match rotation of item in hand
            boundCenterTransform.rotation = itemHandIsHolding.transform.rotation;
            boundCenterTransform.position = bounds.center;
            boundCenterTransform.SetParent(itemModelHolder);

            // Re-parent mesh clone under the bound center pivot
            itemSlotMeshClone.SetParent(boundCenterTransform);

            // Record the "start" transform for the animation
            startingTransformFromHand.SetTransformStruct(
                boundCenterTransform.localPosition,
                boundCenterTransform.localRotation,
                boundCenterTransform.localScale
            );
            boundCenterTransform.localEulerAngles = new Vector3(0, 90, 0);

            // Shrink the item to fit in the inventory box
            inventorySize.enabled = true;
            Vector3 parentSize = inventorySize.bounds.size;
            while (bounds.size.x > parentSize.x ||
                   bounds.size.y > parentSize.y ||
                   bounds.size.z > parentSize.z)
            {
                bounds = GetBoundsOfAllMeshes(boundCenterTransform);
                boundCenterTransform.localScale *= 0.9f;
            }
            inventorySize.enabled = false;

            goalSizeToFitInSlot = boundCenterTransform.localScale;
            animateItemToSlotCoroutine = StartCoroutine(AnimateItemToSlot());
        }

        private void ActivateItemSlotMeshClone()
        {
            if (itemSlotMeshClone != null)
                itemSlotMeshClone.gameObject.SetActive(true);
        }

        /// <summary>
        /// Removes any unneeded physics or XR components from the clone, so it’s purely visual.
        /// </summary>
        private void DestroyComponentsOnClone(Transform clone)
        {
            // If you have special scripts that move colliders around,
            // call them to revert any changes, so they don't hang around on the clone
            var movedColliders = clone.GetComponentsInChildren<IReturnMovedColliders>(true);
            foreach (var t in movedColliders)
                t.ReturnMovedColliders();

            // Destroy common components that should not exist on the visual-only clone
            var monoBehaviors = clone.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var t in monoBehaviors)
                Destroy(t);

            var rigidBodies = clone.GetComponentsInChildren<Rigidbody>(true);
            foreach (var t in rigidBodies)
                Destroy(t);

            var colliders = clone.GetComponentsInChildren<Collider>(true);
            foreach (var t in colliders)
                Destroy(t);

            var lights = clone.GetComponentsInChildren<Light>(true);
            foreach (var t in lights)
                Destroy(t);
        }

        private void SetNewItemModel()
        {
            if (!currentSlotItem)
                return;

            if (!itemSlotMeshClone)
                SetupNewMeshClone(currentSlotItem);
            else
                animateItemToSlotCoroutine = StartCoroutine(AnimateItemToSlot());
        }

        private IEnumerator AnimateItemToSlot()
        {
            Vector3 goalScale = goalSizeToFitInSlot;
            float timer = 0f;

            boundCenterTransform.localPosition = startingTransformFromHand.position;
            boundCenterTransform.localScale = startingTransformFromHand.scale;
            boundCenterTransform.localRotation = startingTransformFromHand.rotation;
            boundCenterTransform.gameObject.SetActive(true);

            while (timer < AnimationLengthItemToSlot + Time.deltaTime)
            {
                float t = timer / AnimationLengthItemToSlot;
                boundCenterTransform.localPosition = Vector3.Lerp(
                    boundCenterTransform.localPosition,
                    Vector3.zero,
                    t
                );
                boundCenterTransform.localScale = Vector3.Lerp(
                    boundCenterTransform.localScale,
                    goalScale,
                    t
                );
                boundCenterTransform.localRotation = Quaternion.Lerp(
                    boundCenterTransform.localRotation,
                    Quaternion.Euler(new Vector3(0, 90, 0)),
                    t
                );

                yield return null;
                timer += Time.deltaTime;
            }

            // Attach the item model holder to the rotating background
            itemModelHolder.SetParent(backImagesThatRotate);
        }

        private Bounds GetBoundsOfAllMeshes(Transform item)
        {
            Bounds bounds = new Bounds();
            Renderer[] rends = item.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer rend in rends)
            {
                if (rend.GetComponent<ParticleSystem>()) 
                    continue;

                if (bounds.extents == Vector3.zero)
                {
                    bounds = rend.bounds;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }

            return bounds;
        }

        private void OnDrawGizmos()
        {
            if (!itemSlotMeshClone)
                return;

            Bounds tempBounds = GetBoundsOfAllMeshes(itemSlotMeshClone);
            Gizmos.DrawWireCube(tempBounds.center, tempBounds.size);
            Gizmos.DrawSphere(tempBounds.center, 0.01f);
        }

        /// <summary>
        /// Constructor so you can pass in a TransformStruct if needed.
        /// </summary>
        public InventorySlot(TransformStruct startingTransformFromHand)
        {
            this.startingTransformFromHand = startingTransformFromHand;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Example usage if your "controller" object is an ActionBasedController
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

    /// <summary>
    /// Simple extension to help unify or grow bounds from multiple sources.
    /// </summary>
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
