using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class SecondaryGrabFollow : MonoBehaviour
    {
        [SerializeField] private Transform mainGrabTransform = null;

        [SerializeField] private XRGrabInteractable mainInteractable = null;
        [SerializeField] protected XRGrabInteractable interactable = null;

        private XRBaseInteractor currentHand;
        private XRBaseInteractor mainGripHand;
        private XRInteractionManager interactionManager;

        private Quaternion mainInteractableHandAttachTransformStartingRotation;
        private TransformStruct interactableStartingTransformData;
        private Transform interactableStartingParent;
        private Quaternion fromToAtStart;

        private void Awake()
        {
            OnValidate();
            interactable.onSelectEnter.AddListener(SetupHandHoldingThis);
            interactable.onSelectExit.AddListener(DisableHandHoldingThis);

            mainInteractable.onSelectEnter.AddListener(SetupMainInteractableHand);
            mainInteractable.onSelectExit.AddListener(DisableMainInteractableHand);

            interactableStartingTransformData.SetTransformStruct(interactable.transform.localPosition,
                interactable.transform.localRotation, interactable.transform.localScale);
            interactableStartingParent = interactable.transform.parent;
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

            if (mainGripHand)
                mainGripHand.attachTransform.localRotation = mainInteractableHandAttachTransformStartingRotation;
        }

        private void SetupMainInteractableHand(XRBaseInteractor hand)
        {
            mainGripHand = hand;
            mainInteractableHandAttachTransformStartingRotation = mainGripHand.attachTransform.localRotation;
            SetStartingFromToRotation();
        }

        private void DisableMainInteractableHand(XRBaseInteractor hand)
        {
            mainGripHand.attachTransform.localRotation = mainInteractableHandAttachTransformStartingRotation;
            mainGripHand = null;

            if (currentHand) //Release if main hand lets go
                interactionManager.SelectExit_public(currentHand, interactable);
        }


        private void Update()
        {
            if (currentHand && mainGripHand)
                SetRotation();
            mainGripHand.attachTransform.LookAt(currentHand.transform);
        }
        
        private void SetStartingFromToRotation()
        {
            Vector3 oldForward = mainGripHand.attachTransform.forward;
            Quaternion oldRotation = mainGripHand.attachTransform.rotation;

            mainGripHand.attachTransform.LookAt(currentHand.transform);
            Vector3 newForward = mainGripHand.attachTransform.forward;

            mainGripHand.attachTransform.rotation = oldRotation;
            fromToAtStart = Quaternion.FromToRotation(oldForward, newForward);
        }

        private void SetRotation()
        {
            mainGripHand.transform.LookAt(currentHand.transform.position);
            mainGripHand.attachTransform.rotation = mainGripHand.attachTransform.rotation * Quaternion.Inverse(fromToAtStart);
        }
    }
}