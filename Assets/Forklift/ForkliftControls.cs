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
            return;
        }

        rotationRigidBody.SetDriveTarget(ArticulationDriveAxis.X, GetDriveTargetValue(rotationRigidBody.xDrive, tiltValue * tiltSpeed));

        if (CheckIfDriveLimit(rotationRigidBody.xDrive))
            tiltAudio.Stop();
        else
            tiltAudio.Play(tiltValue);
    }

    private void Lift(float liftValue)
    {
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
            liftAudio.Play(liftValue);


        void Raise()
        {
            forksRigidBody.SetDriveTarget(ArticulationDriveAxis.Z, GetDriveTargetValue(forksRigidBody.zDrive, liftValue * liftSpeed));
            if (forks.localPosition.z > forksMaxHeight)
                forkRiser.SetParent(forksRigidBody.transform);
        }

        void Lower()
        {
            forksRigidBody.SetDriveTarget(ArticulationDriveAxis.Z, GetDriveTargetValue(forksRigidBody.zDrive, liftValue * liftSpeed));
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