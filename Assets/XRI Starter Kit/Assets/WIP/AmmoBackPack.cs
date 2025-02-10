using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    public class AmmoBackPack : MonoBehaviour
    {
        public XRDirectInteractor leftHand, rightHand;
        [SerializeField] private XRGrabInteractable magazine, magazine2;
        [SerializeField] private GunType gunType1, gunType2;

        private XRInteractionManager interactionManager;
        private XRSimpleInteractable simpleInteractable;

        private void Start()
        {
            OnValidate();
            simpleInteractable.activated.AddListener(CheckControllerGrip);
        }

        private void OnValidate()
        {
            if (!simpleInteractable)
                simpleInteractable = GetComponent<XRSimpleInteractable>();
        }

        private void CheckControllerGrip(ActivateEventArgs args)
        {
            var controller = args.interactorObject as XRBaseInteractor;
            if (controller == null) return;

            if (!IsControllerHoldingObject(controller))
                TryGrabAmmo(controller);
        }

        private bool IsControllerHoldingObject(XRBaseInteractor controller)
        {
            var directInteractor = controller as XRDirectInteractor;
            return directInteractor != null && directInteractor.interactablesSelected.Count > 0;
        }

        private void TryGrabAmmo(XRBaseInteractor interactor)
        {
            XRBaseInteractor currentInteractor = interactor == leftHand ? interactor : rightHand;
            XRBaseInteractor handHoldingWeapon = interactor == leftHand ? rightHand : leftHand;

            if (handHoldingWeapon == null || handHoldingWeapon.interactablesSelected.Count == 0) return;
            if (currentInteractor.interactablesSelected.Count > 0) return;

            var gunType = handHoldingWeapon.interactablesSelected[0].transform.GetComponentInChildren<MagazineAttachPoint>()?.GunType;
            if (gunType == null) return;

            XRGrabInteractable newMagazine = gunType == gunType1 ? Instantiate(magazine) : Instantiate(magazine2);

            newMagazine.transform.position = currentInteractor.transform.position;
            newMagazine.transform.forward = currentInteractor.transform.forward;
            StartCoroutine(GrabItem(currentInteractor, newMagazine));
        }

        private IEnumerator GrabItem(XRBaseInteractor currentInteractor, XRGrabInteractable newMagazine)
        {
            yield return new WaitForFixedUpdate();
            if (currentInteractor.interactablesSelected.Count > 0) yield break;
            interactionManager.SelectEnter(currentInteractor, (IXRSelectInteractable) newMagazine);
        }
    }
}
