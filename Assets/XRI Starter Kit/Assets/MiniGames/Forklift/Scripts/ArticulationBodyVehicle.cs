using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class ArticulationBodyVehicle : MonoBehaviour
    {
        [SerializeField] private VehicleAudio vehicleAudio;
        [SerializeField] private ArticulationBody rootArticulationBody;
        [SerializeField] private DrivingGear drivingGear;
        [SerializeField] private DriveType driveTrain, turnType, brakeType;
        [SerializeField] private ArticulationBody frontLeftWheel, frontRightWheel, backLeftWheel, backRightWheel;
        [SerializeField] private float maxSpeed, acceleration, brakeForce, brakeReleaseForce, maxTurnAngle, movementInput, turnInput;
        [SerializeField] private bool isActive;
        private float currentAcceleration, currentBreakForce, currentTurnAngle, appliedMaxSpeed, appliedAcceleration;

        private void Start()
        {
            currentBreakForce = brakeReleaseForce;
            SetDriveTrain(driveTrain);

            if (isActive)
                TurnOn();
            else
                TurnOff();
        }

        private void OnValidate()
        {
            SetDriveTrain(driveTrain);
            if (drivingGear == DrivingGear.Forward)
                SetDrivingGearForward();
            else
                SetDrivingGearReverse();
        }

        private void Update()
        {
            if (!isActive) return;

            UpdateDrivingForces();
            UpdateAcceleration();
            UpdateTurnAngle();
            UpdateBraking();
            UpdateVehicleAudio();
        }


        private void UpdateDrivingForces()
        {
            currentTurnAngle = maxTurnAngle * turnInput;

            if (movementInput < 0)
            {
                currentBreakForce = brakeForce * -movementInput;
                return;
            }

            currentBreakForce = brakeReleaseForce;

            if (drivingGear == DrivingGear.Forward)
                currentAcceleration = appliedMaxSpeed * movementInput;
            else
                currentAcceleration = appliedMaxSpeed * -movementInput;
        }

        private void UpdateVehicleAudio() =>
            vehicleAudio.AdjustAudio(movementInput, rootArticulationBody.linearVelocity.magnitude);

        private void UpdateAcceleration()
        {
            if (driveTrain == DriveType.AllWheel || driveTrain == DriveType.FrontWheel)
            {
                frontLeftWheel.SetDriveTargetVelocity(ArticulationDriveAxis.X, currentAcceleration);
                frontRightWheel.SetDriveTargetVelocity(ArticulationDriveAxis.X, currentAcceleration);
            }

            if (driveTrain == DriveType.AllWheel || driveTrain == DriveType.RearWheel)
            {
                backLeftWheel.SetDriveTargetVelocity(ArticulationDriveAxis.X, currentAcceleration);
                backRightWheel.SetDriveTargetVelocity(ArticulationDriveAxis.X, currentAcceleration);
            }

            if (currentAcceleration == 0)
            {
                frontLeftWheel.SetDriveForceLimit(ArticulationDriveAxis.X, 1);
                frontRightWheel.SetDriveForceLimit(ArticulationDriveAxis.X, 1);
                backLeftWheel.SetDriveForceLimit(ArticulationDriveAxis.X, 1);
                backRightWheel.SetDriveForceLimit(ArticulationDriveAxis.X, 1);
            }
            else
            {
                frontLeftWheel.SetDriveForceLimit(ArticulationDriveAxis.X, appliedAcceleration);
                frontRightWheel.SetDriveForceLimit(ArticulationDriveAxis.X, appliedAcceleration);
                backLeftWheel.SetDriveForceLimit(ArticulationDriveAxis.X, appliedAcceleration);
                backRightWheel.SetDriveForceLimit(ArticulationDriveAxis.X, appliedAcceleration);
            }
        }

        private void UpdateTurnAngle()
        {
            if (turnType == DriveType.AllWheel || turnType == DriveType.FrontWheel)
            {
                frontLeftWheel.parentAnchorRotation = Quaternion.Euler(new Vector3(0, currentTurnAngle, 0));
                frontRightWheel.parentAnchorRotation = Quaternion.Euler(new Vector3(0, currentTurnAngle, 0));
            }

            if (turnType == DriveType.AllWheel || turnType == DriveType.RearWheel)
            {
                backLeftWheel.parentAnchorRotation = Quaternion.Euler(new Vector3(0, currentTurnAngle, 0));
                backRightWheel.parentAnchorRotation = Quaternion.Euler(new Vector3(0, currentTurnAngle, 0));
            }
        }

        private void UpdateBraking()
        {
            if (brakeType == DriveType.AllWheel || brakeType == DriveType.FrontWheel)
            {
                frontLeftWheel.jointFriction = currentBreakForce;
                frontRightWheel.jointFriction = currentBreakForce;
            }

            if (brakeType == DriveType.AllWheel || brakeType == DriveType.RearWheel)
            {
                backLeftWheel.jointFriction = currentBreakForce;
                backRightWheel.jointFriction = currentBreakForce;
            }
        }

        public void SetDriveTrain(DriveType driveType)
        {
            driveTrain = driveType;

            if (driveTrain == DriveType.AllWheel)
            {
                SetDriveType(frontLeftWheel, ArticulationDriveType.Velocity);
                SetDriveType(frontRightWheel, ArticulationDriveType.Velocity);
                SetDriveType(backLeftWheel, ArticulationDriveType.Velocity);
                SetDriveType(backRightWheel, ArticulationDriveType.Velocity);
                appliedAcceleration = acceleration / 4;
                appliedMaxSpeed = maxSpeed / 4;
            }

            else if (driveTrain == DriveType.FrontWheel)
            {
                SetDriveType(frontLeftWheel, ArticulationDriveType.Velocity);
                SetDriveType(frontRightWheel, ArticulationDriveType.Velocity);
                SetDriveType(backLeftWheel, ArticulationDriveType.Force);
                SetDriveType(backRightWheel, ArticulationDriveType.Force);
                appliedAcceleration = acceleration / 2;
                appliedMaxSpeed = maxSpeed / 2;
            }
            else if (driveTrain == DriveType.RearWheel)
            {
                SetDriveType(frontLeftWheel, ArticulationDriveType.Force);
                SetDriveType(frontRightWheel, ArticulationDriveType.Force);
                SetDriveType(backLeftWheel, ArticulationDriveType.Velocity);
                SetDriveType(backRightWheel, ArticulationDriveType.Velocity);
                appliedAcceleration = acceleration / 2;
                appliedMaxSpeed = maxSpeed / 2;
            }

            void SetDriveType(ArticulationBody articulationBody, ArticulationDriveType driveType)
            {
                var articulationDrive = articulationBody.xDrive;
                articulationDrive.driveType = driveType;
                articulationBody.xDrive = articulationDrive;
            }
        }

        public void SetSpeed(float speed) => movementInput = speed;
        public void SetDirection(float direction) => turnInput = direction;

        public void SetDrivingGearForward()
        {
            drivingGear = DrivingGear.Forward;
            vehicleAudio.PlayReverseSound(false);
        }

        public void SetDrivingGearReverse()
        {
            drivingGear = DrivingGear.Reverse;
            vehicleAudio.PlayReverseSound(true);
        }

        public void EngineState(int state)
        {
            if (state == 0)
                TurnOff();
            else
                TurnOn();
        }

        public void TurnOn()
        {
            vehicleAudio.TurnOn();
            isActive = true;
        }

        public void TurnOff()
        {
            vehicleAudio.TurnOff();
            isActive = false;
        }


        public enum DriveType
        {
            AllWheel,
            FrontWheel,
            RearWheel
        }

        private enum DrivingGear
        {
            Forward,
            Reverse
        }
    }
}