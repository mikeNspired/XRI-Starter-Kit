# Phase Prompts — Dynamic Hand Posing

Paste **one block per fresh Claude Code session** (start a new session each phase to keep context clean). `CLAUDE.md` at the repo root already carries the standing rules — these prompts assume it's there and don't repeat it.

Workflow per phase: paste the block → let it implement and open a PR → pull the branch into Unity → test against the acceptance checklist → merge or comment the specific failure for it to iterate.

> Tip: for the heavier phases (2, 4, 5, 6) set the model to Opus 4.7. Phase 5 (dynamic attach) is investigation-heavy — read its handoff note first.

---

## Phase 0 — `SetJointsDirect`

```
Implement ONLY Phase 0. Branch: phase-0-set-joints-direct.

Goal: let HandAnimator animate to an arbitrary computed pose (raw joint data), not just a PoseScriptableObject.

Build:
- Add `public void SetJointsDirect(PoseScriptableObject.JointData[] jointData, float animationTime)` to HandAnimator.
- Mirror BeginNewPoses internally: snapshot current joints as the "old" TransformStruct[], apply jointData onto matching joints by name (mirror SetJointPositions), snapshot as the "new" TransformStruct[], then run the existing AnimateToPoseOverTime coroutine using animationTime as the blend duration.
- Add a MonoBehaviour `DynamicPoseTester` with an inspector button or key that builds a hardcoded partial-curl JointData[] and calls SetJointsDirect.

Constraints: additive only — do not change the behavior of any existing HandAnimator method.

Acceptance (I will verify in Unity): enter play mode, press the test key, the hand smoothly blends to the arbitrary pose over animationTime and holds.

Out of scope: contact detection, finger isolation, XRI wiring. Open a PR with the acceptance steps restated; do not start Phase 1.
```

---

## Phase 1 — Per-finger curl driver

```
Implement ONLY Phase 1. Branch: phase-1-finger-curl.

Goal: pose each finger independently along Open -> Closed by its own t.

Build:
- Add a HandFingerMap (serializable data or small component) grouping joints into five fingers. Derive each finger's chain from its tip transform (thumbTopTransform, etc.) up to the hand root, or down from the finger's first joint. Expose the five chains.
- Add a serialized `ClosedPose : PoseScriptableObject` field on HandAnimator for the fist pose (Open pose = existing DefaultPose).
- Add `SetFingerCurl(int fingerIndex, float t)` (and/or a float[5] overload): for each joint in that finger, set localPosition/localRotation to the per-joint lerp between Open and Closed at t. Reuse the name-matched lookup. Warn clearly if ClosedPose is unset.
- Extend DynamicPoseTester with five sliders driving SetFingerCurl.

Constraints: no contact logic — pure pose math.

Acceptance (I will verify in Unity): in an empty scene each slider 0->1 curls only its finger from open to fist; no finger hyperextends past the fist pose; fingers move independently.

Out of scope: sweeping, colliders, grabbing. Open a PR with the acceptance steps restated; do not start Phase 2.
```

---

## Phase 2 — Curl sweep solver (in isolation)

```
Implement ONLY Phase 2. Branch: phase-2-curl-sweep-solver.

Goal: compute each finger's contact t against a target, with debug gizmos. No XRI, no grabbing.

Build:
- Define `interface IHandPoseSolver { PoseScriptableObject.JointData[] Solve(HandSolveContext ctx); }`. HandSolveContext carries: the HandAnimator, the finger map, the Open and Closed poses, the target colliders (or a LayerMask), step count N, and probe radius.
- Implement `CurlSweepSolver : IHandPoseSolver`:
  - Per finger, sweep t from 0->1 in N steps (default 15).
  - At each step apply the finger curl (Phase 1 logic), then sample contact at the fingertip via Physics.OverlapSphere(tipPos, probeRadius, mask, QueryTriggerInteraction.Ignore).
  - Lock tFinal at the last step BEFORE first contact (optional + pressBias, default 0). If no contact at t=1, tFinal=1.
  - Return the full-hand JointData[] at the per-finger tFinal values.
- Restore the hand to its pre-solve state on early-out.
- Add a DynamicPoseTester trigger: aim the hand at a target, press a key, run the solver against that object's colliders, apply via SetJointsDirect.
- Add OnDrawGizmos: draw the probe sphere at each finger's final position; colour contacted vs uncontacted fingers.

Constraints: filter queries to the target only (pass its colliders or a dedicated mask) — never query the whole scene.

Acceptance (I will verify in Unity): with a cube, a baseball-sized sphere, and a table in the scene, aiming the open hand at each and pressing the key curls the fingers to stop on the surface; gizmos show where each finger stopped; different objects give visibly different stops.

Out of scope: multi-sample per finger, grab integration, free-hand. Open a PR with the acceptance steps restated; do not start Phase 3.
```

