// Author MikeNspired.
// Phase 5a — isolated dynamic-attach proof. Standalone test of the "hold the object where it was
// grabbed" mechanic, with NO hand poser / solver involvement. Once Step 5b folds this mechanic
// into the dynamic grab gate (HandReference), this component can stay as a sandbox aid or be
// removed.

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Drop on an <see cref="XRGrabInteractable"/> to hold it exactly where it was grabbed instead
    /// of letting XRI snap its attach point into the hand. On grab it aligns the grabbing
    /// interactor's attach transform to this object's CURRENT attach pose, so XRI's grab target
    /// equals the object's current pose and it does not teleport. Toggle <see cref="holdInPlace"/>
    /// off to watch the default XRI snap for comparison.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class HoldInPlaceOnGrab : MonoBehaviour
    {
        [Tooltip("When true, the grabbed object stays where it was grabbed. When false, the " +
                 "default XRI snap is used (object's attach point jumps to the controller).")]
        [SerializeField] private bool holdInPlace = true;

        [Tooltip("Log the object's world-pose delta across the grab (≈0 when hold-in-place works).")]
        [SerializeField] private bool logDelta = true;

        private XRGrabInteractable interactable;

        // Restore state for the interactor attach we move (mirrors HandReference.ResetAttachTransform).
        private Transform movedAttach;
        private Vector3 attachOriginalLocalPos;
        private Quaternion attachOriginalLocalRot;

        private void Awake()
        {
            interactable = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            interactable.selectEntered.AddListener(OnGrab);
            interactable.selectExited.AddListener(OnRelease);
        }

        private void OnDisable()
        {
            interactable.selectEntered.RemoveListener(OnGrab);
            interactable.selectExited.RemoveListener(OnRelease);
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            if (!holdInPlace) return;

            // The exact transform XRI aligns to the interactor attach (handles custom attachTransform).
            var objectAttach = args.interactableObject.GetAttachTransform(args.interactorObject);
            var interactorAttach = args.interactorObject.GetAttachTransform(args.interactableObject);
            if (!objectAttach || !interactorAttach) return;

            Vector3 beforePos = transform.position;
            Quaternion beforeRot = transform.rotation;

            // Cache so we can restore the interactor attach on release.
            movedAttach = interactorAttach;
            attachOriginalLocalPos = interactorAttach.localPosition;
            attachOriginalLocalRot = interactorAttach.localRotation;

            // Align interactor attach to the object's current attach pose: target == current → no move.
            interactorAttach.SetPositionAndRotation(objectAttach.position, objectAttach.rotation);

            if (logDelta)
            {
                float dPos = Vector3.Distance(beforePos, transform.position);
                float dRot = Quaternion.Angle(beforeRot, transform.rotation);
                Debug.Log($"[HoldInPlaceOnGrab] {name} held — pose delta pos={dPos:F4}m, rot={dRot:F2}° (expect ≈0).");
            }
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            if (!movedAttach) return;

            movedAttach.localPosition = attachOriginalLocalPos;
            movedAttach.localRotation = attachOriginalLocalRot;
            movedAttach = null;
        }
    }
}
