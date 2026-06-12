namespace MikeNspired.XRIStarterKit
{
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
