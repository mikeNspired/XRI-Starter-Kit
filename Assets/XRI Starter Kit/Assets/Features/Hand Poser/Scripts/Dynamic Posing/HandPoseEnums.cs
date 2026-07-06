// Author MikeNspired.
// Configuration enums for the dynamic hand posing feature.

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// Per-object policy controlling whether a grab uses the authored pose, the procedural
    /// dynamic solver, or neither. Set on each object's <see cref="XRHandPoser"/>.
    /// </summary>
    public enum HandPosePolicy
    {
        /// Authored pose when grabbed near the grip; dynamic solve as an off-axis fallback.
        Auto,
        /// Always use the authored pose; never solve dynamically (e.g. a handgun in a shooter).
        AuthoredOnly,
        /// No authored pose; always solve dynamically (e.g. a plain cube).
        DynamicOnly,
        /// Do no hand posing at all on this object.
        NoPosing,
    }

    /// <summary>
    /// What happens when a dynamic grasp fails its contact test (not enough fingers reach the
    /// object — e.g. a sphere-cast grab that caught the object while the hand floated past it).
    /// </summary>
    public enum FailedGraspResponse
    {
        /// Force-release the object so it can't be held floating.
        Drop,
        /// Use the authored pose instead if one exists; otherwise drop.
        FallbackToAuthored,
    }
}
