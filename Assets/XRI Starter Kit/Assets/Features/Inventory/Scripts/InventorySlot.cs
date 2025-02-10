
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(InventorySlotItemHandler))]
    public class InventorySlot : MonoBehaviour
    {
        [Header("Optional Starting Item")]
        [SerializeField] private XRBaseInteractable startingItem;

        [Header("Animation References")]
        [SerializeField] private Animator addItemAnimator;
        [SerializeField] private Animator hasItemAnimator;

        [Header("Animation Timings")]
        [SerializeField] private float animationDisableLength = 0.5f;
        [SerializeField] private float animationDisableScaleTime = 0.75f;

        // Instead of UnityEvent, we use a C# Action that passes the current item
        public Action<XRBaseInteractable> onSlotUpdated;

        private bool isDisabling;
        private bool hasBeenSetup; // Flag to ensure we only run setup once

        // Animator parameter hashes
        private readonly int hoverHash = Animator.StringToHash("OnHover");
        private readonly int enableHash = Animator.StringToHash("Enable");
        private readonly int disableHash = Animator.StringToHash("Disable");
        private readonly int resetHash = Animator.StringToHash("Reset");
        
        // Link to item handler
        private InventorySlotItemHandler itemHandler;

        private void Setup()
        {
            // If animators were not assigned, try pulling from the item handler references
            if (!addItemAnimator && itemHandler.SlotDisplayToAddItem)
                addItemAnimator = itemHandler.SlotDisplayToAddItem.GetComponent<Animator>();
            if (!hasItemAnimator && itemHandler.SlotDisplayWhenContainsItem)
                hasItemAnimator = itemHandler.SlotDisplayWhenContainsItem.GetComponent<Animator>();

            if (!itemHandler)
                itemHandler = GetComponent<InventorySlotItemHandler>();
            
            itemHandler.Setup(startingItem);
            hasBeenSetup = true;
        }
        
        public void TryInteractWithSlot(XRBaseInteractor controller)
        {
            if (isDisabling) return;

            itemHandler.InteractWithSlot(controller);

            // Fire the event with the (possibly updated) item
            onSlotUpdated?.Invoke(itemHandler.CurrentSlotItem);
        }

        // ─────────────────────────────────────────────────────────────────
        //   Animations: Hover, Enable/Disable, Icon
        // ─────────────────────────────────────────────────────────────────

        public void BeginControllerHover()
        {
            addItemAnimator?.SetBool(hoverHash, true);
            hasItemAnimator?.SetBool(hoverHash, true);
        }

        public void EndControllerHover()
        {
            addItemAnimator?.SetBool(hoverHash, false);
            hasItemAnimator?.SetBool(hoverHash, false);
        }

        public void EnableSlot()
        {
            StopAllCoroutines();
            
            gameObject.SetActive(true);

            if (!hasBeenSetup) Setup();
            
            isDisabling = false;

            // Possibly reset animations
            ResetAnimationState(addItemAnimator, true);
            ResetAnimationState(hasItemAnimator, true);

            addItemAnimator?.SetTrigger(enableHash);
            hasItemAnimator?.SetTrigger(enableHash);

            // Animate if slot has item or not
            itemHandler.SetSlotDisplayInstant();
            itemHandler.StartCoroutine(itemHandler.AnimateMeshModelOpenOrClose(true, animationDisableLength));

            // Fire the event with current item
            onSlotUpdated?.Invoke(itemHandler.CurrentSlotItem);
        }

        public void DisableSlot()
        {
            if (!isDisabling)
                StartCoroutine(DisableSlotAfterAnimation());
        }

        private IEnumerator DisableSlotAfterAnimation()
        {
            isDisabling = true;

            ResetAnimationState(addItemAnimator, false);
            ResetAnimationState(hasItemAnimator, false);

            addItemAnimator?.SetTrigger(disableHash);
            hasItemAnimator?.SetTrigger(disableHash);
            
            itemHandler.StartCoroutine(itemHandler.AnimateMeshModelOpenOrClose(false, animationDisableLength));

            float timer = 0f;
            while (timer < animationDisableScaleTime)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            gameObject.SetActive(false);
            isDisabling = false;
        }

        private void ResetAnimationState(Animator anim, bool setToStart)
        {
            if (!anim) return;
            anim.ResetTrigger(enableHash);
            anim.ResetTrigger(disableHash);
            anim.SetBool(hoverHash, false);
            if (setToStart) anim.SetTrigger(resetHash);
        }
    }
}