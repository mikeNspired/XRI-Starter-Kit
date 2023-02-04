using System;
using UnityEngine;

public class ForkliftControls : MonoBehaviour
{
    [SerializeField] private ThreePartAudio liftAudio, tiltAudio;
    [SerializeField] private Transform forkRiser, forks;
    [SerializeField] private Vector2 rotateAngleMinMax = new Vector2(-5, 5);
    [SerializeField] private float tiltSpeed = 5f, liftSpeed = .5f;
    [SerializeField] private float forkRiserMaxHeight, forksMaxHeight;
    private Vector3 forkRiserBaseStartingRotation;
    private float rotatedAmount;
    public ArticulationBody forksRigidBody, rotationRigidBody;
    public bool useKeyboard;

    private Transform forkRiserParent;

    private void Start()
    {
        forkRiserParent = forkRiser.parent;
    }

    public void UpdateLift(float speed) => Lift(speed);
    public void UpdateTilt(float speed) => Tilt(speed);

    private void Update()
    {
        if (!useKeyboard) return;
        if (Input.GetKey(KeyCode.Alpha1))
            Lift(1);
        else if (Input.GetKey(KeyCode.Q))
            Lift(-1);
        else Lift(0);

        if (Input.GetKey(KeyCode.Alpha3))
            Tilt(-1);
        else if (Input.GetKey(KeyCode.E))
            Tilt(1);
        else Tilt(0);
    }


    private void Tilt(float tiltValue)
    {
        if (Mathf.Abs(tiltValue) < .1f)
        {
            tiltAudio.Stop();
            rotationRigidBody.SetDriveTargetVelocity(ArticulationDriveAxis.X, 0);
            return;
        }

        rotationRigidBody.SetDriveTargetVelocity(ArticulationDriveAxis.X, tiltValue * tiltSpeed);

        // var newRotationValue = rotatedAmount += tiltValue * tiltSpeed * Time.deltaTime;
        // rotatedAmount = Mathf.Clamp(newRotationValue, rotateAngleMinMax.x, rotateAngleMinMax.y);
        // //forkRiserBase.localEulerAngles = new Vector3(forkRiserBaseStartingRotation.x + rotatedAmount, forkRiserBaseStartingRotation.y, forkRiserBaseStartingRotation.z);
        //
        // if (newRotationValue >= rotateAngleMinMax.x && newRotationValue <= rotateAngleMinMax.y)
        //     tiltAudio.Play(tiltValue);
        // else
        //     tiltAudio.Stop();
    }

    private void Lift(float liftValue)
    {
        if (Mathf.Abs(liftValue) < .1f)
        {
            liftAudio.Stop();
            MoveObjectPositions(forksRigidBody, 0);
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

        void Raise()
        {
            MoveObjectPositions(forksRigidBody, liftValue);
            if (forks.localPosition.z > forksMaxHeight)
                forkRiser.SetParent(forksRigidBody.transform);
        }

        void Lower()
        {
            MoveObjectPositions(forksRigidBody, liftValue);
            if (!(forks.localPosition.z <= forksMaxHeight) || forkRiser.parent == forkRiserParent) return;
            forkRiser.SetParent(forkRiserParent);
            forkRiser.localPosition = Vector3.zero;
        }
    }

    public float maxVelocity;
    private void MoveObjectPositions(ArticulationBody movingPart, float liftValue)
    {
        var liftAmount = liftValue * liftSpeed;
        movingPart.maxJointVelocity = maxVelocity;
        movingPart.SetDriveTargetVelocity(ArticulationDriveAxis.Z, -liftAmount);
    }
}