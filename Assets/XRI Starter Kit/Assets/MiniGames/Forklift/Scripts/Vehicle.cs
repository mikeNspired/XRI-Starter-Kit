using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class Vehicle : MonoBehaviour
    {
        [SerializeField] private VehicleAudio vehicleAudio;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private DriveType driveTrain, turnType, brakeType;
        [SerializeField] private WheelCollider frontLeftWheel, frontRightWheel, backLeftWheel, backRightWheel;
        [SerializeField] private Transform frontLeftWheelMesh, frontRightWheelMesh, backLeftWheelMesh, backRightWheelMesh;
        [SerializeField] private float acceleration, brakeForce, maxTurnAngle, movementInput, turnInput;
        [SerializeField] private DrivingGear drivingGear;
        [SerializeField] private bool isActive;
        private float currentAcceleration, currentBreakForce, currentTurnAngle;

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
            if (isActive)
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

        private void Start()
        {
            if (isActive)
                TurnOn();
            else
                TurnOff();
        }

        private void Update()
        {
            if (!isActive) return;

            UpdateDrivingForces();
            UpdateAcceleration();
            UpdateTurnAngle();
            UpdateBraking();
            UpdateWheels();
            UpdateVehicleAudio();
        }

        private void UpdateDrivingForces()
        {
            if (movementInput <= 0)
                currentBreakForce = brakeForce * -movementInput;

            if (drivingGear == DrivingGear.Forward)
                currentAcceleration = acceleration * movementInput;
            else
                currentAcceleration = acceleration * -movementInput;

            currentTurnAngle = maxTurnAngle * turnInput;
        }

        private void UpdateVehicleAudio() => vehicleAudio.AdjustAudio(movementInput, rb.linearVelocity.magnitude);

        private void UpdateAcceleration()
        {
            if (driveTrain == DriveType.AllWheel || driveTrain == DriveType.FrontWheel)
            {
                frontLeftWheel.motorTorque = currentAcceleration;
                frontRightWheel.motorTorque = currentAcceleration;
            }

            if (driveTrain == DriveType.AllWheel || driveTrain == DriveType.RearWheel)
            {
                backLeftWheel.motorTorque = currentAcceleration;
                backRightWheel.motorTorque = currentAcceleration;
            }
        }

        private void UpdateTurnAngle()
        {
            if (turnType == DriveType.AllWheel || turnType == DriveType.FrontWheel)
            {
                frontLeftWheel.steerAngle = currentTurnAngle;
                frontRightWheel.steerAngle = currentTurnAngle;
            }

            if (turnType == DriveType.AllWheel || turnType == DriveType.RearWheel)
            {
                backLeftWheel.steerAngle = currentTurnAngle;
                backRightWheel.steerAngle = currentTurnAngle;
            }
        }

        private void UpdateBraking()
        {
            if (brakeType == DriveType.AllWheel || brakeType == DriveType.FrontWheel)
            {
                frontLeftWheel.brakeTorque = currentBreakForce;
                frontRightWheel.brakeTorque = currentBreakForce;
            }

            if (brakeType == DriveType.AllWheel || brakeType == DriveType.RearWheel)
            {
                backLeftWheel.brakeTorque = currentBreakForce;
                backRightWheel.brakeTorque = currentBreakForce;
            }
        }

        private void UpdateWheels()
        {
            UpdateWheel(backLeftWheel, backLeftWheelMesh);
            UpdateWheel(backRightWheel, backRightWheelMesh);
            UpdateWheel(frontLeftWheel, frontLeftWheelMesh);
            UpdateWheel(frontRightWheel, frontRightWheelMesh);

            void UpdateWheel(WheelCollider wheel, Transform wheelTransform)
            {
                wheel.GetWorldPose(out var position, out var rotation);
                wheelTransform.SetPositionAndRotation(position, rotation);
            }
        }

        private enum DriveType
        {
            AllWheel,
            FrontWheel,
            RearWheel
        }

        public enum DrivingGear
        {
            Forward,
            Reverse
        }
    }
}