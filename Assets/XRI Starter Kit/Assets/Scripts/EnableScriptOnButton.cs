using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class EnableScriptOnButton : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour behaviour;
        [SerializeField] private ActionBasedController actionBasedController;
        [SerializeField] private bool useTrigger, useGrip, inverse;

        private void Start()
        {
            OnValidate();
            if (!actionBasedController)
                enabled = false;

            if (useGrip)
            {
                actionBasedController.selectActionValue.reference.GetInputAction().performed += x => Activate(!inverse);
                actionBasedController.selectActionValue.reference.GetInputAction().canceled += x => Activate(inverse);
            }

            if (useTrigger)
            {
                actionBasedController.activateActionValue.reference.GetInputAction().performed += x => Activate(!inverse);
                actionBasedController.activateActionValue.reference.GetInputAction().canceled += x => Activate(inverse);
            }
        }

        private void OnValidate()
        {
            if (!actionBasedController) actionBasedController = GetComponentInParent<ActionBasedController>();
        }

        private void Activate(bool state)
        {
            if (behaviour)
                behaviour.enabled = state;
        }
    }
}