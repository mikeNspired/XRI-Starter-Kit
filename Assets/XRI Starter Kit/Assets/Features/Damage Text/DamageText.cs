using TMPro;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class DamageText : MonoBehaviour
    {
        private Transform playerCamera;
        [SerializeField] private GameObject criticalIcon;
        [SerializeField] private GameObject onBeatIcon;
        [SerializeField] private TextMeshProUGUI textMesh;
        [SerializeField] private float sizeFactor = 5f; // The distance at which the text size is considered default

        private void Start()
        {
            if (Camera.main != null) playerCamera = Camera.main.transform;
        }

        private void Update()
        {
            if (playerCamera == null) return;
            transform.LookAt(playerCamera);
            AdjustScale();
        }

        public void SetText(string text)
        {
            textMesh.text = text;
        }

        private void AdjustScale()
        {
            float distance = Vector3.Distance(transform.position, playerCamera.position);
            float scaleFactor = distance / sizeFactor;
            transform.localScale = Vector3.one * scaleFactor;
        }
    }
}