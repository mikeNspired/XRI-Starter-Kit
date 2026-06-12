namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Contract between the dynamic-posing drivers (XRHandPoser grab path, HandGraspProbe,
    /// DynamicPoseTester) and any procedural pose solver. LOCKED as of Phase 7 — a future solver
    /// (e.g. a penetration-resolution variant) must drop in behind this interface without any
    /// driver changes, so additions go on <see cref="HandSolveContext"/> as optional fields with
    /// no-effect defaults, never as new interface members or parameter changes.
    ///
    /// Solve rules every implementation must follow:
    /// - Input is a <see cref="HandSolveContext"/>; output is name-keyed joint data for the solved
    ///   fingers only (a finger disabled via <see cref="HandSolveContext.fingerSettings"/> emits no
    ///   joints), or null when the context is invalid.
    /// - The solver may pose the live joints while solving but MUST restore them to their pre-solve
    ///   state before returning (including on early-out) — applying the result is the driver's job.
    /// - Queries are scoped to <see cref="HandSolveContext.targetColliders"/> /
    ///   <see cref="HandSolveContext.targetMask"/>, never the whole scene.
    /// - Kinematic only: overlap/cast queries and transform writes. No physics forces, no
    ///   ArticulationBodies, no ConfigurableJoints, no IK.
    /// - Callers own the returned array; it must not be reused by the solver afterwards.
    /// </summary>
    public interface IHandPoseSolver
    {
        PoseScriptableObject.JointData[] Solve(HandSolveContext _ctx);

        /// Per-finger contact result from the most recent <see cref="Solve"/> call
        /// (thumb=0 … pinky=4). true = the finger stopped on the target; false = no contact.
        /// Used by the graspability gate to decide whether a dynamic grab can hold.
        bool[] LastSolveContacted { get; }

        /// Per-finger solve telemetry from the most recent <see cref="Solve"/>, populated only when the
        /// <see cref="HandSolveContext.collectDebug"/> flag was set. Null otherwise. Visualization only —
        /// the posing pipeline never reads it.
        HandSolveDebug LastSolveDebug { get; }
    }
}
