using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MikeNspired.UnityXRHandPoser
{
    public class HandJointColliders : MonoBehaviour
    {
        private HandAnimator handAnimator = null;

        private Transform root;

        public List<JointTest> colliders = new List<JointTest>();

        private Transform jointColliderHolder;

        private XRBaseControllerInteractor controller;

        private void Start()
        {
            handAnimator = GetComponent<HandAnimator>();
            root = handAnimator.RootBone.transform;
            controller = GetComponentInParent<XRBaseControllerInteractor>();
            controller.onSelectEnter.AddListener(DisableColliders);
            controller.onSelectExit.AddListener(EnableColliders);
            CreateCollider();
        }



        private void EnableColliders(XRBaseInteractable arg0)
        {
            DisableColliders();
        }

        private void DisableColliders(XRBaseInteractable arg0)
        {
            DisableColliders();
        }


        void Update()
        {
            foreach (JointTest child in colliders)
            {
                child.UpdatePosition();
            }
        }

        public void DisableColliders()
        {
            jointColliderHolder.gameObject.SetActive(false);

        }

        public void EnableColliders()
        {
            jointColliderHolder.gameObject.SetActive(true);
        }

        private void CreateColliders(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.name.EndsWith("aux") || child.name.EndsWith("Ignore"))
                {
                    CreateColliders(child);
                    continue;
                }

                var newCollider = new GameObject();
                newCollider.transform.parent = jointColliderHolder;
                newCollider.name = child.name;
                newCollider.AddComponent<CapsuleCollider>();
                newCollider.GetComponent<CapsuleCollider>().radius = .01f;
                newCollider.GetComponent<CapsuleCollider>().height = .04f;
                var newJointCollider = new JointTest(child, newCollider.transform);
                newJointCollider.UpdatePosition();
                colliders.Add(newJointCollider);

                CreateColliders(child);
            }
        }

        void CreateCollider()
        {
            if (jointColliderHolder == null)
            {
                jointColliderHolder = new GameObject().transform;
                jointColliderHolder.name = "JointColliders";
                jointColliderHolder.transform.parent = transform;
                jointColliderHolder.transform.localPosition = Vector3.zero;
                jointColliderHolder.transform.localEulerAngles = Vector3.zero;
            }

            if (colliders.Count > 0)
            {
                foreach (JointTest child in colliders)
                {
                    child.Destroy();
                }

                colliders.Clear();
                return;
            }

            CreateColliders(root);
        }
    }


    [Serializable]
    public struct JointTest
    {
        public Transform joint;
        public Transform collider;

        public JointTest(Transform one, Transform two)
        {
            joint = one;
            collider = two;
            collider.transform.position = joint.transform.position;
        }

        public void UpdatePosition()
        {
            collider.transform.position = joint.transform.position;
            collider.transform.up = joint.transform.right;
        }

        public void Destroy()
        {
            GameObject.DestroyImmediate(collider.gameObject);

        }
    }
}