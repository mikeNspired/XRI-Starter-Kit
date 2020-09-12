using System.Reflection;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;


/// <summary>
/// Thankyou a436t4ataf for your UnityXR Faq
/// </summary>
///
namespace MikeNspired.UnityXRHandPoser
{
    public static class XRInteractionManager_PublicSelectMethods
    {
        public static void SelectEnter_public(this XRInteractionManager manager, XRBaseInteractor interactor, XRBaseInteractable interactable)
        {
            MethodInfo unity_SelectEnter = typeof(XRInteractionManager).GetMethod("SelectEnter",
                BindingFlags.NonPublic | BindingFlags.Instance);

            unity_SelectEnter.Invoke(manager, new object[] {interactor, interactable});
        }

        public static void SelectExit_public(this XRInteractionManager manager, XRBaseInteractor interactor, XRBaseInteractable interactable)
        {
            MethodInfo unity_SelectEnter = typeof(XRInteractionManager).GetMethod("SelectExit",
                BindingFlags.NonPublic | BindingFlags.Instance);

            unity_SelectEnter.Invoke(manager, new object[] {interactor, interactable});
        }

        public static void HoverExit_public(this XRInteractionManager manager, XRBaseInteractor interactor, XRBaseInteractable interactable)
        {
            MethodInfo unity_SelectEnter = typeof(XRInteractionManager).GetMethod("HoverExit",
                BindingFlags.NonPublic | BindingFlags.Instance);

            unity_SelectEnter.Invoke(manager, new object[] {interactor, interactable});
        }
    }
    
    
}