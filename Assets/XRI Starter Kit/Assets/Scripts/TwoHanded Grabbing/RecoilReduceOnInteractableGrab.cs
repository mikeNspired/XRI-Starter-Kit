using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


namespace MikeNspired.XRIStarterKit
{
    public class RecoilReduceOnInteractableGrab : MonoBehaviour
    {
        [SerializeField] private ProjectileWeapon projectileWeapon = null;
        [SerializeField] private XRGrabInteractable interactable = null;
        [SerializeField] private float recoilReduction = .6f;
        [SerializeField] private float recoilRotationReduction = .8f;
        private float startingRecoil, startingRotationRecoil;

        private void Start()
        {
            OnValidate();
            startingRecoil = projectileWeapon.recoilAmount;
            startingRotationRecoil = projectileWeapon.recoilRotation;

            if (!interactable) return;
            interactable.selectEntered.AddListener(x => ReduceProjectileWeaponRecoil());
            interactable.selectExited.AddListener(x => ReturnProjectileWeaponRecoil());
        }

        private void OnValidate()
        {
            if (!interactable)
                interactable = GetComponent<XRGrabInteractable>();
        }

        public void ReduceProjectileWeaponRecoil()
        {
            projectileWeapon.recoilAmount *= 1 - recoilReduction;
            projectileWeapon.recoilRotation *= 1 - recoilRotationReduction;
        }

        public void ReturnProjectileWeaponRecoil()
        {
            projectileWeapon.recoilAmount = startingRecoil;
            projectileWeapon.recoilRotation = startingRotationRecoil;
        }
    }
}