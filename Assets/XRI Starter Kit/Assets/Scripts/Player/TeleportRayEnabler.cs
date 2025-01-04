using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


namespace MikeNspired.UnityXRHandPoser
{
    public class TeleportRayEnabler : MonoBehaviour
    {
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor teleportRayInteractor;
        [SerializeField] private InputActionReference teleportActivate;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportationProvider;

        private void Start()
        {
            OnValidate();
            LogMessages();
            teleportActivate.GetInputAction().performed += context => EnableRay();
            teleportActivate.GetInputAction().canceled += context => DisableRay();
            teleportRayInteractor.enabled = false;
        }

        private void OnValidate()
        {
            if (!teleportationProvider) teleportationProvider = GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();
        }

        private void EnableRay()
        {
            if (!teleportationProvider.enabled) return;
            teleportRayInteractor.enabled = true;
        }

        //If the ray is not disabled after waiting till next frame, the teleport does not occur
        private void DisableRay() => StartCoroutine(DisableInteractable());

        private IEnumerator DisableInteractable()
        {
            yield return null;
            teleportRayInteractor.enabled = false;
        }
      
        private void LogMessages()
        {
            if (!teleportActivate)
            {
                Debug.Log("TeleportRayEnabler is missing input action");
                enabled = false;
            }

            if (!teleportRayInteractor)
            {
                Debug.Log("TeleportRayEnabler is missing reference to teleportRayInteractor");
                enabled = false;
            }
        }
    }
}