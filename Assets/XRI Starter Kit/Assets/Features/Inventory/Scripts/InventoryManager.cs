using System;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

namespace MikeNspired.XRIStarterKit
{
    public class InventoryManager : MonoBehaviour
    {
        public static event Action<InventorySlot> OnLeftSlotHoverBegan, OnLeftSlotHoverEnded, OnRightSlotHoverBegan, OnRightSlotHoverEnded;

        [SerializeField] private InputActionReference openMenuInputLeftHand, openMenuInputRightHand;
        public SphereInteractionCaster leftController, rightController;

        [SerializeField] private AudioSource enableAudio, disableAudio;

        [Header("Behavior Settings")] [SerializeField]
        private bool lookAtController;

        [SerializeField] private float queryInterval = 0.1f; // Check for closest slot every 0.1s
        [SerializeField] private float interactionRadius = 0.5f;
        [SerializeField] private InventorySlot[] inventorySlots;

        private bool isActive;
        private float nextQueryTime;

        private InventorySlot activeLeftSlot;
        private InventorySlot activeRightSlot;

        public InventorySlot ActiveLeftSlot => activeLeftSlot;
        public InventorySlot ActiveRightSlot => activeRightSlot;

        private void Awake()
        {
            OnValidate();
            openMenuInputLeftHand.GetInputAction().performed += _ => ToggleInventoryAtController(false);
            openMenuInputRightHand.GetInputAction().performed += _ => ToggleInventoryAtController(true);

            foreach (var slot in inventorySlots) slot.gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            if (inventorySlots?.Length == 0)
                inventorySlots = GetComponentsInChildren<InventorySlot>();
        }

        private void OnEnable()
        {
            openMenuInputLeftHand.EnableAction();
            openMenuInputRightHand.EnableAction();
        }

        private void OnDisable()
        {
            openMenuInputLeftHand.DisableAction();
            openMenuInputRightHand.DisableAction();
        }

        private void Update()
        {
            if (!isActive || Time.time < nextQueryTime) return;

            // This checks distance and updates 'activeLeftSlot' / 'activeRightSlot'
            // while also firing OnLeftSlotHoverBegan / OnLeftSlotHoverEnded, etc.
            CheckHandProximity(leftController, ref activeLeftSlot, true);
            CheckHandProximity(rightController, ref activeRightSlot, false);

            nextQueryTime = Time.time + queryInterval;
        }

        private void ToggleInventoryAtController(bool isRightHand)
        {
            if (isRightHand)
                TurnOnInventory(rightController.gameObject);
            else
                TurnOnInventory(leftController.gameObject);
        }

        private void TurnOnInventory(GameObject hand)
        {
            isActive = !isActive;
            ToggleInventoryItems(isActive, hand);
            PlayAudio(isActive);

            // Clear the active slots if turning off
            if (!isActive)
            {
                if (activeLeftSlot)
                {
                    activeLeftSlot.EndControllerHover();
                    // Fire "ended" event for the left slot
                    OnLeftSlotHoverEnded?.Invoke(activeLeftSlot);
                    activeLeftSlot = null;
                }

                if (activeRightSlot)
                {
                    activeRightSlot.EndControllerHover();
                    // Fire "ended" event for the right slot
                    OnRightSlotHoverEnded?.Invoke(activeRightSlot);
                    activeRightSlot = null;
                }
            }
            else
            {
                // Force immediate re-check
                nextQueryTime = Time.time;
            }
        }

        private void PlayAudio(bool state)
        {
            if (state) enableAudio?.Play();
            else disableAudio?.Play();
        }

        private void ToggleInventoryItems(bool state, GameObject hand)
        {
            foreach (var slot in inventorySlots)
            {
                if (!state)
                    slot.DisableSlot();
                else
                {
                    slot.gameObject.SetActive(true);
                    slot.EnableSlot();
                    SetPositionAndRotation(hand);
                }
            }
        }

        private void SetPositionAndRotation(GameObject hand)
        {
            transform.position = hand.transform.position;
            transform.localEulerAngles = Vector3.zero;

            if (lookAtController)
                SetPosition(hand.transform);
            else if (Camera.main)
                transform.LookAt(Camera.main.transform);
        }

        private void SetPosition(Transform hand)
        {
            var handDirection = hand.forward;
            transform.forward = Vector3.ProjectOnPlane(-handDirection, transform.up);
        }

        // 2) Modified 'CheckHandProximity' to fire hover-begin / hover-end events
        private void CheckHandProximity(SphereInteractionCaster caster, ref InventorySlot activeSlot, bool isLeft)
        {
            if (caster == null) return;

            var handPosition = caster.transform.position;
            float closestDistance = float.MaxValue;
            InventorySlot closestSlot = null;

            foreach (var slot in inventorySlots)
            {
                // Skip inactive or unavailable slots
                if (!slot.gameObject.activeInHierarchy) continue;

                float distance = Vector3.Distance(handPosition, slot.transform.position);
                if (distance < interactionRadius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSlot = slot;
                }
            }

            // If the closest slot changed, do the "hover begin/end"
            if (closestSlot != activeSlot)
            {
                // End hover on the previous active slot
                if (activeSlot != null)
                {
                    activeSlot.EndControllerHover();
                    if (isLeft) OnLeftSlotHoverEnded?.Invoke(activeSlot);
                    else OnRightSlotHoverEnded?.Invoke(activeSlot);
                }

                // Assign the new closest slot and begin
                activeSlot = closestSlot;
                if (activeSlot != null)
                {
                    activeSlot.BeginControllerHover();
                    if (isLeft) OnLeftSlotHoverBegan?.Invoke(activeSlot);
                    else OnRightSlotHoverBegan?.Invoke(activeSlot);
                }
            }
            else
            {
                // If the *currently active* slot is no longer active in hierarchy, remove it
                if (activeSlot != null && !activeSlot.gameObject.activeInHierarchy)
                {
                    activeSlot.EndControllerHover();
                    if (isLeft) OnLeftSlotHoverEnded?.Invoke(activeSlot);
                    else OnRightSlotHoverEnded?.Invoke(activeSlot);

                    activeSlot = null;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var slot in inventorySlots)
            {
                if (slot == null) continue;

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(slot.transform.position, interactionRadius);
            }
        }
    }
}