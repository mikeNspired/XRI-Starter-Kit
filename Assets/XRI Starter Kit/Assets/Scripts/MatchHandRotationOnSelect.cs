// Author MikeNspired. 

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class MatchHandRotationOnSelect : MonoBehaviour
    {
        public XRBaseInteractable interactable;
        public Transform HandAttachTransformParent;

        private void OnValidate()
        {
            if (!interactable) interactable = GetComponent<XRBaseInteractable>();
        }

        private void Start()
        {
            OnValidate();
            interactable.onSelectEntered.AddListener(x => SetPosition(x.GetComponentInParent<HandReference>()?.Hand));
        }

        private void SetPosition(HandAnimator handAnimator)
        {
            var handDirection = handAnimator.transform.forward;
            HandAttachTransformParent.transform.forward = Vector3.ProjectOnPlane(handDirection, transform.up);
        }
    }
}
