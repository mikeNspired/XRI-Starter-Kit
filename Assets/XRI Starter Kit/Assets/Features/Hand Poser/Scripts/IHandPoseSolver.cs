namespace MikeNspired.XRIStarterKit
{
    public interface IHandPoseSolver
    {
        PoseScriptableObject.JointData[] Solve(HandSolveContext _ctx);

        /// Per-finger contact result from the most recent <see cref="Solve"/> call
        /// (thumb=0 … pinky=4). true = the finger stopped on the target; false = no contact.
        /// Used by the graspability gate to decide whether a dynamic grab can hold.
        bool[] LastSolveContacted { get; }
    }
}
