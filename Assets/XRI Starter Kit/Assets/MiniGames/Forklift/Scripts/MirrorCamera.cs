using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class MirrorCamera : MonoBehaviour
    {
        private Camera playerCamera;
        private void Start() => playerCamera = Camera.main;
        private void Update() => transform.forward = Vector3.Reflect(playerCamera.transform.forward, transform.parent.forward);
    }
}