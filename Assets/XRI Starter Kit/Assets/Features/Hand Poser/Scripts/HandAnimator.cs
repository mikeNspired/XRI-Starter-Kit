using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// The main script that animates the hands. Located on the hand model, and as a child of the controller.
    /// Uses poses stored in PoseScriptableObjects, which contain joint data.
    /// </summary>
    public class HandAnimator : MonoBehaviour
    {
        [Tooltip("Draws spheres to see joints")]
        [SerializeField] bool drawHelperSpheres;

        [Tooltip("Left or Right hand, used for determining which attachpoint needed")]
        public LeftRight handType;

        public Pose RootBone;
        public PoseScriptableObject DefaultPose;
        public PoseScriptableObject AnimationPose;
        public PoseScriptableObject SecondButtonPose;

        [Tooltip("Fully-open/splayed pose used as the open end of the per-finger curl sweep (t=0). Kept distinct from DefaultPose (the relaxed idle) so the solver has full finger range and fingertips start clear of the target. Falls back to DefaultPose if left unassigned.")]
        public PoseScriptableObject OpenPose;

        [Tooltip("Fist/grip pose used as the closed end of the per-finger curl sweep (t=1). Assign in the Inspector.")]
        public PoseScriptableObject ClosedPose;

        [Tooltip("Optional extra closed poses (e.g. a precision/pinch shape alongside the fist). The dynamic " +
                 "solver sweeps each finger against every candidate and keeps the one whose fingertip ends " +
                 "closest to the object. ClosedPose is always included as the first candidate.")]
        public List<PoseScriptableObject> ClosedPoses = new List<PoseScriptableObject>();

        [Tooltip("Time hand skeleton animates to next pose")]
        public float animationTimeToNewPose = .1f;

        [Tooltip("Time to move hand to the item being grabbed")]
        public float handMoveToTargetAnimationTime = .1f;

        public float triggerAnimationValue, gripAnimationValue;
        public bool isGrabbingObject;

        // Last dynamic-solve telemetry for this hand, assigned by whoever solved (XRHandPoser grab or
        // HandGraspProbe). Read by HandPoseSolveDebugDrawer for gizmos. Null when not solving / debugging.
        public HandSolveDebug LastSolveDebug;
        // Set by a HandPoseSolveDebugDrawer to ask the solver to record telemetry this hand can visualize.
        // Solve consumers OR this with the global HandPoserSettings.drawSolveDebug switch.
        [System.NonSerialized] public bool requestSolveDebug;

        public List<Transform> currentJoints = new List<Transform>();
        List<Transform> goalPoseJoints = new List<Transform>();

        Vector3 originalPosition;
        Quaternion originalRotation;
        Transform originalParent;

        IEnumerator AnimateToPoseAnimation;
        IEnumerator AnimateByTriggerValue;

        // Store the original default & animation poses to restore later
        PoseScriptableObject originalPose;
        PoseScriptableObject originalAnimationPose;

        public UnityAction<bool> NewPoseStarting = delegate {};

        // Fields for finger slider logic
        public PoseScriptableObject defaultPose, goalPose;
        public Transform thumbTopTransform, indexTopTransform, middleTopTransform, ringTopTransform, pinkyTopTransform;

        [Header("Fingertip Probes (solver only)")]
        [Tooltip("Optional probe transforms placed just past each finger's last joint, used ONLY by the " +
                 "dynamic solver to seat the fingertip on a surface. Needed on skeletons with no tip joint " +
                 "(the last bone is the distal knuckle). Their names MUST end in 'Ignore' so the pose system " +
                 "skips them. Use the 'Create Fingertip Probes' button, or leave empty to auto-generate at runtime.")]
        public Transform thumbTipProbe, indexTipProbe, middleTipProbe, ringTipProbe, pinkyTipProbe;

        public void AnimateToCurrent() => AnimateInstantly(DefaultPose);

        void Awake()
        {
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
            originalParent = transform.parent;

            originalPose = DefaultPose;
            originalAnimationPose = AnimationPose;

            if (!DefaultPose && HandPoserSettings.Instance)
                DefaultPose = HandPoserSettings.Instance.DefaultPose;

            SetBones();
            AnimateInstantly(DefaultPose);
        }

        void OnEnable() => Application.onBeforeRender += OnBeforeRender;
        void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

        public void SetAnimationValue(float val) => triggerAnimationValue = val;
        public void StartAnimationPosing() => StartAnimationByButtonValue(ControllerButtons.Trigger);
        public void StartSecondaryPosing() => StartAnimationByButtonValue(ControllerButtons.Grip);
        public void SetSecondaryValue(float val) => gripAnimationValue = val;

        // Stops the live grip/trigger value-driven animation in place (without changing the current
        // pose), so a system like dynamic posing can take over from the hand's current shape instead
        // of the grip clench continuing to fist the hand for a frame before the solved pose lands.
        public void StopButtonValueAnimation()
        {
            if (AnimateByTriggerValue != null)
            {
                StopCoroutine(AnimateByTriggerValue);
                AnimateByTriggerValue = null;
            }
        }

        public void ReturnToDefaultPosing()
        {
            isGrabbingObject = false;
            BeginNewPoses(DefaultPose, AnimationPose, false);
        }

        [System.Serializable]
        public class HandFingerMap
        {
            public List<Transform> thumb  = new List<Transform>();
            public List<Transform> index  = new List<Transform>();
            public List<Transform> middle = new List<Transform>();
            public List<Transform> ring   = new List<Transform>();
            public List<Transform> pinky  = new List<Transform>();

            public List<Transform> Finger(int i) => i switch
            {
                0 => thumb, 1 => index, 2 => middle, 3 => ring, 4 => pinky, _ => null
            };

            // Optional solver-only fingertip probe per finger (thumb=0 … pinky=4), sitting just past the
            // finger's last joint. Null when the skeleton has a real tip joint or the chain is too short.
            public readonly Transform[] tips = new Transform[5];
            public Transform Tip(int i) => (i >= 0 && i < 5) ? tips[i] : null;
        }

        public HandFingerMap fingerMap = new HandFingerMap();

        public void SetBones()
        {
            currentJoints.Clear();
            JointUtility.GatherTransformsForPose(RootBone.transform, currentJoints);
            BuildFingerMap();
        }

        void BuildFingerMap()
        {
            fingerMap.thumb.Clear();
            fingerMap.index.Clear();
            fingerMap.middle.Clear();
            fingerMap.ring.Clear();
            fingerMap.pinky.Clear();

            BuildFingerChain(thumbTopTransform,  "thumb",  fingerMap.thumb);
            BuildFingerChain(indexTopTransform,  "index",  fingerMap.index);
            BuildFingerChain(middleTopTransform, "middle", fingerMap.middle);
            BuildFingerChain(ringTopTransform,   "ring",   fingerMap.ring);
            BuildFingerChain(pinkyTopTransform,  "pinky",  fingerMap.pinky);

            ResolveFingerTip(0, thumbTipProbe,  fingerMap.thumb,  "Thumb");
            ResolveFingerTip(1, indexTipProbe,  fingerMap.index,  "Index");
            ResolveFingerTip(2, middleTipProbe, fingerMap.middle, "Middle");
            ResolveFingerTip(3, ringTipProbe,   fingerMap.ring,   "Ring");
            ResolveFingerTip(4, pinkyTipProbe,  fingerMap.pinky,  "Pinky");
        }

        /// <summary>
        /// Resolves the solver-only fingertip probe for a finger: an explicitly-assigned field, else an
        /// existing "*Ignore" child of the last joint, else (at runtime only) a freshly created probe
        /// extrapolated past the last bone. Leaves the tip null when the chain is too short to extrapolate.
        /// Edit-time creation is the inspector "Create Fingertip Probes" button — never spawned here.
        /// </summary>
        void ResolveFingerTip(int index, Transform explicitProbe, List<Transform> chain, string fingerName)
        {
            fingerMap.tips[index] = null;
            if (explicitProbe) { fingerMap.tips[index] = explicitProbe; return; }
            if (chain == null || chain.Count == 0) return;

            // Reuse an authored / previously-created tip probe so repeated SetBones() is idempotent.
            var existing = FindTipProbeChild(chain[chain.Count - 1]);
            if (existing) { fingerMap.tips[index] = existing; return; }

            if (!Application.isPlaying) return; // runtime fallback only; no edit-time scene/prefab pollution

            var probe = CreateTipProbe(chain, fingerName);
            if (probe) fingerMap.tips[index] = probe;
        }

        /// Marker substring identifying a solver tip probe. Distinct from the physical-presence finger
        /// colliders, which are ALSO "*Ignore" children of the last joint — matching only this substring
        /// keeps the solver from mistaking a distal capsule collider for the fingertip.
        public const string TipProbeMarker = "TipProbe";

        /// First child of <paramref name="parent"/> that is a solver tip probe (name contains "TipProbe").
        /// Deliberately NOT a generic "*Ignore" match, so persistent finger colliders are never picked up.
        public static Transform FindTipProbeChild(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name.Contains(TipProbeMarker)) return parent.GetChild(i);
            return null;
        }

        /// <summary>
        /// Creates a probe child just past a finger's last joint by extending the last bone segment (0.8×).
        /// The name is "&lt;finger&gt;_TipProbe_Ignore": the "TipProbe" marker distinguishes it from physics
        /// colliders, and the "Ignore" suffix makes <see cref="JointUtility.ShouldSkipTransform"/> and pose
        /// saving skip it. Returns null when the chain can't define a direction (needs ≥2 joints). Shared by
        /// the runtime fallback and the editor button so both produce identical, mutually-recognized probes.
        /// </summary>
        public static Transform CreateTipProbe(List<Transform> chain, string fingerName)
        {
            if (chain == null || chain.Count < 2) return null;
            var last = chain[chain.Count - 1];
            var prev = chain[chain.Count - 2];
            Vector3 dir = last.position - prev.position;
            float len = dir.magnitude;
            if (len < 1e-5f) return null;

            var probe = new GameObject($"{fingerName}_{TipProbeMarker}_Ignore").transform;
            probe.SetParent(last, false);
            probe.position = last.position + dir / len * (len * 0.8f);
            probe.localRotation = Quaternion.identity;
            return probe;
        }

        /// <summary>
        /// Builds a finger's joint chain (the finger's base joint plus all of its
        /// descendants). Prefers the explicitly-assigned base transform (the
        /// "Finger Parent Transform" fields); if none is assigned, falls back to a
        /// name match against the root bone's direct children, so the map still
        /// builds on hands where those fields were never wired up. Walks DOWN the
        /// hierarchy via the same JointUtility used by SetBones / SetPoseByValue.
        /// </summary>
        void BuildFingerChain(Transform assignedBase, string nameKeyword, List<Transform> chain)
        {
            var baseJoint = assignedBase ? assignedBase : FindFingerBaseByName(nameKeyword);
            if (!baseJoint) return;
            JointUtility.GatherTransformsForPose(baseJoint, chain);
        }

        Transform FindFingerBaseByName(string nameKeyword)
        {
            if (!RootBone) return null;
            var root = RootBone.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name.ToLowerInvariant().Contains(nameKeyword))
                    return child;
            }
            return null;
        }

        public void SetPoses(PoseScriptableObject primaryPose, PoseScriptableObject animationPose)
        {
            DefaultPose = primaryPose;
            AnimationPose = animationPose;
        }

        public void BeginNewPoses(PoseScriptableObject primaryPose, PoseScriptableObject animationPose, bool isGrabbing)
        {
            isGrabbingObject = isGrabbing;
            NewPoseStarting.Invoke(isGrabbingObject);

            SetJointPositions(DefaultPose, goalPoseJoints);
            TransformStruct[] oldPose = CopyTransformData(currentJoints);

            AnimationPose = animationPose;
            DefaultPose = primaryPose;

            SetJointPositions(primaryPose, goalPoseJoints);
            TransformStruct[] newPose = CopyTransformData(currentJoints);

            if (AnimateByTriggerValue != null) StopCoroutine(AnimateByTriggerValue);
            if (AnimateToPoseAnimation != null) StopCoroutine(AnimateToPoseAnimation);

            AnimateToPoseAnimation = AnimateToPoseOverTime(oldPose, newPose);
            StartCoroutine(AnimateToPoseAnimation);
        }

        void StartAnimationByButtonValue(ControllerButtons button)
        {
            if (button == ControllerButtons.Grip && isGrabbingObject) return;
            var newPose = (button == ControllerButtons.Trigger) ? AnimationPose :
                          (button == ControllerButtons.Grip) ? SecondButtonPose : null;

            if (!newPose) return;
            if (AnimateByTriggerValue != null) StopCoroutine(AnimateByTriggerValue);

            AnimateByTriggerValue = AnimateToPoseByValue2(newPose, button);
            StartCoroutine(AnimateByTriggerValue);
        }

        protected IEnumerator AnimateToPoseOverTime(TransformStruct[] originalPose, TransformStruct[] newPose)
        {
            float timer = 0;
            while (timer <= animationTimeToNewPose + Time.deltaTime)
            {
                for (int i = 0; i < currentJoints.Count; i++)
                {
                    var joint = currentJoints[i];
                    if (!joint) continue;

                    var pos = Vector3.Lerp(originalPose[i].position, newPose[i].position, timer / animationTimeToNewPose);
                    var rot = Quaternion.Lerp(originalPose[i].rotation, newPose[i].rotation, timer / animationTimeToNewPose);
                    SetNewJoint(ref joint, pos, rot);
                }
                timer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }
        }

        // Overload used by SetJointsDirect so callers can specify an explicit blend duration
        // without touching animationTimeToNewPose (which BeginNewPoses relies on).
        protected IEnumerator AnimateToPoseOverTime(TransformStruct[] originalPose, TransformStruct[] newPose, float _duration)
        {
            if (_duration <= 0f)
            {
                for (int i = 0; i < currentJoints.Count; i++)
                {
                    var joint = currentJoints[i];
                    if (!joint) continue;
                    SetNewJoint(ref joint, newPose[i].position, newPose[i].rotation);
                }
                yield break;
            }

            float timer = 0;
            while (timer <= _duration + Time.deltaTime)
            {
                for (int i = 0; i < currentJoints.Count; i++)
                {
                    var joint = currentJoints[i];
                    if (!joint) continue;

                    var pos = Vector3.Lerp(originalPose[i].position, newPose[i].position, timer / _duration);
                    var rot = Quaternion.Lerp(originalPose[i].rotation, newPose[i].rotation, timer / _duration);
                    SetNewJoint(ref joint, pos, rot);
                }
                timer += Time.deltaTime;
                yield return new WaitForSeconds(Time.deltaTime);
            }
        }

        IEnumerator AnimateToPoseByValue2(PoseScriptableObject newPose, ControllerButtons button)
        {
            SetJointPositions(DefaultPose, goalPoseJoints);
            TransformStruct[] startingPose = CopyTransformData(goalPoseJoints);

            SetJointPositions(newPose, goalPoseJoints);
            yield return null;

            while (true)
            {
                float value = (button == ControllerButtons.Trigger) ? triggerAnimationValue :
                              (button == ControllerButtons.Grip) ? gripAnimationValue : 0;

                for (int i = 0; i < currentJoints.Count; i++)
                {
                    var joint = currentJoints[i];
                    if (!joint) continue;

                    var goalPos = goalPoseJoints[i].localPosition;
                    var goalRot = goalPoseJoints[i].localRotation;

                    var pos = Vector3.Lerp(startingPose[i].position, goalPos, value);
                    var rot = Quaternion.Lerp(startingPose[i].rotation, goalRot, value);

                    SetNewJoint(ref joint, pos, rot);
                }
                yield return null;
            }
        }

        public void AnimateInstantly(PoseScriptableObject pose)
        {
            if (currentJoints.Count == 0 || !currentJoints[0])
                SetBones();

            if (AnimateToPoseAnimation != null) StopCoroutine(AnimateToPoseAnimation);
            if (AnimateByTriggerValue != null) StopCoroutine(AnimateByTriggerValue);

            SetJointPositions(pose, goalPoseJoints);
            AnimateInstant(goalPoseJoints);
        }

        void AnimateInstant(List<Transform> goalPose)
        {
            for (int i = 0; i < currentJoints.Count; i++)
            {
                var joint = currentJoints[i];
                if (!joint || i >= goalPose.Count || !goalPose[i]) continue;
                SetNewJoint(ref joint, goalPose[i].localPosition, goalPose[i].localRotation);
            }
        }

        IEnumerator AnimateHandToPosition;
        IEnumerator WaitForObjectToBeClose;

        public void MoveHandToTarget(Transform attachPoint, float interactableAttachEaseInTime, bool waitForHandToAnimateToPosition)
        => StartCoroutine(MoveHandToTargetIE(attachPoint, interactableAttachEaseInTime, waitForHandToAnimateToPosition));

        IEnumerator MoveHandToTargetIE(Transform attachPoint, float interactableAttachEaseInTime, bool waitForHandToAnimateToPosition)
        {
            if (waitForHandToAnimateToPosition)
                yield return new WaitForSeconds(interactableAttachEaseInTime);

            transform.parent = null;

            if (AnimateHandToPosition != null) StopCoroutine(AnimateHandToPosition);
            AnimateHandToPosition = AnimateHandToTransform(handMoveToTargetAnimationTime, attachPoint);
            yield return StartCoroutine(AnimateHandToPosition);

            StartHandPositionTracking(attachPoint);
        }

        public void ReturnAnimationsToOriginal()
        {
            DefaultPose = originalPose;
            AnimationPose = originalAnimationPose;
        }

        public void ReturnHandToPlayer()
        {
            StopAllCoroutines();
            transform.parent = originalParent;

            StopHandPositionTracking();
            if (AnimateHandToPosition != null) StopCoroutine(AnimateHandToPosition);

            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
        }

        IEnumerator AnimateHandToTransform(float animationLength, Transform newTransform)
        {
            float timer = 0;
            var startPos = transform.position;
            var startRot = transform.rotation;

            while (timer < animationLength + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(startPos, newTransform.position, timer / animationLength);
                var newRotation = Quaternion.Lerp(startRot, newTransform.rotation, timer / animationLength);

                transform.SetPositionAndRotation(newPosition, newRotation);

                yield return new WaitForSeconds(Time.deltaTime);
                timer += Time.deltaTime;
            }
            transform.SetPositionAndRotation(newTransform.position, newTransform.rotation);
        }

        IEnumerator AnimateHandTransformLocal(float animationLength, TransformStruct newTransform)
        {
            float timer = 0;
            var startPos = transform.localPosition;
            var startRot = transform.localRotation;

            while (timer < animationLength + Time.deltaTime)
            {
                var newPosition = Vector3.Lerp(startPos, newTransform.position, timer / animationLength);
                var newRotation = Quaternion.Lerp(startRot, newTransform.rotation, timer / animationLength);

                transform.localPosition = newPosition;
                transform.localRotation = newRotation;

                yield return new WaitForSeconds(Time.deltaTime);
                timer += Time.deltaTime;
            }
            transform.localPosition = newTransform.position;
            transform.localRotation = newTransform.rotation;
        }

        void StartHandPositionTracking(Transform target)
        {
            setPosition = true;
            handPositionTarget = target;
        }

        void StopHandPositionTracking()
        {
            setPosition = false;
            handPositionTarget = null;
        }

        Transform handPositionTarget;
        bool setPosition;

        [BeforeRenderOrder(102)]
        void OnBeforeRender()
        {
            if (setPosition && handPositionTarget)
                transform.SetPositionAndRotation(handPositionTarget.position, handPositionTarget.rotation);
        }

        protected void SetNewJoint(ref Transform joint, Vector3 newPosition, Quaternion newRotation)
        {
            joint.localPosition = newPosition;
            joint.localEulerAngles = newRotation.eulerAngles;
        }

        protected TransformStruct[] CopyTransformData(List<Transform> joints)
        {
            var transforms = new TransformStruct[joints.Count];
            for (int i = 0; i < joints.Count; i++)
            {
                var j = joints[i];
                transforms[i].SetTransformStruct(j.localPosition, j.localRotation, Vector3.one);
            }
            return transforms;
        }

        protected void SetJointPositions(PoseScriptableObject poseAsset, List<Transform> jointList)
        {
            if (!poseAsset)
            {
                Debug.LogError("No PoseScriptableObject assigned. Cannot set joint positions.");
                return;
            }

            if (!RootBone)
            {
                Debug.LogError("RootBone is not assigned. Cannot set joint positions.");
                return;
            }

            jointList.Clear();

            // Gather all child joints under RootBone
            List<Transform> allJoints = new List<Transform>();
            GatherAllChildJoints(RootBone.transform, allJoints);

            // Match joints from PoseScriptableObject to actual transforms
            foreach (var poseAssetJoint in poseAsset.joints)
            {
                var match = allJoints.Find(joint => joint.name == poseAssetJoint.jointName);
                if (match)
                {
                    match.localPosition = poseAssetJoint.localPosition;
                    match.localRotation = poseAssetJoint.localRotation;
                    jointList.Add(match);
                }
                else
                {
                    Debug.LogWarning($"Transform not found in hierarchy: {poseAssetJoint.jointName}");
                }
            }
        }
        
        void GatherAllChildJoints(Transform parent, List<Transform> jointList)
        {
            if (parent == null || jointList == null) return;

            // Add the current transform to the list
            jointList.Add(parent);

            // Recurse into children
            for (int i = 0; i < parent.childCount; i++)
            {
                GatherAllChildJoints(parent.GetChild(i), jointList);
            }
        }

        public void SetPoseByValue(Transform currentJoint, PoseScriptableObject startPose, PoseScriptableObject endPose,
            float value)
        {
            if (!startPose || !endPose)
            {
                Debug.LogWarning("Missing PoseScriptableObject. Skipping SetPoseByValue.");
                return;
            }

            // Find the corresponding joints in the start and end poses
            var startJoint = startPose.joints.FirstOrDefault(j => j.jointName == currentJoint.name);
            var endJoint = endPose.joints.FirstOrDefault(j => j.jointName == currentJoint.name);

            if (startJoint.jointName == null || endJoint.jointName == null)
            {
                Debug.LogWarning($"Joint {currentJoint.name} not found in one of the poses. Skipping.");
                return;
            }

            // Interpolate the root joint's position and rotation
            var newPosition = Vector3.Lerp(startJoint.localPosition, endJoint.localPosition, value);
            var newRotation = Quaternion.Lerp(startJoint.localRotation, endJoint.localRotation, value);
            SetNewJoint(ref currentJoint, newPosition, newRotation);

            // Recursively process child joints using JointUtility
            var jointsInHand = new List<Transform>();
            JointUtility.GatherTransformsForPose(currentJoint, jointsInHand);

            for (int i = 0; i < jointsInHand.Count; i++)
            {
                var joint = jointsInHand[i];

                var matchingStartJoint = startPose.joints.FirstOrDefault(j => j.jointName == joint.name);
                var matchingEndJoint = endPose.joints.FirstOrDefault(j => j.jointName == joint.name);

                if (matchingStartJoint.jointName == null || matchingEndJoint.jointName == null) continue;

                var childNewPosition = Vector3.Lerp(matchingStartJoint.localPosition, matchingEndJoint.localPosition, value);
                var childNewRotation = Quaternion.Lerp(matchingStartJoint.localRotation, matchingEndJoint.localRotation, value);
                SetNewJoint(ref joint, childNewPosition, childNewRotation);
            }
        }


        #region DynamicPosing

        /// <summary>
        /// Animates the hand to an arbitrary pose defined by raw joint data, blending over
        /// _animationTime seconds. Joints not present in _jointData keep their current position.
        /// This is the handoff point for all procedural posing phases.
        /// </summary>
        public void SetJointsDirect(PoseScriptableObject.JointData[] _jointData, float _animationTime)
        {
            if (_jointData == null || _jointData.Length == 0) return;

            if (currentJoints.Count == 0 || !currentJoints[0])
                SetBones();

            // Snapshot the current live state as the blend start
            TransformStruct[] oldPose = CopyTransformData(currentJoints);

            // Build target array parallel to currentJoints, matched by name
            var newPose = new TransformStruct[currentJoints.Count];
            for (int i = 0; i < currentJoints.Count; i++)
            {
                var joint = currentJoints[i];
                if (!joint) { newPose[i] = oldPose[i]; continue; }

                bool found = false;
                for (int j = 0; j < _jointData.Length; j++)
                {
                    if (_jointData[j].jointName == joint.name)
                    {
                        newPose[i].SetTransformStruct(_jointData[j].localPosition, _jointData[j].localRotation, Vector3.one);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    newPose[i] = oldPose[i];
            }

            if (AnimateByTriggerValue != null) StopCoroutine(AnimateByTriggerValue);
            if (AnimateToPoseAnimation != null) StopCoroutine(AnimateToPoseAnimation);

            AnimateToPoseAnimation = AnimateToPoseOverTime(oldPose, newPose, _animationTime);
            StartCoroutine(AnimateToPoseAnimation);
        }

        /// <summary>
        /// Applies a computed joint pose to the live joints immediately this frame — no blend coroutine.
        /// Same name-matched mapping as <see cref="SetJointsDirect"/>, but writes transforms directly via
        /// <see cref="SetNewJoint"/> (mirrors the per-frame writes in AnimateToPoseByValue2). Joints not
        /// present in the supplied data are left untouched. Intended for per-frame drivers (free-hand touch).
        /// <paramref name="_lerp"/> 1 = snap to the supplied pose; &lt;1 eases from the current pose toward it
        /// (per-frame exponential smoothing supplied by the caller) to damp single-frame solver jitter.
        /// </summary>
        public void SetJointsImmediate(PoseScriptableObject.JointData[] _jointData, float _lerp = 1f)
        {
            if (_jointData == null || _jointData.Length == 0) return;

            if (currentJoints.Count == 0 || !currentJoints[0])
                SetBones();

            bool snap = _lerp >= 1f;
            for (int i = 0; i < currentJoints.Count; i++)
            {
                var joint = currentJoints[i];
                if (!joint) continue;

                for (int j = 0; j < _jointData.Length; j++)
                {
                    if (_jointData[j].jointName == joint.name)
                    {
                        if (snap)
                            SetNewJoint(ref joint, _jointData[j].localPosition, _jointData[j].localRotation);
                        else
                            SetNewJoint(ref joint,
                                Vector3.Lerp(joint.localPosition, _jointData[j].localPosition, _lerp),
                                Quaternion.Slerp(joint.localRotation, _jointData[j].localRotation, _lerp));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Immediately poses a single finger to the lerp between DefaultPose (t=0) and ClosedPose (t=1).
        /// fingerIndex: 0=thumb, 1=index, 2=middle, 3=ring, 4=pinky.
        /// Delegates to the explicit-pose overload using the hand's own DefaultPose / ClosedPose.
        /// </summary>
        public void SetFingerCurl(int fingerIndex, float t) => SetFingerCurl(fingerIndex, t, DefaultPose, ClosedPose);

        /// <summary>
        /// Poses a single finger to the lerp between openPose (t=0) and closedPose (t=1) using
        /// explicitly-supplied poses. A solver sweeps with this so the poses it curls through are
        /// exactly the poses it lerps the final result from (no reliance on the hand's live fields).
        /// </summary>
        public void SetFingerCurl(int fingerIndex, float t, PoseScriptableObject openPose, PoseScriptableObject closedPose)
        {
            if (!openPose || !closedPose)
            {
                Debug.LogWarning("[HandAnimator] SetFingerCurl: openPose or closedPose is not assigned. Assign DefaultPose and a fist/grip ClosedPose to enable per-finger curl.");
                return;
            }

            var chain = fingerMap.Finger(fingerIndex);
            if (chain == null || chain.Count == 0) return;

            // chain[0] is the finger's base joint; SetPoseByValue walks its descendants
            SetPoseByValue(chain[0], openPose, closedPose, t);
        }

        /// <summary>
        /// Convenience overload: pose all five fingers at once.
        /// t must have at least 5 elements (thumb=0, index=1, middle=2, ring=3, pinky=4).
        /// </summary>
        public void SetFingerCurls(float[] t)
        {
            if (t == null || t.Length < 5)
            {
                Debug.LogWarning("[HandAnimator] SetFingerCurls requires an array of at least 5 values.");
                return;
            }
            for (int i = 0; i < 5; i++)
                SetFingerCurl(i, t[i]);
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (!RootBone) return;
            if (drawHelperSpheres)
                RootBone.DrawJoints(RootBone.transform);
            else
                RootBone.debugSpheresEnabled = false;
        }
    }
}
