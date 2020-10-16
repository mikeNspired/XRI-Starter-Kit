using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class SecondaryGrabFollow : MonoBehaviour
    {
        [SerializeField] private XRGrabInteractable mainInteractable = null;
        [SerializeField] protected XRGrabInteractable interactable = null;

        private XRBaseInteractor currentHand, mainGripHand;
        private XRInteractionManager interactionManager;
        private Quaternion mainInteractableHandAttachTransformStartingRotation;
        private Vector3 mainHandAttachStartingRotation;
        private TransformStruct interactableStartingTransformData;
        private Vector3 mainHandStartingTransform;
        private Transform interactableStartingParent, newHandAttachTransform;

        private void Awake()
        {
            OnValidate();
            interactable.onSelectEnter.AddListener(SetupHandHoldingThis);
            interactable.onSelectExit.AddListener(DisableHandHoldingThis);

            mainInteractable.onSelectEnter.AddListener(SetupMainInteractableHand);
            mainInteractable.onSelectExit.AddListener(DisableMainInteractableHand);

            //Set starting position, rotation, and parent of interactable to return to when released
            interactableStartingTransformData.SetTransformStruct(interactable.transform.localPosition,
                interactable.transform.localRotation, interactable.transform.localScale);
            interactableStartingParent = interactable.transform.parent;

            //Setup a Transform to help with Quaternions. 
            newHandAttachTransform = new GameObject().transform;
            newHandAttachTransform.name = mainInteractable.gameObject.name + "Follow Hand Helper";
            newHandAttachTransform.parent = transform;
        }

        private void OnValidate()
        {
            if (!interactable)
                interactable = GetComponent<XRGrabInteractable>();
            if (!interactionManager)
                interactionManager = FindObjectOfType<XRInteractionManager>();
        }


        private void SetupHandHoldingThis(XRBaseInteractor hand)
        {
            currentHand = hand;
            SetStartingFromToRotation();
            mainHandStartingTransform = mainGripHand.transform.localEulerAngles;
            mainHandAttachStartingRotation = mainGripHand.attachTransform.localEulerAngles;

        }

        private void DisableHandHoldingThis(XRBaseInteractor hand)
        {
            currentHand = null;

            //Return interactable to original position
            var interactableTransform = interactable.transform;
            interactableTransform.parent = interactableStartingParent;
            interactableTransform.localPosition = interactableStartingTransformData.position;
            interactableTransform.localRotation = interactableStartingTransformData.rotation;
            interactableTransform.localScale = interactableStartingTransformData.scale;

            ResetMainHandAttachTransform();
        }

        private void SetupMainInteractableHand(XRBaseInteractor hand)
        {
            mainGripHand = hand;
            mainInteractableHandAttachTransformStartingRotation = mainGripHand.attachTransform.localRotation;
        }

        private void DisableMainInteractableHand(XRBaseInteractor hand)
        {
            if (currentHand) //Release if main hand lets go{
            {
                interactionManager.SelectExit_public(currentHand, interactable);
            }
            ResetMainHandAttachTransform();
            mainGripHand = null;

        }

        private void ResetMainHandAttachTransform()
        {
            if (!mainGripHand) return;
            mainGripHand.attachTransform.parent = mainGripHand.transform;
            mainGripHand.attachTransform.localRotation = mainInteractableHandAttachTransformStartingRotation;
            newHandAttachTransform.parent = transform;
        }

        private void Update()
        {
            if (currentHand && mainGripHand)
                SetRotation();
        }


        private void SetStartingFromToRotation()
        {
            newHandAttachTransform.position = mainGripHand.attachTransform.position;
            newHandAttachTransform.LookAt(currentHand.transform);
            newHandAttachTransform.parent = mainGripHand.attachTransform.parent;
            mainGripHand.attachTransform.parent = newHandAttachTransform;
        }

        [SerializeField] private bool rotateOnY = false, rotateOnZ = false;

        private void SetRotation()
        {
            newHandAttachTransform.LookAt(currentHand.transform.position);

            var r = mainHandAttachStartingRotation;

            if (rotateOnZ)
            {
                var rToAdd = mainGripHand.transform.localEulerAngles.z - mainHandStartingTransform.z;
                //mainGripHand.attachTransform.localEulerAngles = new Vector3(r.x, r.y, r.z + rToAdd);
                mainGripHand.attachTransform.localEulerAngles = r;
                mainGripHand.attachTransform.Rotate(mainGripHand.attachTransform.forward, rToAdd,Space.World);
            }
            else if (rotateOnY)
            {
                var rToAdd = mainGripHand.transform.localEulerAngles.y - mainHandStartingTransform.y;
                //mainGripHand.attachTransform.localEulerAngles = new Vector3(r.x, r.y + rToAdd, r.z);
                mainGripHand.attachTransform.localEulerAngles = r;
                mainGripHand.attachTransform.Rotate(mainGripHand.attachTransform.up, rToAdd,Space.World);
            }
        }
    }
}