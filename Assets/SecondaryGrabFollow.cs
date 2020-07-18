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
                mainGripHand.attachTransform.LookAt(currentHand.transform);
        }

        //Credit to "VR with Andrew" on youTube for this method 
        private void SetRotation()
        {
            Vector3 target = currentHand.transform.position - mainGrabTransform.position;
            Debug.DrawLine(currentHand.transform.position,target);
            Quaternion lookRotation = Quaternion.LookRotation(target);

            Vector3 gripRotation = Vector3.zero;
            gripRotation.z = mainGripHand.transform.eulerAngles.z;

            Debug.Log(target + " " + gripRotation + " " +  gripRotation.z);

            lookRotation *= Quaternion.Euler(gripRotation);
            mainGripHand.attachTransform.rotation = lookRotation;
        }
        
        
    }
}