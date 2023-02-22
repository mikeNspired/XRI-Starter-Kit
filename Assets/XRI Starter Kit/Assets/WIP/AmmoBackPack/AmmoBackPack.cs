using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class AmmoBackPack : MonoBehaviour
    {
        public XRDirectInteractor leftHand = null, rightHand = null;

        [SerializeField] private XRGrabInteractable magazine = null;
        [SerializeField] private XRGrabInteractable magazine2 = null;
        [SerializeField] private GunType gunType1 = null;
        [SerializeField] private GunType gunType2 = null;
        private XRInteractionManager interactionManager;
        private XRSimpleInteractable simpleInteractable;


        private void Start()
        {
            OnValidate();

            simpleInteractable.onActivate.AddListener(CheckControllerGrip);
        }


        private void OnValidate()
        {
            if (!simpleInteractable)
                simpleInteractable = GetComponent<XRSimpleInteractable>();
        }

        private void CheckControllerGrip(XRBaseInteractor controller)
        {
            if (!IsControllerHoldingObject(controller))
                TryGrabAmmo(controller.GetComponent<XRBaseInteractor>());
        }

        private bool IsControllerHoldingObject(XRBaseInteractor controller)
        {
            return controller.GetComponent<XRDirectInteractor>().selectTarget;
        }

        private void TryGrabAmmo(XRBaseInteractor interactor)
        {
            XRBaseInteractor currentInteractor;

            XRBaseInteractor handHoldingWeapon;
            if (interactor == leftHand)
            {
                handHoldingWeapon = rightHand;
                currentInteractor = interactor;
            }
            else
            {
                handHoldingWeapon = leftHand;
                currentInteractor = interactor;
            }

            //Check if hand not interacting with pack is holding weapon
            if (!handHoldingWeapon || !handHoldingWeapon.selectTarget) return;
            if (currentInteractor.selectTarget) return;

            var gunType = handHoldingWeapon.selectTarget.GetComponentInChildren<MagazineAttachPoint>()?.GunType;
            if (!gunType) return;

            XRGrabInteractable newMagazine;
            if (gunType1 == gunType)
                newMagazine = Instantiate(magazine);
            else if (gunType2 == gunType)
                newMagazine = Instantiate(magazine2);
            else newMagazine = Instantiate(magazine2);
            newMagazine.transform.position = currentInteractor.transform.position;
            newMagazine.transform.forward = currentInteractor.transform.forward;
            StartCoroutine(GrabItem(currentInteractor, newMagazine));
        }

        private IEnumerator GrabItem(XRBaseInteractor currentInteractor, XRGrabInteractable newMagazine)
        {
            yield return new WaitForFixedUpdate();
            if (currentInteractor.selectTarget) yield break;
            interactionManager.SelectEnter(currentInteractor, newMagazine);
        }
    }
}