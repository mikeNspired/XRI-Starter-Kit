using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// How a finger ended up posed by the dynamic solver — used only to colour the debug gizmos.
    /// </summary>
    public enum FingerSolveState
    {
        NoData,      // finger had no joint chain / was not solved
        Contacted,   // a joint stopped on the target
        NoContact,   // finger reached full close without touching anything (curl-sweep solver)
        RelaxedFist, // finger touched nothing and settled into the relaxed rest curl (progressive solver)
    }

    /// <summary>
    /// One sampled joint along a finger, captured at the curl <c>t</c> where it locked. World space.
    /// </summary>
    public struct JointProbe
    {
        public Vector3 position;      // world position of the sampled joint at its locked t
        public float   lockedT;       // 0..1 curl at which this joint locked
        public bool    contacted;     // did this joint's segment stop on the target
        public Vector3 contactPoint;  // nearest point on the target surface (approx; Collider.ClosestPoint)
        public Vector3 contactNormal; // surface -> joint direction (approx; for drawing only)
    }

    /// <summary>Per-finger solve telemetry. Reuses its probe buffer across solves.</summary>
    public class FingerSolveDebug
    {
        public FingerSolveState state;
        public PoseScriptableObject chosenClosed; // closed pose this finger ended up using
        public float probeRadius;                 // contact-test radius, so the gizmo sphere matches
        public int probeCount;                    // number of valid entries in probes
        public JointProbe[] probes = new JointProbe[0];

        public void EnsureCapacity(int count)
        {
            if (probes.Length < count)
                probes = new JointProbe[count];
        }

        public void Clear()
        {
            state = FingerSolveState.NoData;
            chosenClosed = null;
            probeCount = 0;
        }
    }

    /// <summary>
    /// Per-finger solve telemetry the dynamic solvers fill when <see cref="HandSolveContext.collectDebug"/>
    /// is set, so a gizmo drawer can explain — from the Scene view alone — why each finger is straight vs
    /// curled. Pure data with reused buffers; populating it allocates nothing after the first solve.
    /// Visualization only — nothing in the posing pipeline reads it.
    /// </summary>
    public class HandSolveDebug
    {
        public readonly FingerSolveDebug[] fingers = new FingerSolveDebug[5];
        public bool hasData;

        public HandSolveDebug()
        {
            for (int i = 0; i < fingers.Length; i++)
                fingers[i] = new FingerSolveDebug();
        }

        public void BeginSolve()
        {
            hasData = false;
            for (int i = 0; i < fingers.Length; i++)
                fingers[i].Clear();
        }
    }
}
