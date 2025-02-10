using System.Collections;

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Handles all item logic: spawning a starting item, swapping, disabling in-hand,
    /// and mesh clone creation/scaling. No animation coroutines for UI icons.
    /// </summary>
    public class InventorySlotItemHandler : MonoBehaviour
    {
        [Header("Visual Slot Displays")]
        [SerializeField] private GameObject slotDisplayWhenContainsItem;
        [SerializeField] private GameObject slotDisplayToAddItem;

        [Header("Transforms & Colliders")]
        [SerializeField] private Transform itemModelHolder;
        [SerializeField] private Transform backImagesThatRotate;
        [SerializeField] private BoxCollider inventorySize;

        [Header("Audio")]
        [SerializeField] private AudioSource grabAudio;
        [SerializeField] private AudioSource releaseAudio;

        public GameObject SlotDisplayWhenContainsItem => slotDisplayWhenContainsItem;
        public GameObject SlotDisplayToAddItem => slotDisplayToAddItem;

        public XRBaseInteractable CurrentSlotItem { get; private set; }

        private TransformStruct itemStartingTransform;
        private Transform boundCenterTransform, itemSlotMeshClone;
        private Vector3 goalSizeToFitInSlot;

        public float AnimationLengthItemToSlot = 0.15f;
        private Coroutine animateItemToSlotCoroutine;
        private XRInteractionManager interactionManager;
        private bool isBusy;


        private void OnEnable()
        {
            isBusy = false;
        }

        
        public void Setup(XRBaseInteractable prefab)
        {
            interactionManager = FindFirstObjectByType<XRInteractionManager>();

            if (!boundCenterTransform)
            {
                boundCenterTransform = new GameObject("Bound Center Transform").transform;
                boundCenterTransform.SetParent(itemModelHolder);
            }
            
            // Create a starting slot item if 'prefab' is assigned
            if (prefab)
            {
                CurrentSlotItem = Instantiate(prefab);
                CurrentSlotItem.transform.SetParent(transform);        
                CurrentSlotItem.transform.localPosition = Vector3.zero;
                CurrentSlotItem.transform.localEulerAngles = Vector3.zero;

                SetupNewMeshClone(CurrentSlotItem);
                CurrentSlotItem.gameObject.SetActive(false);
                SnapItemToSlot();
            }
        }
        
        #region Slot Displays
        
        public void SetSlotDisplayInstant()
        {
            if (CurrentSlotItem)
            {
                SlotDisplayWhenContainsItem?.SetActive(true);
                SlotDisplayToAddItem?.SetActive(false);
            }
            else
            {
                SlotDisplayWhenContainsItem?.SetActive(false);
                SlotDisplayToAddItem?.SetActive(true);
            }
        }
        
        private IEnumerator AnimateIcon()
        {
            // Simple example: fade out one icon, fade in the other
            if (CurrentSlotItem) // If has item, show "contains item" display
            {
                slotDisplayWhenContainsItem.gameObject.SetActive(true);
                yield return null; 
                slotDisplayToAddItem.gameObject.SetActive(false);
            }
            else
            {
                slotDisplayToAddItem.gameObject.SetActive(true);
                slotDisplayWhenContainsItem.gameObject.SetActive(false);
            }
            isBusy = false; 
        }
        
        public IEnumerator AnimateMeshModelOpenOrClose(bool toOne, float duration)
        {
            float timer = 0f;
            Vector3 initialScale = toOne ? Vector3.zero : Vector3.one;
            Vector3 targetScale = toOne ? Vector3.one : Vector3.zero;

            while (timer < duration)
            {
                float t = Mathf.Clamp01(timer / duration);
                itemModelHolder.localScale = Vector3.Lerp(initialScale, targetScale, t);

                yield return null;
                timer += Time.deltaTime;
            }
            itemModelHolder.localScale = targetScale;
        }
        
        #endregion


        // ─────────────────────────────────────────────────────────────────
        //  (1) The main entry point to "use" the slot
        // ─────────────────────────────────────────────────────────────────
        
        /// <summary>
        /// Called by your InventorySlot or "grab" script. 
        /// If the hand is holding an item, we store (or swap). 
        /// If the hand is empty, we retrieve from the slot.
        /// </summary>
        public void InteractWithSlot(XRBaseInteractor controller)
        {
            if (!controller || isBusy)
                return;

            isBusy = true;

            // Stop any ongoing animations (e.g., if someone spam-clicks)
            if (animateItemToSlotCoroutine != null)
                StopCoroutine(animateItemToSlotCoroutine);

            var itemInHand = GetItemInHand(controller);

            if (itemInHand)
            {
                // Either place the item in the slot (if empty) or swap (if slot already has item)
                AddItemToSlot(controller);
            }
            else
            {
                // Hand is empty -> retrieve from slot (if there's something in the slot)
                if (CurrentSlotItem)
                    RetrieveItemFromSlot(controller, destroyItemMesh: true);
                else
                    isBusy = false; // No item in slot, do nothing
            }

            StartCoroutine(AnimateIcon());
        }


        // ─────────────────────────────────────────────────────────────────
        //  (2) Adding Item To Slot (including swap)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Takes the item in the user's hand and places it into the slot.
        /// If the slot already has an item, we first move that old item into the user's hand (swap).
        /// </summary>
        private void AddItemToSlot(XRBaseInteractor controller)
        {
            var itemHandIsHolding = GetItemInHand(controller);
            if (!itemHandIsHolding)
            {
                isBusy = false; // nothing in hand
                return;
            }

            // If there's already an item in the slot, let's swap:
            if (CurrentSlotItem != null)
            {
                // Destroy the existing mesh clone for the old slot item
                if (itemSlotMeshClone)
                    Destroy(itemSlotMeshClone.gameObject);

                // Old slot item becomes the new item in the player's hand:
                CurrentSlotItem.gameObject.SetActive(true);
                CurrentSlotItem.transform.SetParent(null);

                // Actually "grab" it next frame
                StartCoroutine(GrabNewItem(controller, CurrentSlotItem));

                // The slot is now effectively empty for a moment
                CurrentSlotItem = null;
            }

            // Now place the new item into the slot
            releaseAudio?.Play();
            ReleaseItemFromHand(controller, itemHandIsHolding);

            // Reparent under the slot
            itemHandIsHolding.transform.SetParent(transform);
            
            // Re-enable its colliders/triggers if needed
            var grabDisable = itemHandIsHolding.GetComponent<OnGrabEnableDisable>();
            grabDisable?.EnableAll();

            // Mark the slot as containing this new item
            CurrentSlotItem = itemHandIsHolding;

            // Disable the real item, build & animate the mesh clone
            SetupNewMeshClone(itemHandIsHolding);
            itemHandIsHolding.gameObject.SetActive(false);
            itemHandIsHolding.transform.localPosition = Vector3.zero;
            itemHandIsHolding.transform.localEulerAngles = Vector3.zero;

            // Animate into place
            animateItemToSlotCoroutine = StartCoroutine(AnimateItemToSlot());
        }


        // ─────────────────────────────────────────────────────────────────
        //  (3) Retrieving / Removing Item From Slot
        // ─────────────────────────────────────────────────────────────────

        private void RetrieveItemFromSlot(XRBaseInteractor controller, bool destroyItemMesh)
        {
            if (!CurrentSlotItem) return;

            // Destroy the mesh clone from the slot if desired
            if (itemSlotMeshClone && destroyItemMesh)
                Destroy(itemSlotMeshClone.gameObject);

            // Enable and remove from slot
            CurrentSlotItem.gameObject.SetActive(true);
            CurrentSlotItem.transform.SetParent(null);

            // Grab it with the hand (delayed by 1 FixedUpdate to avoid rigidbody sync issues)
            StartCoroutine(GrabNewItem(controller, CurrentSlotItem));

            grabAudio?.Play();

            // The slot is now empty
            CurrentSlotItem = null;
        }


        // ─────────────────────────────────────────────────────────────────
        //  (4) Utility: Release in-hand item & forcibly grab
        // ─────────────────────────────────────────────────────────────────

        private static XRBaseInteractable GetItemInHand(XRBaseInteractor controller)
        {
            if (!controller.hasSelection) return null;
            if (controller.interactablesSelected.Count == 0) return null;
            return controller.interactablesSelected[0] as XRBaseInteractable;
        }

        /// <summary>
        /// Forces the hand to "unselect" the item it is currently holding.
        /// </summary>
        private void ReleaseItemFromHand(XRBaseInteractor interactor, XRBaseInteractable interactable)
        {
            if (!interactionManager)
                return;
            interactionManager.SelectExit((IXRSelectInteractor)interactor, interactable);
        }

        /// <summary>
        /// Forces the hand to "select" (grab) the given interactable. 
        /// We wait 1 physics frame to avoid errors with Unity’s XR rig & rigidbodies.
        /// </summary>
        private IEnumerator GrabNewItem(XRBaseInteractor interactor, XRBaseInteractable interactable)
        {
            yield return new WaitForFixedUpdate();
            if (interactionManager)
                interactionManager.SelectEnter((IXRSelectInteractor)interactor, interactable);
        }


        // ─────────────────────────────────────────────────────────────────
        //  (5) Item-to-slot Animation
        // ─────────────────────────────────────────────────────────────────

        private IEnumerator AnimateItemToSlot()
        {
            float timer = 0f;

            while (timer < AnimationLengthItemToSlot + Time.deltaTime)
            {
                float t = timer / AnimationLengthItemToSlot;
                boundCenterTransform.localPosition =
                    Vector3.Lerp(itemStartingTransform.position, Vector3.zero, t);
                boundCenterTransform.localRotation =
                    Quaternion.Lerp(itemStartingTransform.rotation, Quaternion.Euler(0, 90, 0), t);
                boundCenterTransform.localScale =
                    Vector3.Lerp(itemStartingTransform.scale, goalSizeToFitInSlot, t);
                
                yield return null;
                timer += Time.deltaTime;
            }
            isBusy = false;
        }

        private void SnapItemToSlot()
        {
            boundCenterTransform.localPosition = Vector3.zero;
            boundCenterTransform.localScale = goalSizeToFitInSlot;
            boundCenterTransform.localRotation = Quaternion.Euler(0, 90, 0);
        }


        // ─────────────────────────────────────────────────────────────────
        //  (6) Creating & Fitting the Mesh Clone
        // ─────────────────────────────────────────────────────────────────

        private void SetupNewMeshClone(XRBaseInteractable newItem)
        {
            // Destroy old clone if it exists
            if (itemSlotMeshClone)
                Destroy(itemSlotMeshClone.gameObject);

            // Recreate the "bound center" transform
            CreateBoundsCenter();

            // 1) Clone the real item (meshes only)
            itemSlotMeshClone = GameObjectCloner.DuplicateAndStrip(newItem.gameObject).transform;

            // 2) Put clone under itemModelHolder at the real item’s position
            itemSlotMeshClone.SetParent(itemModelHolder);
            itemSlotMeshClone.SetPositionAndRotation(newItem.transform.position, newItem.transform.rotation);

            // 3) Calculate initial bounds
            var bounds = GetBoundsOfAllMeshes(itemSlotMeshClone);

            // 4) Move boundCenterTransform to bounding box center
            boundCenterTransform.position = bounds.center;
            boundCenterTransform.rotation = newItem.transform.rotation;

            // 5) Then parent the clone under the boundCenterTransform
            itemSlotMeshClone.SetParent(boundCenterTransform);

            // 6) Figure out how much to scale to fit into 'inventorySize'
            inventorySize.enabled = true;
            Vector3 parentSize = inventorySize.bounds.size;
            float ratioX = parentSize.x / bounds.size.x;
            float ratioY = parentSize.y / bounds.size.y;
            float ratioZ = parentSize.z / bounds.size.z;
            float scaleRatio = Mathf.Min(ratioX, ratioY, ratioZ);
            scaleRatio = Mathf.Min(scaleRatio, 1f); // Only shrink large items, do not enlarge smaller

            boundCenterTransform.localScale = Vector3.one * scaleRatio;
            inventorySize.enabled = false;

            // 7) Record these as "start" transforms for the Lerp
            itemStartingTransform.SetTransformStruct(
                newItem.transform.position,
                newItem.transform.rotation,
                newItem.transform.lossyScale
            );
            goalSizeToFitInSlot = boundCenterTransform.localScale;
        }

        private void CreateBoundsCenter()
        {
            if (boundCenterTransform)
                Destroy(boundCenterTransform.gameObject);

            boundCenterTransform = new GameObject("Bound Center Transform").transform;
            boundCenterTransform.SetParent(itemModelHolder, false);
            boundCenterTransform.localScale = Vector3.one;
        }

        private static Bounds GetBoundsOfAllMeshes(Transform item)
        {
            Bounds bounds = new Bounds();
            var rends = item.GetComponentsInChildren<Renderer>();
            foreach (var rend in rends)
            {
                // Ignore particles, etc.
                if (rend.GetComponent<ParticleSystem>()) continue;

                if (bounds.extents == Vector3.zero)
                    bounds = rend.bounds;
                else
                    bounds.Encapsulate(rend.bounds);
            }
            return bounds;
        }
    }
}
