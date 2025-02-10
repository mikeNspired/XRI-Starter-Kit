// Author: MikeNspired
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    public class MatchHandRotationOnSelect : MonoBehaviour
    {
        [SerializeField]
        private XRBaseInteractable interactable;

        [SerializeField]
        private Transform handAttachTransformParent;

        void OnValidate()
        {
            if (!interactable)
                interactable = GetComponent<XRBaseInteractable>();
        }

        void Start()
        {
            OnValidate();
            
            // Listen for a select event, then rotate the hand attach transform
            interactable.selectEntered.AddListener(args =>
            {
                var handRef = args.interactorObject.transform.GetComponentInParent<HandReference>();
                if (handRef?.Hand == null) return;
                SetPosition(handRef.Hand);
            });
        }

        void SetPosition(HandAnimator handAnimator)
        {
            var handDirection = handAnimator.transform.forward;
            handAttachTransformParent.transform.forward =
                Vector3.ProjectOnPlane(handDirection, transform.up);
        }
    }
}