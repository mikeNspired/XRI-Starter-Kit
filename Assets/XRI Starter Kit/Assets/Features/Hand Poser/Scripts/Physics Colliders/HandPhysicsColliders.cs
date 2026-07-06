// Author MikeNspired.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(HandAnimator))]
    public class HandPhysicsColliders : MonoBehaviour
    {
        [SerializeField] private HandColliderConfig config;
        [Tooltip("Layer assigned to every built collider. Use a dedicated layer and exclude it from " +
                 "the NearFar interactor and DistanceGrabber masks so the fingers don't block grabs.")]
        [SerializeField] private int colliderLayer;

        [Tooltip("Draw the built colliders as Scene-view gizmos while this hand is selected " +
                 "(green = enabled, red = disabled). Purely a setup aid — turn off once the colliders " +
                 "are dialed in so they stop cluttering the view.")]
        [SerializeField] private bool drawGizmos = true;

        private HandAnimator handAnimator;

        // Serialized so edit-mode colliders survive domain reloads and aren't duplicated on rebuild.
        [SerializeField, HideInInspector] private List<Collider> fingerColliders = new();
        [SerializeField, HideInInspector] private List<GameObject> generatedChildren = new();
        private bool isGrabbing;

        void Awake()
        {
            handAnimator = GetComponent<HandAnimator>();

            var rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        void Start()
        {
            // Disable the physical colliders while this hand is grabbing so they can't fight the
            // grabbed object or the curl solver, and re-enable on release. This runtime job is why
            // the component stays on the hand — it is not an edit-only collider builder.
            var handRef = GetComponentInParent<HandReference>();
            if (handRef != null && handRef.NearFarInteractor != null)
            {
                handRef.NearFarInteractor.selectEntered.AddListener(_ => SetCollidersEnabled(false));
                handRef.NearFarInteractor.selectExited.AddListener(_ => SetCollidersEnabled(true));
            }

            // Edit-mode colliders are prebuilt geometry; only build at runtime if none exist yet.
            if (fingerColliders == null || fingerColliders.Count == 0)
                BuildColliders();
        }

        public void BuildColliders()
        {
            ClearColliders();

            if (handAnimator == null)
                handAnimator = GetComponent<HandAnimator>();

            // A duplicated/mirrored hand can carry a serialized joint list of all-null refs, so
            // rebuild when the list is empty or every entry is null.
            var joints = handAnimator.currentJoints;
            if (joints.Count == 0 || AllNull(joints))
            {
                handAnimator.SetBones();
                joints = handAnimator.currentJoints;
            }

            bool verbose = config != null && config.verboseBuildLogging;

            WarnIfMirroredScale();

            if (joints == null || joints.Count == 0)
            {
                Debug.LogWarning($"[HandPhysicsColliders] {name}: no joints to build from. " +
                                 "Is RootBone assigned on the HandAnimator and did SetBones run?", this);
                return;
            }

            var jointSet = new HashSet<Transform>(joints);

            // Ignored joints are skipped as collider owners but bridged through to keep the chain linked.
            int nonNull = 0;
            var activeSet = new HashSet<Transform>();
            foreach (var j in joints)
            {
                if (!j) continue;
                nonNull++;
                if (!(config != null && config.IsIgnoredJoint(j.name))) activeSet.Add(j);
            }

            if (verbose)
                Debug.Log($"[HandPhysicsColliders] {name}: joints={joints.Count}, non-null={nonNull}, active={activeSet.Count}", this);

            if (nonNull == 0)
            {
                Debug.LogWarning($"[HandPhysicsColliders] {name}: HandAnimator.currentJoints has " +
                                 $"{joints.Count} entries but all are null (stale references, e.g. a " +
                                 "duplicated/mirrored hand). Re-assign RootBone on the HandAnimator so " +
                                 "SetBones can rebuild the joint list.", this);
                return;
            }

            if (activeSet.Count == 0)
            {
                Debug.LogWarning($"[HandPhysicsColliders] {name}: every joint matched an ignore token " +
                                 "(ignoreJointNameContains) — nothing left to build colliders on.", this);
                return;
            }

            foreach (var joint in joints)
            {
                if (!joint || !activeSet.Contains(joint)) continue;

                // Catch per joint so a single failure is named instead of aborting the whole build.
                try
                {
                    var jointChildren = new List<Transform>();
                    CollectActiveChildren(joint, jointSet, activeSet, jointChildren);

                    if (jointChildren.Count > 1)
                        AddPalmCollider(joint);
                    else if (jointChildren.Count == 1)
                        AddCapsuleCollider(joint, jointChildren[0]);
                    else
                    {
                        // Leaf: skip dedicated tip markers (parent capsule covers them), otherwise
                        // extend a distal bone past the last real joint.
                        if (config != null && config.IsFingertipMarker(joint.name)) continue;
                        AddDistalTipCollider(joint);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HandPhysicsColliders] {name}: failed building collider for " +
                                   $"'{joint.name}': {e}", joint);
                }
            }

            ApplyColliderLayer();

            if (verbose)
                Debug.Log($"[HandPhysicsColliders] {name}: built {fingerColliders.Count} colliders.", this);
        }

        // A collider's layer is its GameObject's layer; apply to each so they can be excluded from
        // the interactor/grabber masks.
        private void ApplyColliderLayer()
        {
            foreach (var col in fingerColliders)
                if (col) col.gameObject.layer = colliderLayer;
        }

        // Diagnostic only: Unity colliders can't be mirrored, so warn when the hand or an ancestor
        // has negative scale (still attempts the build).
        private void WarnIfMirroredScale()
        {
            if (config == null || !config.warnOnMirroredScale) return;

            for (var t = transform; t != null; t = t.parent)
            {
                var s = t.localScale;
                if (s.x < 0f || s.y < 0f || s.z < 0f)
                {
                    Debug.LogWarning($"[HandPhysicsColliders] '{t.name}' has negative scale {s}. Unity " +
                                     "colliders cannot be mirrored; capsule shapes and positions will be " +
                                     "wrong. Mirror the physics hand by rotation rather than negative " +
                                     "scale. (Disable warnOnMirroredScale to silence.)", t);
                    return;
                }
            }
        }

        // Nearest active descendant joints, descending through skipped joints to bridge connectivity.
        private static void CollectActiveChildren(Transform t, HashSet<Transform> jointSet, HashSet<Transform> activeSet, List<Transform> result)
        {
            for (int c = 0; c < t.childCount; c++)
            {
                var child = t.GetChild(c);
                if (activeSet.Contains(child))
                    result.Add(child);
                else if (jointSet.Contains(child))
                    CollectActiveChildren(child, jointSet, activeSet, result);
            }
        }

        private void AddPalmCollider(Transform joint)
        {
            if (config == null || !config.addPalmCollider) return;

            if (config.palmShape == PalmColliderShape.Box)
            {
                // Effective box size = size * lossyScale; negate size on mirrored axes so the
                // effective box stays positive (no child transform needed).
                Vector3 lossy = joint.lossyScale;
                var box = joint.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(
                    config.palmBoxSize.x * (lossy.x < 0f ? -1f : 1f),
                    config.palmBoxSize.y * (lossy.y < 0f ? -1f : 1f),
                    config.palmBoxSize.z * (lossy.z < 0f ? -1f : 1f));
                box.center = config.palmOffset;
                box.enabled = !isGrabbing;
                fingerColliders.Add(box);
            }
            else
            {
                var sphere = joint.gameObject.AddComponent<SphereCollider>();
                sphere.radius = config.palmRadius;
                sphere.center = config.palmOffset;
                sphere.enabled = !isGrabbing;
                fingerColliders.Add(sphere);
            }
        }

        private void AddCapsuleCollider(Transform joint, Transform childJoint)
        {
            // Local space respects non-uniform scale.
            Vector3 localVec = joint.InverseTransformVector(childJoint.position - joint.position);
            float localLength = localVec.magnitude;
            if (localLength < 0.001f)
            {
                if (config != null && config.verboseBuildLogging)
                    Debug.Log($"[HandPhysicsColliders] {name}: skipped '{joint.name}' — bone to " +
                              $"'{childJoint.name}' is degenerate (len {localLength:F5}).", joint);
                return;
            }

            Vector3 localDir = localVec.normalized;

            var fingerCfg = config?.GetFingerConfig(joint.name);
            var overrideCfg = config?.GetJointOverride(joint.name);

            float radMult = (config?.globalRadiusMultiplier ?? 0.35f) * (fingerCfg?.radiusMultiplier ?? 1f);
            float hMult = (config?.globalHeightMultiplier ?? 1f) * (fingerCfg?.heightMultiplier ?? 1f);

            float height = overrideCfg is { overrideHeight: true }
                ? overrideCfg.height
                : localLength * hMult;

            float radius = overrideCfg is { overrideRadius: true }
                ? overrideCfg.radius
                : Mathf.Min(localLength * radMult, height * 0.49f);

            var col = joint.gameObject.AddComponent<CapsuleCollider>();
            col.direction = DominantAxis(localDir);
            col.height = height;
            col.radius = radius;
            col.center = localDir * (localLength * 0.5f) + (overrideCfg?.localOffset ?? Vector3.zero);
            col.enabled = !isGrabbing;

            fingerColliders.Add(col);
        }

        // Distal phalanx for a leaf that is the last real joint (no child marks the tip); direction
        // and length come from the parent bone.
        private void AddDistalTipCollider(Transform joint)
        {
            var parent = joint.parent;
            if (parent == null)
            {
                AddSphereTip(joint);
                return;
            }

            Vector3 worldDir = joint.position - parent.position;
            Vector3 localVec = joint.InverseTransformVector(worldDir);
            float parentLength = localVec.magnitude;
            if (parentLength < 0.001f)
            {
                if (config != null && config.verboseBuildLogging)
                    Debug.Log($"[HandPhysicsColliders] {name}: '{joint.name}' parent bone degenerate " +
                              $"(len {parentLength:F5}); using sphere tip.", joint);
                AddSphereTip(joint);
                return;
            }

            Vector3 localDir = localVec.normalized;

            var fingerCfg = config?.GetFingerConfig(joint.name);
            var overrideCfg = config?.GetJointOverride(joint.name);

            float radMult = (config?.globalRadiusMultiplier ?? 0.35f) * (fingerCfg?.radiusMultiplier ?? 1f);
            float hMult = (config?.globalHeightMultiplier ?? 1f) * (fingerCfg?.heightMultiplier ?? 1f);
            float distalMult = config?.distalLengthMultiplier ?? 0.8f;

            float height = overrideCfg is { overrideHeight: true }
                ? overrideCfg.height
                : parentLength * distalMult * hMult;

            float radius = overrideCfg is { overrideRadius: true }
                ? overrideCfg.radius
                : Mathf.Min(parentLength * radMult, height * 0.49f);

            Vector3 localOffset = overrideCfg?.localOffset ?? Vector3.zero;

            // Opt-in: a single-axis CapsuleCollider tilts on joints not aligned with the finger, so
            // place it on a child rotated down the bone.
            if (config != null && config.orientDistalTipWithChild)
            {
                // "Ignore" suffix so JointUtility (EndsWith("Ignore")) keeps the Hand Poser off it.
                var go = new GameObject(joint.name + "_DistalCollider_Ignore");
                var childT = go.transform;
                childT.SetParent(joint, false);
                childT.localPosition = Vector3.zero;
                childT.localRotation = Quaternion.FromToRotation(Vector3.up, localDir);

                var childCol = go.AddComponent<CapsuleCollider>();
                childCol.direction = 1; // Y, matching the FromToRotation above
                childCol.height = height;
                childCol.radius = radius;
                childCol.center = Vector3.up * (height * 0.5f) + localOffset;
                childCol.enabled = !isGrabbing;

                generatedChildren.Add(go);
                fingerColliders.Add(childCol);
                return;
            }

            var col = joint.gameObject.AddComponent<CapsuleCollider>();
            col.direction = DominantAxis(localDir);
            col.height = height;
            col.radius = radius;
            col.center = localDir * (height * 0.5f) + localOffset;
            col.enabled = !isGrabbing;

            fingerColliders.Add(col);
        }

        // Fallback tip when no bone direction can be inferred (e.g. a parentless leaf).
        private void AddSphereTip(Transform joint)
        {
            var fingerCfg = config?.GetFingerConfig(joint.name);
            var overrideCfg = config?.GetJointOverride(joint.name);

            float globalScale = (config?.globalRadiusMultiplier ?? 0.35f) / 0.35f;
            float tipRadius = overrideCfg is { overrideRadius: true }
                ? overrideCfg.radius
                : 0.008f * globalScale * (fingerCfg?.radiusMultiplier ?? 1f);

            var col = joint.gameObject.AddComponent<CapsuleCollider>();
            col.radius = tipRadius;
            col.height = tipRadius * 2f;
            col.center = overrideCfg?.localOffset ?? Vector3.zero;
            col.enabled = !isGrabbing;

            fingerColliders.Add(col);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            isGrabbing = !enabled;
            foreach (var col in fingerColliders)
                if (col) col.enabled = enabled;
        }

        // Public so the "Clear Colliders" editor button can remove colliders without rebuilding.
        public void ClearColliders()
        {
            foreach (var col in fingerColliders)
                if (col) DestroyImmediate(col);
            fingerColliders.Clear();

            foreach (var go in generatedChildren)
                if (go) DestroyImmediate(go);
            generatedChildren.Clear();
        }

        private static bool AllNull(List<Transform> list)
        {
            foreach (var t in list)
                if (t) return false;
            return true;
        }

        private static int DominantAxis(Vector3 v)
        {
            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
            if (ax >= ay && ax >= az) return 0;
            if (ay >= ax && ay >= az) return 1;
            return 2;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            foreach (var col in fingerColliders)
            {
                if (!col) continue;

                Gizmos.color = col.enabled ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.3f);

                if (col is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(col.transform.TransformPoint(sphere.center), sphere.radius);
                    continue;
                }

                if (col is BoxCollider box)
                {
                    var prev = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(box.transform.position, box.transform.rotation, box.transform.lossyScale);
                    Gizmos.DrawWireCube(box.center, box.size);
                    Gizmos.matrix = prev;
                    continue;
                }

                if (col is CapsuleCollider cap)
                {
                    var t = cap.transform;
                    Vector3 center = t.TransformPoint(cap.center);
                    Vector3 axis = cap.direction == 0 ? t.right : cap.direction == 1 ? t.up : t.forward;
                    float halfExtent = Mathf.Max(0f, cap.height * 0.5f - cap.radius);
                    Gizmos.DrawWireSphere(center + axis * halfExtent, cap.radius);
                    Gizmos.DrawWireSphere(center - axis * halfExtent, cap.radius);
                }
            }
        }
    }
}