---

## Phase 3 — Decision gate (routing only)

```
Implement ONLY Phase 3. Branch: phase-3-decision-gate.

Goal: on a real grab, decide authored-vs-dynamic and LOG the decision — without changing the pose yet.

Build:
- In the grab entry path (extend XRHandPoser / HandReference grab handling), on selectEntered, BEFORE the authored snap is committed, compute:
  - authoredAttachWorld = world pose of leftHandAttach/rightHandAttach for the grabbing hand.
  - actualGrabWorld = the world pose where the interactor actually grabbed (interactor attach transform world pose / closest point on the interactable), captured BEFORE MoveHandToTarget overrides it.
  - offsetPos = distance(actualGrabWorld, authoredAttachWorld); offsetAngle = Quaternion.Angle(...).
- Decision: if no authored pose for this hand (CheckIfPoseExistForHand false) OR offsetPos > positionThreshold OR offsetAngle > rotationThreshold -> DYNAMIC; else AUTHORED. Defaults: positionThreshold 0.05, rotationThreshold 30. Expose on the poser.
- For now, Debug.Log the decision and measured offset. AUTHORED behaves exactly as today; DYNAMIC also falls through to today's behavior (no pose change) — only the log differs.

Constraints: zero change to the actual pose this phase. Only the routing logic and its log.

Acceptance (I will verify in Unity): console reads AUTHORED when grabbing a known object near its attach point, DYNAMIC (with a plausible offset) when grabbing the wrong end of a bat or an unauthored object; the hand still poses exactly as before in all cases.

Out of scope: running the solver, changing any pose. Open a PR with the acceptance steps restated; do not start Phase 4.
```

---

## Phase 4 — Grab-time dynamic pose

```
Implement ONLY Phase 4. Branch: phase-4-grab-dynamic-pose.

Goal: on the DYNAMIC branch from Phase 3, run the Phase 2 solver and apply it.

Build:
- When the gate returns DYNAMIC, gather the grabbed interactable's colliders, build a HandSolveContext, call the IHandPoseSolver, and apply via SetJointsDirect(result, animationTimeToNewPose).
- Scope the solver's queries to the grabbed object only: pass its colliders, or temporarily move them to a dedicated layer (name it yourself) for the solve and restore in a finally block.
- Release returns the hand to default exactly as today.

Constraints: AUTHORED branch untouched. The dynamic solve runs ONCE on grab, not per frame. Restore any temporarily-modified state in a finally.

Acceptance (I will verify in Unity): grabbing a known object near its attach point gives the authored pose identical to before; grabbing a bat at the wrong end wraps the fingers where the hand actually is; grabbing an unauthored baseball cups it; release returns to normal; repeated grabs leave no stuck state and no console errors.

Out of scope: free-hand world touch, multi-sample polish. Open a PR with the acceptance steps restated; do not start Phase 5.
```

---

## Phase 5 — Dynamic attach / snap suppression

