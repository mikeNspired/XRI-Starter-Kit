// Author MikeNspired.

using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Plays a soft contact sound when the FREE hand wraps onto a surface — i.e. when
    /// <see cref="HandGraspProbe"/> acquires a grasp (the empty hand pressing on geometry, e.g. a table).
    /// It deliberately does NOT sound a normal grab: a grabbed <c>XRGrabInteractable</c> already plays its
    /// own grab sound (see <see cref="GrabAudioEffect"/>), so the hand would only double it.
    ///
    /// Reuses <see cref="AudioRandomize"/> for the pitch/volume variation. Volume scales with how many
    /// fingers landed, and a short re-trigger cooldown is a second guard on top of the probe's own
    /// grasp-acquire debounce.
    ///
    /// Wiring: it subscribes to <see cref="HandGraspProbe.OnGraspAcquired"/> in code (no inspector
    /// drag-ref needed). That event is also public, so other systems (haptics / VFX) can tie into the same
    /// moment.
    /// </summary>
    public class HandContactAudio : AudioRandomize
    {
        #region Inspector

        [Header("Hand Contact")]
        [Tooltip("Free-hand grasp source. Auto-found on this object's parents if left empty.")]
        [SerializeField] private HandGraspProbe graspProbe;

        [Tooltip("Volume (0..1, as a fraction of the AudioSource volume) for a single-finger contact. " +
                 "Scales up to full at Fingers For Full Volume.")]
        [SerializeField, Range(0f, 1f)] private float minVolume = 0.4f;

        [Tooltip("Number of contacting fingers at which the contact plays at full volume.")]
        [SerializeField, Range(1, 5)] private int fingersForFullVolume = 4;

        [Tooltip("Shortest time (s) between contact sounds — a second guard on top of the probe's own " +
                 "grasp-acquire debounce.")]
        [SerializeField] private float minRetriggerInterval = 0.1f;

        #endregion

        #region Private Fields

        private float lastPlayTime = -999f;

        #endregion

        #region Unity Lifecycle

        // Start (not Awake): the base AudioRandomize.Awake does its audio setup first, and the probe is
        // guaranteed constructed by the time Start runs.
        private void Start()
        {
            if (!graspProbe) graspProbe = GetComponentInParent<HandGraspProbe>();

            if (graspProbe)
                graspProbe.OnGraspAcquired.AddListener(OnGraspAcquired);
            else
                Debug.Log("[HandContactAudio] No HandGraspProbe found in parents on : " + gameObject.name +
                          " — free-hand contact audio will not play.", this);
        }

        private void OnDestroy()
        {
            if (graspProbe)
                graspProbe.OnGraspAcquired.RemoveListener(OnGraspAcquired);
        }

        protected new void OnValidate()
        {
            base.OnValidate();
            if (!graspProbe) graspProbe = GetComponentInParent<HandGraspProbe>();
        }

        #endregion

        #region Private Methods

        // _fingerCount = number of fingers (1..5) that wrapped the surface.
        private void OnGraspAcquired(int _fingerCount)
        {
            if (Time.time - lastPlayTime < minRetriggerInterval) return;
            lastPlayTime = Time.time;

            float t = Mathf.InverseLerp(1f, Mathf.Max(1, fingersForFullVolume), _fingerCount);
            Play(Mathf.Lerp(minVolume, 1f, t));
        }

        #endregion
    }
}
