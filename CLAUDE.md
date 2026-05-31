
---

## Active Work: Dynamic Hand Posing

Standing context for the dynamic hand posing feature. These rules apply to every task touching this work, in addition to everything above.

### What we're building

A procedural fallback for the Hand Poser. Today, grabbing snaps the hand to an authored `PoseScriptableObject`. We're adding: when a player grabs an object **far from its authored grab point**, or grabs an object with **no authored pose**, the fingers curl procedurally until they contact the object's geometry and stop. A later phase extends the same solver to the **free (non-grabbing) hand** so it can rest on surfaces and drape over edges.

### The mechanism (do not deviate without being asked)

Each finger is posed by a single blend value `t` in `[0,1]`: `t = 0` is the **Open pose** (existing `DefaultPose`), `t = 1` is a **Closed/fist pose**, and every joint is the per-joint lerp between them at `t`. The solver's job per finger is to **find the largest `t` before the finger contacts the target**, by sweeping `t` upward and sphere-testing along the finger. This is essentially a per-finger variant of the existing grip blend in `HandAnimator`.

The approach is **kinematic** — `Physics.OverlapSphere`/`SphereCast` queries plus direct transform writes. There are **NO** physics forces, **NO** ArticulationBodies, **NO** ConfigurableJoints, and **NO** IK. This is a deliberate choice for Unity-update stability. Do not introduce any of those.

### Standing rules for this feature

1. **IP hygiene — critical.** This is a published, paid asset. Build only from the specs in this repo and the existing code. **Never** fetch, read, decompile, paraphrase, or reference the source of Auto Hand, Hurricane VR (HVR), VRIF, or any other third-party hand asset. Implement every technique from first principles, in this project's own naming.
2. **Never break the authored-pose path.** Grab near the attach point → authored `PoseScriptableObject` is the default and must stay untouched in its happy path. Dynamic posing is strictly a fallback for off-axis or unauthored grabs.
3. **Solver behind an interface.** All procedural posing goes through `IHandPoseSolver`; drivers depend on the interface, never a concrete solver, so the approach stays swappable.
4. **Physical-presence colliders are a separate track.** `HandPhysicsColliders` / `HandColliderConfig` are an unrelated feature. Do **not** wire them into the posing system. The solver uses its own queries, not those persistent colliders.
5. **One phase per branch/PR.** Work is sequenced in `phase-prompts.md`. Implement only the requested phase; respect its explicit out-of-scope list; do not start the next phase early.
6. **You cannot run Unity.** There is no headless way to verify visual posing or enter play mode. Do **not** claim a change is "tested." In the PR description, restate the phase's acceptance criteria as the steps the human will verify in-editor, and state any assumptions made.

### Working agreement

- Branch per phase: `phase-N-short-name`. Small, focused commits.
- PR description restates the phase's acceptance criteria as a manual test checklist.
- If a task needs a file not named in the phase spec, **stop and ask** first.
- Prefer additive changes (new files/methods) over rewriting existing methods. When editing an existing method, preserve behavior for all current callers.

### Hand Poser members the solver relies on

In `Features/Hand Poser/Scripts/` (all `MikeNspired.XRIStarterKit`):

- `HandAnimator` — owns the joint list and all posing. Relevant members: `currentJoints` (live joint transforms), `RootBone`, `DefaultPose`/`AnimationPose`/`SecondButtonPose`, `animationTimeToNewPose`, `handType`, and the existing fingertip transforms `thumbTopTransform`/`indexTopTransform`/`middleTopTransform`/`ringTopTransform`/`pinkyTopTransform` (**use these as sweep probe anchors — they already exist**). Mirror `BeginNewPoses` / `SetJointPositions` / `AnimateToPoseOverTime` / `SetNewJoint` for new pose application; do not invent a new joint-storage format.
- `PoseScriptableObject` — pose storage; nested `JointData { string jointName; Vector3 localPosition; Quaternion localRotation; }`. Poses match live joints **by name**.
- `HandPoser` — authored-pose setup on grabbables (`leftHandPose`/`rightHandPose`, `leftHandAttach`/`rightHandAttach`, `BeginNewHandPoses`, `Release`).
- `XRHandPoser` — XRI bridge; subscribes to `selectEntered`/`selectExited`, gates on `CheckIfPoseExistForHand`. Grab-time dynamic posing hooks in here.
- `HandReference` — per-hand refs (`Hand`, `LeftRight`, `NearFarInteractor`); its grab handling snaps the XR attach transform. The actual grab pose must be read **before** that snap overrides it.

### Deferred — do NOT build unless explicitly asked

Physics-driven fingers (Articulation/Configurable joints), a `Physics.ComputePenetration` solver, multi-raycast shape classification, and the physical-presence collider feature. Intentionally out of the current plan.
