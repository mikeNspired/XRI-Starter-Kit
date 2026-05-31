using XR.Interaction.Toolkit.Samples;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static Unity.Mathematics.math;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// An interactable that follows the position of the interactor on a single axis
    /// </summary>
    public class XRSlider : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
    {
        [SerializeField]
        [Tooltip("The object that is visually grabbed and manipulated")]
        Transform m_Handle = null;

        [SerializeField]
        [Tooltip("The default behaviour uses the attach transform")]
        bool m_UseControllerForPosition = true;
        
        [SerializeField]
        [Tooltip("The value of the slider")]
        [Range(0.0f, 1.0f)]
        float m_Value = 0.5f;

        [SerializeField]
        [Tooltip("The offset of the slider at value '1'")]
        float m_MaxPosition = 0.5f;

        [SerializeField]
        [Tooltip("The offset of the slider at value '0'")]
        float m_MinPosition = -0.5f;

        [SerializeField]
        [Tooltip("Events to trigger when the slider is moved")]
        UnityEventFloat m_OnValueChange = new UnityEventFloat();

        [SerializeField]
        [Tooltip("Remap sliders min value of 0 to a new value")]
        float m_RemapValueMin = 0f;
        [SerializeField]
        [Tooltip("Remap sliders max value of 1 to a new value")]
        float m_RemapValueMax = 1f;
        
        IXRSelectInteractor m_Interactor;
        ControllerInputActionManager m_Controller;

        /// <summary>
        /// The value of the slider
        /// </summary>
        public float Value
        {
            get { return m_Value; }
            set
            {
                SetValue(value);
                SetSliderPosition(value);
            }
        }

        /// <summary>
        /// Events to trigger when the slider is moved
        /// </summary>
        public UnityEventFloat OnValueChange => m_OnValueChange;

        
        void Start()
        {
            SetValue(m_Value);
            SetSliderPosition(m_Value);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(StartGrab);
            selectExited.AddListener(EndGrab);
        }

        protected override void OnDisable()
        {
            selectEntered.RemoveListener(StartGrab);
            selectExited.RemoveListener(EndGrab);
            base.OnDisable();
        }

        void StartGrab(SelectEnterEventArgs args)
        {
            m_Interactor = args.interactorObject;
            m_Controller = m_Interactor.transform.GetComponentInParent<ControllerInputActionManager>();

            UpdateSliderPosition();
        }

        void EndGrab(SelectExitEventArgs args)
        {
            m_Interactor = null;
            m_Controller = null;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (isSelected)
                {
                    UpdateSliderPosition();
                }
            }
        }

        void UpdateSliderPosition()
        {
            // Put anchor position into slider space

            Vector3 position;
            position = m_UseControllerForPosition ? m_Controller.transform.position : m_Interactor.GetAttachTransform(this).position;
            
            var localPosition = transform.InverseTransformPoint(position);
            var sliderValue = Mathf.Clamp01((localPosition.z - m_MinPosition) / (m_MaxPosition - m_MinPosition));
            SetValue(sliderValue);
            SetSliderPosition(sliderValue);
        }

        void SetSliderPosition(float value)
        {
            if (m_Handle == null)
                return;

            var handlePos = m_Handle.localPosition;
            handlePos.z = Mathf.Lerp(m_MinPosition, m_MaxPosition, value);
            m_Handle.localPosition = handlePos;
        }

        void SetValue(float value)
        {
            m_Value = value;
            m_OnValueChange?.Invoke(remap(0,1,m_RemapValueMin,m_RemapValueMax,m_Value));
        }

        void OnDrawGizmosSelected()
        {
            var sliderMinPoint = transform.TransformPoint(new Vector3(0.0f, 0.0f, m_MinPosition));
            var sliderMaxPoint = transform.TransformPoint(new Vector3(0.0f, 0.0f, m_MaxPosition));

            Gizmos.color = Color.green;
            Gizmos.DrawLine(sliderMinPoint, sliderMaxPoint);
        }

        void OnValidate()
        {
            SetSliderPosition(m_Value);
        }
    }
}
