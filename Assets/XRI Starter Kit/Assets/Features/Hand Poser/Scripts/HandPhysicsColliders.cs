using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MikeNspired.XRIStarterKit
{
    [RequireComponent(typeof(HandAnimator))]
    public class HandPhysicsColliders : MonoBehaviour
    {
        [SerializeField] private HandColliderConfig config;
        [SerializeField] private LayerMask interactLayers = ~0;

        private HandAnimator handAnimator;

        // Serialized so colliders built in edit mode are tracked across domain reloads /
        // play-mode entry. Without this, the list would be empty after a reload and a
        // rebuild would stack a duplicate set of colliders on top of the existing ones.
        [SerializeField, HideInInspector] private List<Collider> fingerColliders = new();
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
            var handRef = GetComponentInParent<HandReference>();
            if (handRef != null)
            {
                handRef.NearFarInteractor.selectEntered.AddListener(_ => SetCollidersEnabled(false));
                handRef.NearFarInteractor.selectExited.AddListener(_ => SetCollidersEnabled(true));
            }

            // Colliders built (and persisted) in edit mode act as prebuilt geometry — only
            // build at runtime as a fallback when none exist yet.
            if (fingerColliders == null || fingerColliders.Count == 0)
                BuildColliders();
        }

        // Called from editor and at Start
        public void BuildColliders()
        {
            ClearColliders();

            if (handAnimator == null)
                handAnimator = GetComponent<HandAnimator>();

            if (handAnimator.currentJoints.Count == 0)
                handAnimator.SetBones();

            var joints = handAnimator.currentJoints;
            var jointSet = new HashSet<Transform>(joints);

            // Active joints get colliders; aux/helper joints are skipped but still used as
            // pass-through links so the bone chain stays connected (e.g. a finger's MCP
            // knuckle named "..._aux" is bridged to the next real joint).
            var activeSet = new HashSet<Transform>();
            foreach (var j in joints)
                if (j && !(config != null && config.IsIgnoredJoint(j.name))) activeSet.Add(j);

            foreach (var joint in joints)
            {
                if (!joint || !activeSet.Contains(joint)) continue;

                // Collect the nearest active descendant joints, bridging through skipped ones.
                var jointChildren = new List<Transform>();
                CollectActiveChildren(joint, jointSet, activeSet, jointChildren);

                if (jointChildren.Count > 1)
                {
                    // Palm/branching joint — add palm collider (sphere or box)
                    AddPalmCollider(joint);
                }
                else if (jointChildren.Count == 1)
                {
                    AddCapsuleCollider(joint, jointChildren[0]);
                }
                else
                {
                    // Leaf joint. Two rigs to support:
                    //  - Rig with a dedicated non-rotating tip marker: the parent capsule already
                    //    spans to it, so this leaf needs no collider.
                    //  - Rig where the leaf is the last *real* joint (e.g. a DIP) with the distal
                    //    phalanx extending past it and no child to define the tip: build a distal
                    //    bone that continues the finger's last direction.
                    if (config != null && config.IsFingertipMarker(joint.name)) continue;
                    AddDistalTipCollider(joint);
                }
            }
        }

        // Nearest active descendant joints. Descends through skipped joints (still in jointSet)
        // to bridge connectivity; ignores non-joint transforms, matching the original behavior.
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
                var box = joint.gameObject.AddComponent<BoxCollider>();
                box.size = config.palmBoxSize;
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
            // Convert world-space vector to local to respect non-uniform scale
            Vector3 localVec = joint.InverseTransformVector(childJoint.position - joint.position);
            float localLength = localVec.magnitude;
            if (localLength < 0.001f) return;

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

        // Builds the distal phalanx for a leaf that is the last *real* joint of a finger (no child
        // transform marks the tip). Direction and length are derived from the parent bone so the
        // distal capsule continues the finger naturally instead of being a tiny overlapping sphere.
        private void AddDistalTipCollider(Transform joint)
        {
            var parent = joint.parent;

            // Without a parent we can't infer a bone direction; fall back to a small sphere.
            if (parent == null)
            {
                AddSphereTip(joint);
                return;
            }

            // Continue the parent->joint bone direction, expressed in the joint's local space.
            Vector3 worldDir = joint.position - parent.position;
            Vector3 localVec = joint.InverseTransformVector(worldDir);
            float parentLength = localVec.magnitude;
            if (parentLength < 0.001f)
            {
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

            var col = joint.gameObject.AddComponent<CapsuleCollider>();
            col.direction = DominantAxis(localDir);
            col.height = height;
            col.radius = radius;
            // Extend outward from the joint along the finger direction.
            col.center = localDir * (height * 0.5f) + (overrideCfg?.localOffset ?? Vector3.zero);
            col.enabled = !isGrabbing;

            fingerColliders.Add(col);
        }

        // Last-resort tiny tip when no bone direction can be inferred (e.g. a parentless leaf).
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

        private void ClearColliders()
        {
            foreach (var col in fingerColliders)
                if (col) DestroyImmediate(col);
            fingerColliders.Clear();
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
