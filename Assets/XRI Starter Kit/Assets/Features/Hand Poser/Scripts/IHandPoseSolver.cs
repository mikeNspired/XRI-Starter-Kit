namespace MikeNspired.XRIStarterKit
{
    public interface IHandPoseSolver
    {
        PoseScriptableObject.JointData[] Solve(HandSolveContext _ctx);
    }
}
