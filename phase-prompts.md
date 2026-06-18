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

> **Phase 5 outcome (branch `claude/beautiful-euler-RaccO` / `phase-5`).** Snap suppression shipped and
> works: on a DYNAMIC grab the object is held where it was grabbed (interactor attach aligned to the
> object's current attach pose) instead of snapping its authored grip into the palm; the AUTHORED path
> is unchanged. The session also pulled a lot of solver work forward beyond the snap scope — treat all
> of this as provisional and up for a proper dial-in pass in Phase 7:
> - **Pose policy** per object on `XRHandPoser`: `Auto` / `AuthoredOnly` (e.g. a handgun) / `DynamicOnly`
>   (e.g. a cube) / `NoPosing`.
> - **Global + per-object settings**: thresholds, probe radius, step count, samples, grasp rule, and a
>   failed-grasp response (`Drop` / `FallbackToAuthored`) live on the `HandPoserSettings` asset with a
>   per-object override toggle on `XRHandPoser`.
> - **Graspability gate**: a dynamic grab must reach thumb + N fingers or it drops / falls back, so a
>   sphere-cast grab can't leave an object floating.
> - **ProgressiveCurlSolver** (new `IHandPoseSolver`, default on via `useProgressiveSolver`): per-joint
>   base→tip curl with bisection-refined contact; non-gripping fingers settle into a relaxed fist
>   (`noContactCurl`). `CurlSweepSolver` retained as the single-`t`-per-finger fallback. Multi closed-pose
>   selection exists but is largely redundant with the progressive solver.
> - **Grab transition**: dynamic now applies on the grab frame like the authored path (no clench/flail).
>
> **Known-not-good, deferred to Phase 7:** the actual grab *shapes* are still often unnatural (fingers
> overshoot/under-reach, odd straight-vs-curled joints, occasional wild poses). `OpenPose` must be authored
> per hand or the sweep falls back to the relaxed `DefaultPose`. `HoldInPlaceOnGrab` (the Phase 5a isolation
> test component) is still in the repo as a sandbox aid — remove or keep as desired.


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

> **Phase 6 outcome (branch `claude/wonderful-maxwell-fkrrtx`).** The **per-finger solve visualization was
> pulled forward from Phase 7** (built first) so we can see what the solver is doing. A first "free-hand
> touch" attempt revealed a scope misread (see below) and was reframed into three sub-features:
> - **Solve telemetry + debug viz:** `HandSolveDebug` (per-finger state + reused per-joint probe buffer:
>   world position, locked t, contact flag, approx contact point/normal). `IHandPoseSolver` gains
>   `LastSolveDebug`; `HandSolveContext` gains a `collectDebug` opt-in (zero cost when off). Both solvers
>   record it. `HandPoseSolveDebugDrawer` (on the hand) gizmo-draws the last solve on **real** interactions
>   — a sphere per sampled joint, green=contacted / red=no-contact / yellow=relaxed-fist, contact normals,
>   and per-finger labels (locked t + chosen pose). The drawer's `draw` toggle drives collection per-hand
>   (sets `HandAnimator.requestSolveDebug`), OR'd with the global `HandPoserSettings.drawSolveDebug`.
> - **Scope correction:** running the *grab* solver continuously on the free hand makes it try to **grasp
>   everything** in range (a flashlight by the palm gets wrapped) — that is the grasp mechanic, not world
>   reaction. The real Phase 6 goal (a resting hand that only reacts when geometry *touches* a finger and
>   pushes it) needs displacement + penetration resolution (`Physics.ComputePenetration` / IK), which
>   `CLAUDE.md` defers. That true world-reaction is **deferred to its own future script** pending a decision
>   to relax that rule.
> - **Kept as `HandGraspProbe`** (renamed from the misnamed `FreeHandContactDriver`; on the hand,
>   `enableProbe` off by default). Drives the grab solver against nearby world geometry and applies it via
>   the new `HandAnimator.SetJointsImmediate` (no coroutine), with smoothing. Two modes: **AutoGraspTest**
>   (editor-only, continuous — a live dial-in tool for grab settings) and **GripHoldPose** (runtime — solves
>   only while the grip button is held + a non-grabbable object is in reach, e.g. pressing the hand onto a
>   table). Excludes the hand/rig's own colliders so a broad mask can't self-collide; pauses while grabbing.
> - **Known-not-good / deferred:** the true world-reaction pushback (Feature 1); `IHandPoseSolver.Solve`
>   still allocates per call (buffer-reuse pass needed before per-frame use is GC-clean; driver-level buffers
>   already reused); grasp *shapes* are unjudged here (no Unity) and share the Phase 7 dial-in needs.
> - **Solver-quality pass (`ProgressiveCurlSolver`, the default).** Two structural causes of the "claw"
>   grab poses, both kinematic-only fixes: (1) a joint that found no contact snapped straight to a full
>   fist (`t=1`), so a finger resting its knuckle on an object slammed its fingertip closed — now, once a
>   finger has gripped upstream, each further-out no-contact joint continues as a gentle monotonic spiral
>   (`tj[i] = min(1, tj[i-1] + distalFollowCurl)`), wrapping thin objects over a couple joints without
>   hard-fisting past thick ones; (2) the most-distal joint is a leaf whose contact sweep is degenerate
>   (rotating it doesn't move its own pivot, so it could only snap the tip fully open/closed) — now its
>   pivot is tested once to keep the grasp-contact gate honest, and the tip is posed by continuing the
>   finger's curl. New tunable `distalFollowCurl` (HandSolveContext default 0.33; `dynamicDistalFollowCurl`
>   in HandPoserSettings + XRHandPoser override + HandGraspProbe), mirroring `noContactCurl` plumbing.
> - **Two-pass solver redesign (follow-up).** Replaced the base→tip single sweep with: **Pass A — gross
>   close**, curling the whole finger as a unit (every joint shares one `t`) until any segment first
>   contacts, which places a natural uniform curve up to the contact instead of sweeping the base alone
>   and fisting it when the object sits further out (the "base-slam" that over-fisted the knuckle on
>   fingertip-held objects); then **Pass B — distal wrap**, curling each joint past the contact further,
>   one at a time, until its own segment meets the surface, with the gentle follow-spiral retained for
>   distal joints that reach nothing and the leaf-pivot handling retained for the degenerate fingertip.
>   Same `IHandPoseSolver` surface, candidate (fist/pinch) selection, contact gate, and debug telemetry.
>   Remaining honest limit: still no IK, so a uniform gross close can't reach a small fingertip target
>   without some base curl — but it never hard-fists the base now. `CurlSweepSolver` (non-default) and
>   the authored path are untouched.
> - **Fingertip probes + grasp-probe feel (after first in-editor test — "10× better").** The robot hand
>   skeleton has **no tip joint** (last bone is the distal knuckle), so the fingertip pad was unprobed and
>   the leaf stayed degenerate. Added solver-only tip probes: a **"Create Fingertip Probes"** button on the
>   `HandAnimator` inspector extrapolates a `<finger>_TipProbe_Ignore` child past each finger's last joint.
>   The `Ignore` suffix makes `JointUtility.ShouldSkipTransform` and pose-saving skip it (verified the same
>   rule guards `SetBones`, `HandAnimatorEditor.GatherJointData`, and `PoseConverterWindow`); hands without
>   authored probes auto-generate them at runtime. **Detection matches the `TipProbe` marker, NOT any
>   `*Ignore` child** — the physical-presence feature's distal finger colliders (`*_DistalCollider_Ignore`)
>   are also `Ignore` children of the last joint, and an earlier broad match grabbed those capsule colliders
>   instead of creating a probe. The physics feature enumerates `currentJoints` (which excludes `*Ignore`),
>   so it never sees the probe; the two tracks stay decoupled.
> - **Tip probes moved to their own component + codebase cleanup (user review).** The probes "still didn't
>   work" because the first (buggy) button run had already saved the distal colliders into the HandAnimator
>   tip fields, and the fixed button kept any assigned field. Root fix + the user's architectural point
>   (tips are a *dynamic*-posing concern, not authored posing): new **`HandFingertipProbes`** MonoBehaviour
>   (on the hand, optional) owns the 5 probes, with a "Create / Refresh Probes" inspector button, runtime
>   auto-create, and **collider rejection** (an assigned transform with a Collider is warned about and
>   ignored — the exact prior failure). Solvers read `HandSolveContext.tipProbes` (filled by XRHandPoser /
>   HandGraspProbe / DynamicPoseTester via `GetComponent<HandFingertipProbes>()`); HandAnimator lost all tip
>   code, plus dead members `SetFingerCurls` and `AnimateHandTransformLocal` (no callers). `ClosedPoses` was
>   audited and KEPT — it is the wired multi-candidate (fist vs pinch) picker, zero cost when empty.
>   **Scripts folder reorganized** (git mv, GUIDs preserved → scene/prefab refs survive): `Core/` (authored
>   posing + shared types), `Dynamic Posing/` (solvers, drivers, debug, testers), `Physics Colliders/`,
>   `Helpers/` (audio/animation/Note), `UnityXR/` (+ HandReference, which is XRI-coupled), `Editor/`.
>   Usage audit by GUID: every script is referenced except `HoldInPlaceOnGrab` (Phase 5 proof mechanic,
>   superseded by HandReference snap suppression — flagged for deletion, awaiting user) and
>   `DynamicPoseTester` (dev-only; depends on gitignored Odin).
> - **All dynamic config off HandAnimator → one `HandDynamicPoses` component (user review).** HandAnimator
>   is the *authored*-pose component every asset user touches, yet it carried the dynamic-solver inputs
>   (`OpenPose`/`ClosedPose`/`ClosedPoses`) — confusing and forced on people who never use dynamic posing.
>   Renamed `HandFingertipProbes` → **`HandDynamicPoses`** and folded the solver poses in: it now holds
>   `Open`, `Closed`, `Closed Candidates` (renamed from the opaque "ClosedPoses", with a clear tooltip) and
>   the fingertip probes, plus Preview Open/Closed and Create/Refresh Probes buttons. `HandAnimator` lost all
>   three fields (and the editor that drew them); it's back to authored posing only. Consumers
>   (`XRHandPoser`, `HandGraspProbe`, `DynamicPoseTester`) read the poses + tips via
>   `GetComponent<HandDynamicPoses>()`; a hand without the component (or without a Closed pose) is treated as
>   **authored-only** — `IsGrabDynamic`/`ShouldUseDynamic` return false and `DynamicOnly` logs and falls back,
>   so snap suppression and the grab path stay consistent. GUID preserved on the rename, so a hand that
>   already had the component keeps it. **Setup required in-editor:** add `HandDynamicPoses` to each hand,
>   assign Open/Closed (+ candidates), and click Create / Refresh Probes. `HandFingerMap` gains `tips[5]`/`Tip(i)` (no `HandSolveContext` change —
>   tips ride along on `fingerMap`). Both solvers test the leaf→tip segment, so the leaf is now a **real
>   contact sweep** (rotating it moves the tip child) instead of the binary pivot test, and the debug drawer
>   shows a sphere at the actual fingertip. Grab path benefits automatically. **Feel** on `HandGraspProbe`
>   (fixes the spider-claw entry, instant snap-back, and crawl jitter the test surfaced): proximity-weighted
>   blend from idle (reach edge) to full solve (within `fullPoseDistance`) via nearest-surface distance; a
>   rotation/position **deadband** that freezes micro solver noise while passing deliberate motion; and a
>   short **release fade** to idle instead of a snap. Grab start still hard-stops the probe (never fights the
>   grab). The earlier headless-CI/test idea was dropped as impractical (no Unity in this environment).
> - **Hardening pass (post-reframe review):** committed the 4 missing `.cs.meta` files (solver interface/context,
>   CurlSweepSolver, DynamicPoseTester); both solvers now skip `Collider.ClosestPoint` on unsupported colliders
>   (non-convex mesh/terrain — was a per-call Unity error + gap=0 corrupting candidate pick); grab + tester
>   collider gathers filter trigger/disabled colliders; `HandGraspProbe` smooths against its **own last output**
>   instead of the live joints (the grip/trigger value animations write joints every Update and were diluting
>   the solve to ~25% per frame), warns on an empty World Mask, and releases cleanly when not Ready;
>   `useProgressiveSolver` toggles now take effect live (solver was pinned by `??=`); `XRControllerButtons`
>   unsubscribes the same delegate instances it subscribed (was leaking handlers onto the shared InputAction);
>   `HandPoserSettings` auto-create no longer makes a folder named `…asset` and lands in `Assets/Resources`.

---

## Phase 7 — Solver dial-in, object seating, and visual debugging

> This is the dedicated "make it read like a real hand" pass. Phases 2–5 stood the solver up and proved
> the snap suppression; the grab *shapes* are still rough and were intentionally left for here. Expect to
> iterate against many real objects in-editor. The visual debugging foundation (per-finger solve viz on
> real interactions) was **already pulled forward into Phase 6** — extend/refine it here rather than
> rebuilding, and focus this phase on the dial-in, object seating, and interface lock.

```
Implement ONLY Phase 7. Branch: phase-7-polish.

Goal: make dynamic grabs look natural across many objects, and give us the tools to see why.

Build (visual debugging first — it unblocks all the tuning below):
- Per-finger solve visualization that runs on REAL grabs (XRHandPoser), not just DynamicPoseTester.
  Drive it from a debug toggle. For each finger draw, distinctly coloured:
    * contacted vs non-contacted fingers (e.g. green = contacted, red = no contact, yellow = relaxed-fist
      fallback) — right now every sphere is red and unreadable.
    * the probe sphere at each SAMPLED joint (not just the tip), at the t where the joint locked, so we can
      see which segment actually stopped the finger.
    * the chosen closed pose per finger (when multiple) and the final per-joint t (label or colour ramp).
    * the contact point / surface normal that halted each joint.
  Goal: from the Scene view alone, explain why each finger is straight vs curled.

Then the solver dial-in (all behind the existing settings, no new mechanism unless needed):
- Per-finger calibration: reach limits / anti-overshoot so a finger stops ON a surface instead of curling
  through or past it into air (the lower-fingers-hang-below-the-object case); per-finger probe radius /
  bias to fix the pinky/ring under- or over-closing.
- Preferred-angle / relaxed-fist bias: tune the non-gripping-finger rest so it reads as a natural grip
  (started in Phase 5 as noContactCurl); make it a real preferred pose if a single curl value isn't enough.
- Per-finger enable mask: let specific fingers stay on the authored pose while others solve.
- Optional HandJointLimits asset: per-joint min/max clamp, behind a null check, for when the fist pose
  alone isn't a tight enough limit.
- Multi-sample / drape quality so an edge-wrap reads as a drape, not a claw.

Then object seating (reduce unnatural grabs at the source, not just in the fingers):
- Let a grabbed object nudge/orient itself into a more natural spot in the hand before/with the solve
  (small position+rotation settle toward the palm or a nearest-graspable feature), instead of pure
  hold-where-grabbed. Keep it kinematic and additive; must not reintroduce the authored-grip snap.

Finalize: lock IHandPoseSolver / HandSolveContext so a future PenetrationContactSolver could drop in
without touching any driver.

Acceptance (I will verify in Unity): the debug view clearly shows which fingers contacted and why each is
posed as it is; fingers stop on surfaces instead of passing through/under; a finger reaching nothing rests
in a natural relaxed grip; disabling a finger in the mask leaves it authored while the rest solve; grabs
across a handful of varied objects look plausible rather than wild.

Open a PR with the acceptance steps restated.
```

> **Phase 7 outcome (branch `claude/ecstatic-turing-bb2j4x`).** Phase 7 shipped and the branch grew well
> past its original scope into solver dial-in, a full grasp-probe **surface-stick** feature, and an
> inspector overhaul. Treat the grab *shapes* as "good enough to build on" — further per-object calibration
> is now data, not code.
>
> **Solver dial-in (Phase 7 proper):**
> - **Debug viz finished:** per-JOINT contact colouring (green = this joint contacted, grey = idle joint on
>   a gripping finger, red = no-contact finger, yellow = relaxed rest). `HandPoseSolveDebugDrawer` gained
>   draw toggles, a contacted-joints-only filter, every colour configurable WITH alpha, sphere scale, label
>   tint/colour, and a **Label Alpha** — the label text alpha was a real bug (`Handles.Label` ignores
>   `Handles.color`; text colour now comes from a cached `GUIStyle`).
> - **Per-finger calibration** (`PerFingerSolveSettings`: enable mask, probe-radius scale, max-curl
>   anti-overshoot clamp, press bias) on the settings asset + per-object (`XRHandPoser`) / per-probe
>   (`HandGraspProbe`) overrides. A disabled finger keeps the authored pose on the grab path and is exempt
>   from the grasp gate. Plus `samplesPerSegment` for drape quality.
> - **Relaxed rest pose** (`HandDynamicPoses.RelaxedPose`): a no-contact finger takes an authored rest
>   shape, superseding the `noContactCurl` scalar when assigned.
> - **HandJointLimits** was built then **removed** — redundant with the pose-bounded curl + per-finger
>   max-curl. Its real use (geometry pushing fingers around) belongs to the deferred world-pushback feature;
>   the asset is in git history.
> - **Object seating** (`Seat In Palm`, off by default): a dynamic grab settles the object a capped distance
>   toward a palm anchor (`HandDynamicPoses`) before the solve. Position-only; rotation settle deferred
>   (needs shape classification).
> - **Interface lock + allocation pass:** `IHandPoseSolver` / `HandSolveContext` documented as the frozen
>   seam a future IK/penetration solver drops into; both solvers reuse pose-dict caches and snapshot/result
>   buffers (per-frame-safe).
> - **Per-finger candidate hysteresis** (`candidateStickiness`): each finger keeps its chosen Closed
>   candidate unless another fits closer by a margin — kills the frame-to-frame pose flicker with multiple
>   candidates. Default-off for the one-shot grab; on for the continuous probe.
>
> **Consolidation / inspector overhaul:**
> - `HandSolverSettings` — the shared solver-tuning block, defined ONCE and reused on the settings asset
>   (global source of truth), `XRHandPoser` (per-object override) and `HandGraspProbe` (per-probe override);
>   killed the triple-maintained fields.
> - One-line **info boxes** on every dynamic-pose component (`DynamicPoseInfoBox`, with a universal
>   Tools ▸ XRI Starter Kit ▸ Dynamic Pose Info Boxes toggle). Fixed the repeating "Fingertip Probes" header
>   and added a tip-gizmo toggle on `HandDynamicPoses`. `DynamicPoseTester` marked dev-only / removable.
>   `HandPhysicsColliders` got a gizmo toggle, a Default-layer warning, and a de-cluttered inspector.
>
> **HandGraspProbe — grew into a real free-hand feature:**
> - **Latch Grip Hold:** acquire the grasp once on grip-press and hold it (no per-frame chase).
> - **Surface Stick:** the HAND ROOT anchors to the gripped surface (reusing `HandAnimator.MoveHandToTarget`
>   / `ReturnHandToPlayer` per-frame tracking) with a tunable **leash** give, snapping back past a **break
>   distance**. Exposes `StrainNormalized` (0→1 toward the break) and an `onHandSnapBack` UnityEvent.
>   Decision after discussion: rigid-freeze was the safe option but leash + active-poser was chosen because
>   it sets up the eventual fingertip-pinning IK — it feeds that solver the palm motion.
> - **Grasp Validity gate** (`Require Real Grasp` / `Min Grasp Fingers` / `Min Wrap Angle`): a grip only
>   poses/sticks when enough fingers actually WRAP (contact AND curl past the angle from the Open pose), so
>   a hand jammed flat into a surface no longer fakes a grab. The stick exists only while a valid grasp does.
> - `Proximity Ease In` toggle + `Full Pose Distance` clamp/tooltip fix (`>= reachRadius` degenerated),
>   stale-debug-gizmo clear on idle, mode enum reordered (GripHoldPose first), and a conditional **custom
>   editor** (one Grip-Hold-Pose foldout; dead fields grey out so a bool toggle never reflows the layout).
>
> **Deferred / known-not-good:**
> - **Fingertip-pinning IK** (lock the fingertips, let the palm move) — the genuinely-nicer "stuck hand"
>   look, but it needs real IK (deferred by `CLAUDE.md`). The surface-stick leash is deliberately its future
>   input. Confirmed the architecture composes: an IK solver drops in as another `IHandPoseSolver` and the
>   drivers / anchor / latch / gate / viz / probes / authored poses all carry forward unchanged.
> - **Grasp-gate hysteresis:** the stick is tied to per-frame grasp validity, so a wrap hovering at the
>   angle threshold could flicker the anchor. Add a dead-zone / few-frame grace if it shows up in play.
> - **Polish hooks wired but unbuilt:** `StrainNormalized` (hand shakes as it nears the snap) and
>   `onHandSnapBack` (sound on snap) — left as clean seams, and the natural lead-in to Phase 8.

---

## Phase 8 — Grab / contact audio (exploration)

> Exploratory, not a tight spec. `GrabAudioEffect` (`Features/Hand Poser/Scripts/Helpers/`) already exists:
> an `AudioRandomize` subclass that plays a randomized clip on an interactable's `selectEntered`, shipped as
> a preconfigured prefab you drop under any XR grab interactable. This phase explores richer grab/contact
> audio and wires it into the systems this branch built. Kept for "whatever the next phase ends up being" —
> the dynamic-posing work is already long.

```
Explore Phase 8 — grab / contact audio. Branch: phase-8-grab-audio.

Starting point: GrabAudioEffect (Helpers/) — AudioRandomize + selectEntered → Play(), a prefab child of
grab interactables. Keep that simple path working and backward-compatible.

Directions to explore (pick what reads best in-editor; additive, decoupled — audio listens to events, the
posing/solver code never references audio, mirroring the IHandPoseSolver discipline):
- Tie audio to the NEW dynamic events, not just selectEntered:
    * surface-stick onHandSnapBack is already a UnityEvent — a snap-back sound is a one-liner wiring.
    * a grasp-acquire sound when a dynamic grab / grip-hold actually WRAPS (Require Real Grasp passes),
      and a release sound.
- Per-surface / per-material grab + tap sounds (a wood table vs a metal rail should sound different):
  a small ScriptableObject mapping a PhysicMaterial / layer / tag to a clip set, read at grab/contact time.
- Optional soft-contact tick when the free-hand grasp probe first touches a surface — gate it so it does
  not machine-gun while sliding (debounce on the grasp-acquire transition, not every contacting frame).

Out of scope: haptics (could share the same event seams later), music / ambience.
```

> **Phase 8 outcome (branch `phase-8`).** Implemented the note's "soft-contact tick on the free-hand grasp
> probe" direction, scoped to **free-hand contact only** (a grabbed object's `GrabAudioEffect` already owns
> the grab sound, so the hand must not double it):
> - **`HandGraspProbe.onGraspAcquired`** — a `UnityEvent<int>` (wrapped-finger count) fired once on the
>   **grasp-acquire transition** (debounced via a `contactReported` latch, re-armed in `HardStop`; rides the
>   existing `requireRealGrasp` gate, so a hand jammed flat into a surface doesn't fire it). Public for
>   haptics / VFX; sits alongside the existing `onHandSnapBack` seam.
> - **`HandContactAudio`** (new, `: AudioRandomize`) — subscribes to that event **in code** (`AddListener`,
>   no inspector drag-ref), plays a randomized clip with volume scaled by finger count and a re-trigger
>   cooldown. Per the maintainer's note: events are exposed for others, built-in audio is wired in code.
> - **Cleanups:** dropped the Odin dependency from `DynamicPoseTester` (`[ContextMenu]` instead of `[Button]`,
>   so the asset compiles without the paid Sirenix package); removed `HoldInPlaceOnGrab` (superseded Phase 5a
>   proof mechanic).
> - **Deferred (still open from the exploration):** per-surface / per-material clip sets; a release sound;
>   wiring `onHandSnapBack` to a clip; richer grab-event audio.
> - **In-editor setup:** add an `AudioSource` + `HandContactAudio` on the hand (or a child) with contact
>   clips; enable `HandGraspProbe` (`GripHoldPose`) with a World Mask. (No Unity here — human verifies.)

---

## Phase 9 — Physics interactables for the new hands

> Separate track from the hand solver: this is about the **interactable objects**, not the hand. The
> CLAUDE.md "no joints / no physics" rule is a **hand-posing** constraint and does **not** apply here —
> physics joints (Hinge / Configurable) are the right tool for a door, lever, slider, etc. IP hygiene
> still applies: build from first principles, never reference third-party hand/interaction assets.

```
Implement ONLY Phase 9. Branch: phase-9-physics-interactables.

Goal: physics-driven versions of the core interactables so the new dynamic hands can physically
push / move them (Rigidbody + joints), instead of the current transform/interactor-driven behavior.

Targets (build physics variants; keep the existing ones working):
  XR Push Button, Axis Drag Interactable, XR Slider, XR Lever, XR Joystick, XR Knob, Door.

What exists today:
- Official Unity Assets/XRI/Scripts/ holds COPIED official XRI scripts — XRPushButton, XRSlider, XRLever,
  XRKnob, XRGripButton — transform/interactor-driven, NOT physics. Per AGENTS.md these copies must NOT be
  edited in place. Add NEW physics versions alongside them.
- Custom: Interactables/JoystickLever/XRJoystick.cs, Interactables/Door/Door.cs,
  Scripts/Modified VRBeginner Scripts/AxisDragInteractable.cs.

Approach: Rigidbody + HingeJoint / ConfigurableJoint with drive limits/springs; additive, in the project
namespace. Expose UnityEvents for value changes (for others to tie into) without requiring inspector
wiring internally. Likely split across sub-tasks/PRs (one interactable family at a time).

Acceptance (human verifies in Unity): each interactable can be physically actuated by the hand and
responds with believable physics; the existing (non-physics) versions still work.

Out of scope: per sub-task. Open a PR restating acceptance.
```
