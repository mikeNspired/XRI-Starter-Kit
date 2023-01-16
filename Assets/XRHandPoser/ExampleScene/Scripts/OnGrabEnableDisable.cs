// Author MikeNspired. 

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class OnGrabEnableDisable : MonoBehaviour, IReturnMovedColliders
    {
        [SerializeField] private XRGrabInteractable grabInteractable;

        [Tooltip("Transform gets disabled when the interactable is grabbed and enabled when released")] [SerializeField]
        private Transform disableOnGrab = null;

        [Tooltip("Transform is disabled at start, and enabled when the interactable is grabbed, and disabled when released")] [SerializeField]
        private Transform enableOnGrab = null;

        [SerializeField] private bool moveAndDisableAfterFrameOnGrabColliders = true;


        private bool PreventDisableOfCollidersForObjectDisable;
        private Vector3 enableOnGrabStartPosition;
        private Vector3 disableOnGrabStartPosition;

        private void Awake()
        {
            OnValidate();

            grabInteractable.onSelectEntered.AddListener(x => OnGrab());
            grabInteractable.onSelectExited.AddListener(x => OnRelease());

            if (disableOnGrab) disableOnGrabStartPosition = disableOnGrab.transform.localPosition;
            if (enableOnGrab) enableOnGrabStartPosition = enableOnGrab.transform.localPosition;
        }

        private void OnValidate()
        {
            if (!grabInteractable)
                grabInteractable = GetComponent<XRGrabInteractable>();
        }

        private void Start()
        {
            if (disableOnGrab) disableOnGrab.gameObject.SetActive(true);
            if (enableOnGrab) enableOnGrab.gameObject.SetActive(false);
        }

        public void EnableAll()
        {
            StopAllCoroutines();

            if (disableOnGrab)
            {
                disableOnGrab.gameObject.SetActive(true);
                disableOnGrab.transform.localPosition = disableOnGrabStartPosition;
                disableOnGrab.GetComponent<CollidersSetToTrigger>()?.ReturnToDefaultState();
            }

            if (enableOnGrab)
            {
                enableOnGrab.gameObject.SetActive(true);
                enableOnGrab.transform.localPosition = enableOnGrabStartPosition;
                enableOnGrab.GetComponent<CollidersSetToTrigger>()?.ReturnToDefaultState();
            }
        }


        private void OnRelease()
        {
            if (moveAndDisableAfterFrameOnGrabColliders)
            {
                StopAllCoroutines();
                if (disableOnGrab)
                    disableOnGrab.GetComponent<CollidersSetToTrigger>()?.ReturnToDefaultState();
                StartCoroutine(MoveDisableAndReturn(enableOnGrab));
            }
            else if (enableOnGrab)
                enableOnGrab.gameObject.SetActive(false);

            if (disableOnGrab)
                disableOnGrab.gameObject.SetActive(true);
        }

        private void OnGrab()
        {
            if (moveAndDisableAfterFrameOnGrabColliders)
            {
                StopAllCoroutines();
                if (enableOnGrab)
                    enableOnGrab.GetComponent<CollidersSetToTrigger>()?.ReturnToDefaultState();
                StartCoroutine(MoveDisableAndReturn(disableOnGrab));
            }
            else if (disableOnGrab)
                disableOnGrab.gameObject.SetActive(false);

            if (enableOnGrab)
            {
                enableOnGrab.gameObject.SetActive(true);
                enableOnGrab.transform.localPosition = enableOnGrabStartPosition;
            }
        }

        private IEnumerator MoveDisableAndReturn(Transform objectToMove)
        {
            if (!objectToMove) yield break;
            objectToMove.GetComponent<CollidersSetToTrigger>()?.SetAllToTrigger();
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);

            objectToMove.position += Vector3.one * 9999;
            //Lets physics respond to collider disappearing before disabling object physics update needs to run twice
            yield return new WaitForSeconds(Time.fixedDeltaTime * 2);
            objectToMove.gameObject.SetActive(false);
            objectToMove.localPosition = objectToMove == enableOnGrab ? enableOnGrabStartPosition : disableOnGrabStartPosition;

            objectToMove.GetComponent<CollidersSetToTrigger>()?.ReturnToDefaultState();
        }

        public void ReturnMovedColliders()
        {
            StopAllCoroutines();
            if (enableOnGrab)
                enableOnGrab.localPosition = enableOnGrabStartPosition;
            if (disableOnGrab)
                disableOnGrab.localPosition = disableOnGrabStartPosition;
        }
    }
}