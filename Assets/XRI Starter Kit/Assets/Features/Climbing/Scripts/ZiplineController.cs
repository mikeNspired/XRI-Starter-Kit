using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Moves a ClimbGrabPoint along a series of waypoints when grabbed,
    /// then returns it to the first waypoint when released.
    /// </summary>
    public class ZiplineController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The ClimbGrabPoint that moves along the zipline.")]
        [SerializeField] private ClimbGrabPoint climbGrabPoint;

        [Tooltip("The waypoints that define the zipline path.")]
        [SerializeField] private Transform[] ziplineWaypoints;

        [Header("Speeds")]
        [Tooltip("Speed at which the ClimbGrabPoint moves forward.")]
        [SerializeField] private float ziplineSpeed = 5f;

        [Tooltip("Speed at which the ClimbGrabPoint returns when released.")]
        [SerializeField] private float returnSpeed = 2f;

        [Tooltip("Wait time when grabbing to allow hand to fully grab object.")]
        [SerializeField] private float waitTimeOnGrab = 0.5f;
        
        [Tooltip("Looping audio that plays while traversing")]
        [SerializeField] private AudioSource audioSource;

        private int currentWaypointIndex;
        private bool isReturning;

        private void Awake()
        {
            if (!climbGrabPoint)
                climbGrabPoint = GetComponent<ClimbGrabPoint>();

            climbGrabPoint.GrabInteractable.selectEntered.AddListener(OnGrab);
            climbGrabPoint.GrabInteractable.selectExited.AddListener(OnRelease);
        }

        private void Start()
        {
            if (ziplineWaypoints != null && ziplineWaypoints.Length > 0)
            {
                climbGrabPoint.transform.position = ziplineWaypoints[0].position;
                currentWaypointIndex = 0;
            }
        }

        /// <summary>
        /// Called when the zipline is grabbed.
        /// </summary>
        private void OnGrab(SelectEnterEventArgs args)
        {
            isReturning = false;
            StopAllCoroutines();
            audioSource.Stop();

            StartCoroutine(MoveAlongPath(true));
        }

        /// <summary>
        /// Called when the zipline is released.
        /// </summary>
        private void OnRelease(SelectExitEventArgs args)
        {
            if (isReturning) return;

            isReturning = true;
            StopAllCoroutines();
            audioSource.Stop();

            // Ensure return movement starts even when stopped between waypoints
            currentWaypointIndex = FindClosestWaypointIndex();
            StartCoroutine(MoveAlongPath(false));
        }

        
        public void TestGo()
        {
            StopAllCoroutines();
            StartCoroutine(MoveAlongPath(true));
        }

        
        public void TestReturn()
        {
            StopAllCoroutines();
            isReturning = true;
            StartCoroutine(MoveAlongPath(false));
        }

        /// <summary>
        /// Moves the ClimbGrabPoint along the waypoints, either forward or backward.
        /// </summary>
        private IEnumerator MoveAlongPath(bool forward)
        {
            if (ziplineWaypoints == null || ziplineWaypoints.Length < 2)
                yield break;

            int direction = forward ? 1 : -1;
            float speed = forward ? ziplineSpeed : returnSpeed;

            if (forward)
                yield return new WaitForSeconds(waitTimeOnGrab);

            audioSource.Play();
            while (true)
            {
                int nextIndex = Mathf.Clamp(currentWaypointIndex + direction, 0, ziplineWaypoints.Length - 1);
                Vector3 startPos = climbGrabPoint.transform.position;
                Vector3 endPos = ziplineWaypoints[nextIndex].position;
                float distance = Vector3.Distance(startPos, endPos);
                float duration = distance / speed;
                float elapsed = 0f;

                // Smooth movement towards the next waypoint
                while (elapsed < duration)
                {
                    climbGrabPoint.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                    elapsed += Time.fixedDeltaTime;
                    yield return new WaitForFixedUpdate();
                }

                climbGrabPoint.transform.position = endPos;

                // If we reach the last or first waypoint, stop moving
                if ((forward && nextIndex >= ziplineWaypoints.Length - 1) || (!forward && nextIndex <= 0))
                {
                    if (!forward) // If returning, reset index to 0
                        currentWaypointIndex = 0;
                    break;
                }

                currentWaypointIndex = nextIndex;
            }
            audioSource.Stop();

            isReturning = !forward;
        }

        /// <summary>
        /// Finds the closest waypoint to the ClimbGrabPoint's current position.
        /// </summary>
        private int FindClosestWaypointIndex()
        {
            if (ziplineWaypoints == null || ziplineWaypoints.Length == 0)
                return 0;

            int closestIndex = 0;
            float minDistance = float.MaxValue;

            for (int i = 0; i < ziplineWaypoints.Length; i++)
            {
                float distance = Vector3.Distance(climbGrabPoint.transform.position, ziplineWaypoints[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            // Special case: If stuck between waypoints 0 and 1, move towards 1 first
            if (closestIndex == 0 && Vector3.Distance(climbGrabPoint.transform.position, ziplineWaypoints[0].position) > 0.01f)
                return 1;

            return closestIndex;
        }
    }
}
