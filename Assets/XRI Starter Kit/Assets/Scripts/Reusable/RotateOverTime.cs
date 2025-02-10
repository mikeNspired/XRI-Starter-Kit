using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class RotateOverTime : MonoBehaviour
    {
        [SerializeField] private float speed = 15;
        [SerializeField] private Vector3 direction = Vector3.forward;

        private void Update()
        {
            transform.Rotate(direction, speed * Time.deltaTime, Space.Self);
        }
    }
}