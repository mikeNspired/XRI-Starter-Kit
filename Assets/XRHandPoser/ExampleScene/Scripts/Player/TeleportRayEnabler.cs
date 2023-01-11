using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class TeleportRayEnabler : MonoBehaviour
    {
        [SerializeField] private XRRayInteractor teleportRayInteractor = null;
        [SerializeField] private InputActionReference teleportActivate = null;
        private XRInteractorLineVisual lineVisual;
        private void Start()
        {
            LogMessages();
            teleportActivate.GetInputAction().performed += context => EnableRay();
            teleportActivate.GetInputAction().canceled += context => DisableRay();
            //lineVisual = teleportRayInteractor.GetComponent<XRInteractorLineVisual>();
        }
        
        private void EnableRay()
        {
            teleportRayInteractor.enabled = true;
            //lineVisual.enabled = true;
        }

        private void DisableRay()
        {
            StartCoroutine(DisableInteractable());
           // lineVisual.enabled = false;
        }

        private IEnumerator DisableInteractable()
        {
            yield return new WaitForSeconds(Time.deltaTime);
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