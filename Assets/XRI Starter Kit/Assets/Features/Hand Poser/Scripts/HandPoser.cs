// Author MikeNspired. 
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// The main script to setup the hand for animations.
    /// Its main purpose is to quickly setup hand poses for each item, and then assign those poses to the hand when the item is grabbed.
    /// </summary>
    public class HandPoser : MonoBehaviour
    {
        public PoseScriptableObject leftHandPose;
        public PoseScriptableObject rightHandPose;
        public PoseScriptableObject LeftHandAnimationPose;
        public PoseScriptableObject RightHandAnimationPose;

        public Transform leftHandAttach = null;
        public Transform rightHandAttach = null;

        [SerializeField] private bool hasAnimationPose = false;
        [SerializeField] private HandAnimator currentLeftHand = null;
        [SerializeField] private HandAnimator currentRightHand = null;
        [SerializeField] private Transform grabAttachPoints = null;

        private HandAnimator currentHandGrabbing;

        public bool HasAnimationPose => hasAnimationPose;

        protected virtual void Awake()
        {
            if (!hasAnimationPose)
            {
                LeftHandAnimationPose = null;
                RightHandAnimationPose = null;
            }

            if (grabAttachPoints == null || leftHandAttach == null || rightHandAttach == null)
            {
                CreateTransforms();
            }
        }

        //Determine which hand is being grabbed to send the hand animator the proper poses for the hand.
        protected virtual void BeginNewHandPoses(HandAnimator hand)
        {
            currentHandGrabbing = hand;
            
            if (!hasAnimationPose)
            {
                LeftHandAnimationPose = null;
                RightHandAnimationPose = null;
            }
            if (hand.handType == LeftRight.Left)
            {
                currentLeftHand = hand;
                SetToPose(currentLeftHand, leftHandPose, LeftHandAnimationPose);
            }
            else
            {
                currentRightHand = hand;
                SetToPose(currentRightHand, rightHandPose, RightHandAnimationPose);
            }
        }

        //Reset the hand back to its original position and restore the original animations.
        protected void Release()
        {
            if (!currentHandGrabbing) return;

            currentHandGrabbing.ReturnHandToPlayer();
            currentHandGrabbing.ReturnAnimationsToOriginal();
            currentHandGrabbing.ReturnToDefaultPosing();
            currentHandGrabbing = null;
        }


        //Tells the hand to begin the new poses
        private void SetToPose(HandAnimator hand, PoseScriptableObject primaryPose, PoseScriptableObject animPose)
        {
            hand.BeginNewPoses(primaryPose, animPose, true);
        }


        //------------------------------------ In Editor Methods ------------------------------------

        #region EditorMethods

        private void SetToPoseInEditor(HandAnimator hand, PoseScriptableObject primaryPose, PoseScriptableObject animPose)
        {
            hand.SetPoses(primaryPose, animPose);
            hand.AnimateInstantly(primaryPose);
        }


        private void MirrorHand(HandAnimator handToCopyTo, HandAnimator originalHand)
        {
            var pos = originalHand.transform.localPosition;
            handToCopyTo.transform.localPosition = new Vector3(-pos.x, pos.y, pos.z);

            var rot = originalHand.transform.localEulerAngles;

            handToCopyTo.transform.localEulerAngles = new Vector3(rot.x, -rot.y, -rot.z);
            handToCopyTo.DefaultPose = originalHand.DefaultPose;
            handToCopyTo.AnimateToCurrent();

            if (handToCopyTo.handType == LeftRight.Left)
            {
                leftHandPose = rightHandPose;
                LeftHandAnimationPose = RightHandAnimationPose;
            }
            else
            {
                rightHandPose = leftHandPose;
                RightHandAnimationPose = LeftHandAnimationPose;
            }
        }


        private void CreateTransforms()
        {
            if (transform.GetComponent<XRBaseInteractable>())
                CreateGrabTransform(ref grabAttachPoints, transform, nameof(grabAttachPoints));
            else
                grabAttachPoints = transform;

            CreateGrabTransform(ref leftHandAttach, grabAttachPoints, nameof(leftHandAttach));
            CreateGrabTransform(ref rightHandAttach, grabAttachPoints, nameof(rightHandAttach));

            void CreateGrabTransform(ref Transform grabTransform, Transform parent, string name)
            {
                if (grabTransform) return;
                grabTransform = parent.Find(name.First().ToString().ToUpper() + name.Substring(1));

                if (grabTransform) return;

                grabTransform = new GameObject().transform;
                grabTransform.parent = parent;
                grabTransform.localPosition = Vector3.zero;
                grabTransform.localEulerAngles = Vector3.zero;

                grabTransform.name = name.First().ToString().ToUpper() + name.Substring(1);
            }
        }

        public void CreateLeftHand()
        {
            CreateHand(ref currentLeftHand, LeftRight.Left);
            if (!leftHandPose)
                leftHandPose = HandPoserSettings.Instance.DefaultPose;
        }

        public void CreateRightHand()
        {
            CreateHand(ref currentRightHand, LeftRight.Right);
            if (!rightHandPose)
                rightHandPose = HandPoserSettings.Instance.DefaultPose;
        }

        public void DestroyLeftHand() => DestroyImmediate(currentLeftHand.gameObject);
        public void DestroyRightHand() => DestroyImmediate(currentRightHand.gameObject);


        private void CreateHand(ref HandAnimator curHand, LeftRight handType)
        {
            CreateTransforms();

            if (curHand != null)
            {
                DestroyImmediate(curHand.gameObject);
                return;
            }

            string name = handType == LeftRight.Left ? nameof(HandPoserSettings.Instance.LeftHand) : nameof(HandPoserSettings.Instance.RightHand);

            HandAnimator hand = transform.Find(name)?.GetComponent<HandAnimator>();
            if (hand != null)
            {
                curHand = hand;
                return;
            }

            var handPrefab = handType == LeftRight.Left ? (HandPoserSettings.Instance.LeftHand) : (HandPoserSettings.Instance.RightHand);

            if (handPrefab == null) HandPoserSettings.ShowNotSetupWarning();
            
            hand = Instantiate(handPrefab);
            hand.name = hand.name.Replace("(Clone)", "").Trim();
            hand.transform.parent = grabAttachPoints;
            hand.transform.localPosition = Vector3.zero;
            hand.transform.localEulerAngles = Vector3.zero;
            hand.SetBones();
            curHand = hand;
        }

        private void SetHandToPose(HandAnimator curHand, Transform attachmentPoint, PoseScriptableObject currentPose)
        {
            curHand.transform.SetPositionAndRotation(attachmentPoint.transform.position, attachmentPoint.transform.rotation);
            SetToPoseInEditor(curHand, currentPose, null);
        }


        public void SetLeftHandToPose() => SetHandToPose(currentLeftHand, leftHandAttach, leftHandPose);


        public void SetRightHandToPose() => SetHandToPose(currentRightHand, rightHandAttach, rightHandPose);


        public void SetLeftHandToAnimationPose() => SetHandToPose(currentLeftHand, leftHandAttach, LeftHandAnimationPose);

        public void SetRightHandToAnimationPose() => SetHandToPose(currentRightHand, rightHandAttach, RightHandAnimationPose);


        public void CopyLeftToRight() => MirrorHand(currentRightHand, currentLeftHand);

        public void CopyRightToLeft() => MirrorHand(currentLeftHand, currentRightHand);

        public void MatchPoses()
        {
            if (leftHandPose != null)
                rightHandPose = leftHandPose;
            else if (rightHandPose != null)
                leftHandPose = rightHandPose;
            if (LeftHandAnimationPose != null)
                RightHandAnimationPose = LeftHandAnimationPose;
            else if (RightHandAnimationPose != null)
                LeftHandAnimationPose = RightHandAnimationPose;
        }

        public void SaveAttachPoints()
        {
            if (currentLeftHand)
                UpdateOffSet(ref leftHandAttach, currentLeftHand);

            if (currentRightHand)
                UpdateOffSet(ref rightHandAttach, currentRightHand);

            void UpdateOffSet(ref Transform offSet, HandAnimator hand)
            {
                offSet.position = hand.transform.position;
                offSet.rotation = hand.transform.rotation;
                Debug.Log(offSet.gameObject.name + " position and rotation updated.");
            }
        }

        #endregion
    }

}