> **Handoff from the Phase 3+4 session (branch `claude/loving-sagan-dQxQu`).** Read this before writing any code.
>
> **What's already shipped on that branch:** the decision gate (AUTHORED vs DYNAMIC), the grab-time curl-sweep solver for no-pose / off-axis grabs, and a dedicated `OpenPose` field on `HandAnimator` used as the sweep's t=0 anchor (falls back to `DefaultPose` if unassigned — author one per hand). `SetJointsDirect`, `IHandPoseSolver`, `CurlSweepSolver`, `HandSolveContext` all exist and work.
>
> **The unsolved problem this phase owns:** when a player grabs an *authored* object off-axis (e.g. a rifle by the barrel), the object still snaps so its authored grip lands in the palm, defeating the dynamic solve. Phase 4 left this in deliberately.
>
> **A snap-suppression attempt was already tried and reverted — do NOT just retry it.** The naive approach was: make `XRHandPoser.IsGrabDynamic` public and have `HandReference.OnGrab` early-return (skip its attach write) when the grab is dynamic. It did NOT stop the snap — the rifle still spun to the authored grip. Conclusion: `HandReference` is **not the only** thing setting the grab pose. There is at least one other actor (the `XRGrabInteractable`'s own `attachTransform`, a grab transformer, and/or a script that assigns the attach transform on Awake/OnEnable). The revert is in git history if you want to read the diff (commits around the "Phase 5 — suppress HandReference snap" revert).

```
Implement ONLY Phase 5. Branch: phase-5-dynamic-attach.

Goal: on the DYNAMIC branch, stop the grabbed object from snapping its authored grip into
the palm, so the hand holds the object where it was actually grabbed and the curl solver
wraps the fingers there. The AUTHORED (near-attach) path must be byte-for-byte unchanged.

FIRST, investigate before changing anything (this is the hard part — budget most of the phase here):
- Map EVERY place the grab pose / attach transform is set for a grabbed object: HandReference.OnGrab,
  XRGrabInteractable.attachTransform, any XRBaseGrabTransformer in the project, useDynamicAttach,
  and any script that sets an attach transform in Awake/OnEnable/Start. Search the repo, don't assume.
- Determine, with logging, which actor actually produces the off-axis snap (the reverted attempt proved
  HandReference alone is not it). Confirm the XRI grab pose pipeline order for this project's setup.
- Only then choose the interception point. Prefer the swappable, least-invasive seam; keep the solver
  behind IHandPoseSolver per the standing rules.

Constraints: never break the authored happy path. No physics forces / ArticulationBodies / IK.
Do not wire in the physical-presence collider feature. Additive over rewrites.

Acceptance (human verifies in Unity): grab a gun at its grip -> AUTHORED, snaps to authored pose
exactly as before. Grab the same gun by the barrel -> DYNAMIC, the gun does NOT teleport its handle
into the palm; it stays where grabbed and fingers curl onto the barrel. No-pose object -> DYNAMIC,
no snap. Release / re-grab / play-mode-exit are clean.

Out of scope: free-hand world touch (Phase 6), solver polish / per-finger calibration (Phase 7).
Open a PR with the acceptance steps restated; do not start Phase 6.
```

> **Other known limitations to carry forward (not this phase's job):** the pinky under-closes vs the
> other fingers on dynamic grabs — that's per-finger solver calibration, slated for Phase 7. `OpenPose`
> must be authored per hand or the sweep falls back to the relaxed `DefaultPose`.

---

## Phase 6 — Free-hand world touch

```
Implement ONLY Phase 6. Branch: phase-6-free-hand-touch.

Goal: run the sweep continuously while the hand is NOT grabbing, against world geometry.

Build:
- Add a FreeHandContactDriver component that, each frame the hand is empty, runs the IHandPoseSolver against a configurable world LayerMask, writing per-finger curl directly each frame (no coroutine — set joints immediately, like AnimateToPoseByValue2).
- Resolve each finger independently so fingers past a table edge curl down while fingers on the surface stay flat.
- Document the contact-owner rule: while this driver is active, any persistent finger colliders from the separate physical-presence feature must be disabled or set to triggers. Do NOT wire that feature in — just guard against the conflict.
- Gate behind an enableFreeHandTouch toggle, off by default.

Constraints: bounded per-frame cost (cap fingers x steps x samples, reuse buffers, no per-frame allocations). Must not run while grabbing. Restrict queries to the world mask — never the player body or other hand.

Acceptance (I will verify in Unity): toggle on, open hand, no grab — lowering onto a table rests the fingers on the surface; sliding to the edge curls fingers past the lip down over it while fingers on the table stay flat; toggle off returns to normal.

Out of scope: polish. Open a PR with the acceptance steps restated; do not start Phase 7.
```

---

## Phase 7 — Polish + extension seam

```
Implement ONLY Phase 7. Branch: phase-7-polish.

Goal: make it read like a real hand and lock the extension points.

Build:
- Multi-sample per finger: sample contact at 2-3 points along each finger (not just the tip) so edge-wrap reads as a drape, not a claw.
- Preferred-angle bias: a finger that contacts nothing drifts toward a gentle rest curl (preferredT per finger blended by preferredWeight) instead of fully open/closed.
- Per-finger enable mask: allow specific fingers to stay on the authored pose while others solve.
- Optional HandJointLimits asset: per-joint min/max clamp, behind a null check, for cases where the fist pose alone isn't a tight enough limit.
- Finalize IHandPoseSolver / HandSolveContext so a future PenetrationContactSolver could drop in without touching any driver.

Acceptance (I will verify in Unity): edge drape looks like a hand wrapping a lip; a pinky reaching nothing rests gently rather than clawing; disabling a finger in the mask leaves it on the authored pose while the rest solve.

Open a PR with the acceptance steps restated.
```
