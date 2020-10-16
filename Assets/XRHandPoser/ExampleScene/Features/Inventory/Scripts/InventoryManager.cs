using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class InventoryManager : MonoBehaviour
    {
        private InventorySlot[] inventorySlots;
        [SerializeField] private InputHelpers.Button activationButton = InputHelpers.Button.MenuButton;
        public XRController leftController = null, rightController = null;
        [SerializeField] private AudioSource enableAudio = null, disableAudio = null;

        [SerializeField] private bool lookAtController = false;
        private bool isActive = false;

        private void Start()
        {
            OnValidate();

            foreach (var itemSlot in inventorySlots)
                itemSlot.StartCoroutine(itemSlot.CreateStartingItemAndDisable());
        }

        private void OnValidate()
        {
            inventorySlots = GetComponentsInChildren<InventorySlot>();
        }

        private void Update()
        {
            if (leftController && rightController)
                CheckController();
        }

        private bool buttonClicked;

        private void CheckController()
        {
            float activationThreshold = .5f;
            bool isRightHand = false;

            leftController.inputDevice.IsPressed(activationButton, out bool isActive, activationThreshold);
            if (!isActive)
            {
                rightController.inputDevice.IsPressed(activationButton, out isActive, activationThreshold);
                isRightHand = true;
            }

            if (isActive && !buttonClicked)
            {
                buttonClicked = true;
                ToggleInventoryAtController(isRightHand);
            }
            else if (!isActive)
                buttonClicked = false;
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
        }

        private void PlayAudio(bool state)
        {
            if (state)
                enableAudio.Play();
            else
                disableAudio.Play();
        }


        private void ToggleInventoryItems(bool state, GameObject hand)
        {
            foreach (var itemSlot in inventorySlots)
            {
                if (!state)
                {
                    itemSlot.DisableSlot();
                }
                else
                {
                    if (itemSlot.gameObject.activeSelf)
                        itemSlot.EnableSlot();
                    itemSlot.gameObject.SetActive(state);
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
            else
                transform.LookAt(Camera.main.transform);
        }

        private void SetPosition(Transform hand)
        {
            var handDirection = hand.transform.forward;
            transform.transform.forward = Vector3.ProjectOnPlane(-handDirection, transform.up);
        }
    }
}