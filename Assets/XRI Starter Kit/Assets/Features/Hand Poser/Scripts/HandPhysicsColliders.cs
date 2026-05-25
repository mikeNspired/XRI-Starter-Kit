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
        private readonly List<Collider> fingerColliders = new();
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

            foreach (var joint in joints)
            {
                if (!joint) continue;

                // Collect children that are also joints
                var jointChildren = new List<Transform>();
                for (int c = 0; c < joint.childCount; c++)
                {
                    var child = joint.GetChild(c);
                    if (jointSet.Contains(child)) jointChildren.Add(child);
                }

                if (jointChildren.Count > 1)
                {
                    // Palm/branching joint — add sphere collider
                    AddPalmCollider(joint);
                }
                else if (jointChildren.Count == 1)
                {
                    AddCapsuleCollider(joint, jointChildren[0]);
                }
                else
                {
                    // Fingertip — add small sphere-like capsule
                    AddTipCollider(joint);
                }
            }
        }

        private void AddPalmCollider(Transform joint)
        {
            if (config == null || !config.addPalmCollider) return;

            var col = joint.gameObject.AddComponent<SphereCollider>();
            col.radius = config.palmRadius;
            col.center = config.palmOffset;
            col.enabled = !isGrabbing;
            fingerColliders.Add(col);
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
            float hMult = config?.globalHeightMultiplier ?? 1f;

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

        private void AddTipCollider(Transform joint)
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
