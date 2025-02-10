using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Handles grabbing items from inventory slots, either via button presses
    /// or via "touch" (automatic, event-based) when the controller is near a slot.
    /// </summary>
    public class InventoryGrabInteract : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private InteractionMethod interactionMethod = InteractionMethod.Button;

        [SerializeField]
        private ButtonInteractionMode buttonInteractionMode = ButtonInteractionMode.Pressed;

        [Header("Controller Input")]
        [SerializeField] private InputActionReference leftControllerInput;
        [SerializeField] private InputActionReference rightControllerInput;

        [Header("References")]
        [SerializeField] private InventoryManager inventoryManager;

        // We assume these are "NearFarInteractor" components on each controller
        private NearFarInteractor cachedLeftInteractor, cachedRightInteractor;

        // For sticky triggers:
        private XRBaseInteractor leftStickyInteractor, rightStickyInteractor;
        private bool leftItemLocked, rightItemLocked;

        
        // -------------------------
        //    Unity Methods
        // -------------------------
        
        private void Awake()
        {
            // Ensure we have an InventoryManager reference
            if (!inventoryManager)
                inventoryManager = GetComponentInParent<InventoryManager>();
            if (!inventoryManager)
                Debug.LogWarning("InventoryManager is missing!", this);

            // Warn if input actions are missing
            if (leftControllerInput == null || leftControllerInput.action == null)
                Debug.LogWarning("LeftControllerInput or its action is null.", this);
            if (rightControllerInput == null || rightControllerInput.action == null)
                Debug.LogWarning("RightControllerInput or its action is null.", this);
        }

        private void OnEnable()
        {
            // Cache the XR Interactors (NearFarInteractor) from the inventory manager’s controllers
            if (inventoryManager.leftController != null)
            {
                cachedLeftInteractor = inventoryManager.leftController.GetComponentInChildren<NearFarInteractor>();
            }
            if (inventoryManager.rightController != null)
            {
                cachedRightInteractor = inventoryManager.rightController.GetComponentInChildren<NearFarInteractor>();
            }

            // If we're using the Button method, hook into performed/canceled
            if (interactionMethod == InteractionMethod.Button)
            {
                if (leftControllerInput?.action != null)
                {
                    leftControllerInput.action.performed += OnLeftControllerAction;
                    leftControllerInput.action.canceled += OnLeftControllerAction;
                }

                if (rightControllerInput?.action != null)
                {
                    rightControllerInput.action.performed += OnRightControllerAction;
                    rightControllerInput.action.canceled += OnRightControllerAction;
                }
            }

            // If we're using the Touch method, subscribe to hover begin events from InventoryManager
            if (interactionMethod == InteractionMethod.Touch)
            {
                InventoryManager.OnLeftSlotHoverBegan += OnLeftSlotHoverBegan;
                InventoryManager.OnRightSlotHoverBegan += OnRightSlotHoverBegan;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from button actions
            if (interactionMethod == InteractionMethod.Button)
            {
                if (leftControllerInput?.action != null)
                {
                    leftControllerInput.action.performed -= OnLeftControllerAction;
                    leftControllerInput.action.canceled -= OnLeftControllerAction;
                }

                if (rightControllerInput?.action != null)
                {
                    rightControllerInput.action.performed -= OnRightControllerAction;
                    rightControllerInput.action.canceled -= OnRightControllerAction;
                }
            }

            // Unsubscribe from touch events
            if (interactionMethod == InteractionMethod.Touch)
            {
                InventoryManager.OnLeftSlotHoverBegan -= OnLeftSlotHoverBegan;
                InventoryManager.OnRightSlotHoverBegan -= OnRightSlotHoverBegan;
            }

            // Clean up sticky interactor event listeners
            if (leftStickyInteractor != null)
                leftStickyInteractor.selectExited.RemoveListener(OnLeftSelectExited);
            if (rightStickyInteractor != null)
                rightStickyInteractor.selectExited.RemoveListener(OnRightSelectExited);
        }


        // -------------------------
        //    Button Mode
        // -------------------------

        private void OnLeftControllerAction(InputAction.CallbackContext context)
        {
            ProcessControllerAction(
                context,
                cachedLeftInteractor,
                inventoryManager.ActiveLeftSlot,
                ref leftStickyInteractor,
                ref leftItemLocked,
                OnLeftSelectExited
            );
        }

        private void OnRightControllerAction(InputAction.CallbackContext context)
        {
            ProcessControllerAction(
                context,
                cachedRightInteractor,
                inventoryManager.ActiveRightSlot,
                ref rightStickyInteractor,
                ref rightItemLocked,
                OnRightSelectExited
            );
        }

        private void ProcessControllerAction(
            InputAction.CallbackContext context,
            XRBaseInteractor interactor,
            InventorySlot activeSlot,
            ref XRBaseInteractor stickyInteractor,
            ref bool itemLocked,
            UnityEngine.Events.UnityAction<SelectExitEventArgs> selectExitedHandler)
        {
            if (activeSlot == null || interactor == null) return;
            if (!ShouldInteract(context)) return;

            // Mode: "State" → press to grab, release to "add" or drop
            if (buttonInteractionMode == ButtonInteractionMode.State)
            {
                if (context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Started)
                {
                    if (!itemLocked)
                    {
                        activeSlot.TryInteractWithSlot(interactor);
                        itemLocked = true;
                        stickyInteractor = interactor;
                    }
                }
                else if (context.phase == InputActionPhase.Canceled)
                {
                    if (itemLocked)
                    {
                        activeSlot.TryInteractWithSlot(interactor);
                        itemLocked = false;
                        stickyInteractor = null;
                    }
                }
                return;
            }

            // Other modes (Pressed / Released / PressedAndReleased)
            XRBaseInputInteractor inputInteractor = interactor as XRBaseInputInteractor;
            bool isSticky = inputInteractor != null &&
                            inputInteractor.selectActionTrigger == XRBaseInputInteractor.InputTriggerType.Sticky;

            if (isSticky)
            {
                if (!itemLocked)
                {
                    // First press
                    activeSlot.TryInteractWithSlot(interactor);
                    stickyInteractor = interactor;
                    itemLocked = true;
                    stickyInteractor.selectExited.AddListener(selectExitedHandler);
                }
                else
                {
                    // Already locked
                    if (buttonInteractionMode == ButtonInteractionMode.Pressed ||
                        buttonInteractionMode == ButtonInteractionMode.PressedAndReleased)
                    {
                        activeSlot.TryInteractWithSlot(interactor);
                        itemLocked = false;
                        stickyInteractor.selectExited.RemoveListener(selectExitedHandler);
                        stickyInteractor = null;
                    }
                    else if (buttonInteractionMode == ButtonInteractionMode.Released &&
                             context.phase == InputActionPhase.Canceled)
                    {
                        activeSlot.TryInteractWithSlot(interactor);
                        itemLocked = false;
                        stickyInteractor.selectExited.RemoveListener(selectExitedHandler);
                        stickyInteractor = null;
                    }
                }
            }
            else
            {
                // Non-sticky → just interact immediately
                activeSlot.TryInteractWithSlot(interactor);
            }
        }

        private bool ShouldInteract(InputAction.CallbackContext context)
        {
            switch (buttonInteractionMode)
            {
                case ButtonInteractionMode.Pressed:
                    return (context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Started);
                
                case ButtonInteractionMode.Released:
                    return (context.phase == InputActionPhase.Canceled);
                
                case ButtonInteractionMode.PressedAndReleased:
                    return (context.phase == InputActionPhase.Performed ||
                            context.phase == InputActionPhase.Started ||
                            context.phase == InputActionPhase.Canceled);
                
                case ButtonInteractionMode.State:
                    // "State" logic is handled fully in ProcessControllerAction
                    return true;
                
                default:
                    return false;
            }
        }


        // -------------------------
        //    Touch Mode (Event-Based)
        // -------------------------

        /// <summary>
        /// Called when InventoryManager detects the left hand has begun hovering a new slot.
        /// </summary>
        private void OnLeftSlotHoverBegan(InventorySlot slot)
        {
            if (slot == null) return;
            if (cachedLeftInteractor == null) return;

            // Immediately attempt to interact (grab/add) with that slot
            slot.TryInteractWithSlot(cachedLeftInteractor);
        }

        /// <summary>
        /// Called when InventoryManager detects the right hand has begun hovering a new slot.
        /// </summary>
        private void OnRightSlotHoverBegan(InventorySlot slot)
        {
            if (slot == null) return;
            if (cachedRightInteractor == null) return;

            // Immediately attempt to interact (grab/add) with that slot
            slot.TryInteractWithSlot(cachedRightInteractor);
        }


        // -------------------------
        //    Sticky Interactor: Exited
        // -------------------------
        
        private void OnLeftSelectExited(SelectExitEventArgs args)
        {
            ProcessSelectExited(args, leftStickyInteractor, inventoryManager.ActiveLeftSlot);
        }

        private void OnRightSelectExited(SelectExitEventArgs args)
        {
            ProcessSelectExited(args, rightStickyInteractor, inventoryManager.ActiveRightSlot);
        }

        private void ProcessSelectExited(SelectExitEventArgs args, XRBaseInteractor stickyInteractor, InventorySlot activeSlot)
        {
            if (stickyInteractor == null) return;
            
            // If the exiter is the same as our sticky interactor, let's "re‐add" or finalize interaction
            if ((XRBaseInteractor)args.interactorObject == stickyInteractor && activeSlot != null)
            {
                activeSlot.TryInteractWithSlot(stickyInteractor);
            }
        }
        
        public enum ButtonInteractionMode
        {
            Pressed,
            Released,
            PressedAndReleased,
            State
        }

        public enum InteractionMethod
        {
            Button,
            Touch
        }
    }
}
