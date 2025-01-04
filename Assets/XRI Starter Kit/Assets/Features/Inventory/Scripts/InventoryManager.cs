using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace MikeNspired.UnityXRHandPoser
{
    public class InventoryManager : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference openMenuInputLeftHand, openMenuInputRightHand;
        private InventorySlot[] inventorySlots;
        public ControllerInputActionManager leftController, rightController;
        [SerializeField] private AudioSource enableAudio, disableAudio;

        [SerializeField] private bool lookAtController;
        private bool isActive;

        private void Start()
        {
            OnValidate();

            foreach (var itemSlot in inventorySlots)
                itemSlot.StartCoroutine(itemSlot.CreateStartingItemAndDisable());
            
            openMenuInputLeftHand.GetInputAction().performed += x => ToggleInventoryAtController(false);
            openMenuInputRightHand.GetInputAction().performed += x => ToggleInventoryAtController(true);
        }

        private void OnValidate()
        {
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
                    itemSlot.gameObject.SetActive(true);
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