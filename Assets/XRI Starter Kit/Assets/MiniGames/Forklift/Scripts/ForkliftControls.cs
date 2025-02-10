using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class ForkliftControls : MonoBehaviour
    {
        [SerializeField] private ThreePartAudio liftAudio, tiltAudio;
        [SerializeField] private Transform forkRiser, forks;
        [SerializeField] private float tiltSpeed = 5f, liftSpeed = .5f;
        [SerializeField] private float forksMaxHeight;
        [SerializeField] private ArticulationBody forksRigidBody, rotationRigidBody;
        [SerializeField] private bool isActive;
        private Transform forkRiserParent;

        private void Start()
        {
            forkRiserParent = forkRiser.parent;
            if (isActive)
                TurnOn();
            else
                TurnOff();
        }

        public void TurnOn()
        {
            isActive = true;
        }

        public void TurnOff()
        {
            isActive = false;
            liftAudio.Stop();
            tiltAudio.Stop();
        }

        public void UpdateLift(float speed) => Lift(speed);
        public void UpdateTilt(float speed) => Tilt(speed);

        private void Tilt(float tiltValue)
        {
            if (!isActive) return;
            if (Mathf.Abs(tiltValue) < .1f)
            {
                tiltAudio.Stop();
                return;
            }

            rotationRigidBody.SetDriveTarget(ArticulationDriveAxis.X,
                GetDriveTargetValue(rotationRigidBody.xDrive, tiltValue * tiltSpeed));

            if (CheckIfDriveLimit(rotationRigidBody.xDrive))
                tiltAudio.Stop();
            else
                tiltAudio.Play();
        }

        private void Lift(float liftValue)
        {
            if (!isActive) return;
            if (Mathf.Abs(liftValue) < .1f)
            {
                liftAudio.Stop();
                return;
            }

            switch (liftValue)
            {
                case > 0:
                    Raise();
                    break;
                case < 0:
                    Lower();
                    break;
            }

            if (CheckIfDriveLimit(forksRigidBody.zDrive))
                liftAudio.Stop();
            else
                liftAudio.Play();


            void Raise()
            {
                forksRigidBody.SetDriveTarget(ArticulationDriveAxis.Z,
                    GetDriveTargetValue(forksRigidBody.zDrive, liftValue * liftSpeed));
                if (forks.localPosition.z > forksMaxHeight)
                    forkRiser.SetParent(forksRigidBody.transform);
            }

            void Lower()
            {
                forksRigidBody.SetDriveTarget(ArticulationDriveAxis.Z,
                    GetDriveTargetValue(forksRigidBody.zDrive, liftValue * liftSpeed));
                if (!(forks.localPosition.z <= forksMaxHeight) || forkRiser.parent == forkRiserParent) return;
                forkRiser.SetParent(forkRiserParent);
                forkRiser.localPosition = Vector3.zero;
                forkRiser.localEulerAngles = Vector3.zero;
            }
        }

        private bool CheckIfDriveLimit(ArticulationDrive drive)
        {
            if (drive.target <= drive.lowerLimit)
                return true;
            if (drive.target >= drive.upperLimit)
                return true;
            return false;
        }

        private float GetDriveTargetValue(ArticulationDrive drive, float inputValue)
        {
            var currentValue = drive.target;
            var minValue = drive.lowerLimit;
            var maxValue = drive.upperLimit;
            var ratio = Mathf.InverseLerp(minValue, maxValue, currentValue) / 1;

            if (inputValue > 0)
            {
                var percentage = ratio + Mathf.Abs(inputValue) * Time.deltaTime;
                currentValue = Mathf.Lerp(minValue, maxValue, Mathf.Abs(percentage));
            }
            else
            {
                var percentage = Mathf.Abs(1 - ratio) + Mathf.Abs(inputValue) * Time.deltaTime;
                currentValue = Mathf.Lerp(maxValue, minValue, Mathf.Abs(percentage));
            }

            return currentValue;
        }
    }
}