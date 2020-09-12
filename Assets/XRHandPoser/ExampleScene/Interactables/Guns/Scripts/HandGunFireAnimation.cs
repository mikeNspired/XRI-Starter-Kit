using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandGunFireAnimation : MonoBehaviour
{
    [SerializeField] private GunCocking gunCocking;
    [SerializeField] private float movePositionAnimationTime;

    [SerializeField] private Transform slider;
    [SerializeField] private Transform sliderGoalPosition;
    
    [SerializeField] private Transform keyBangor;
    [SerializeField] private Transform keyBangorOpen;
    private Vector3 keyBangorStartPosition;
    private Quaternion keyBangorStartRotation;

    private void Start()
    {
        keyBangorStartPosition = keyBangor.transform.localPosition;
        keyBangorStartRotation = keyBangor.transform.localRotation;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            StartCoroutine(MoveSlider(slider, sliderGoalPosition));
        }
    }

    private IEnumerator MoveSlider(Transform mover, Transform goalPosition)
    {
        float timer = 0;

        SetKeyBangerClosed();
        while (timer <= movePositionAnimationTime)
        {
            var newPosition = Vector3.Lerp(gunCocking.GetStartPoint(), gunCocking.GetEndPoint(), timer / movePositionAnimationTime);

            mover.localPosition = newPosition;

            timer += Time.deltaTime;
            yield return null;
        }


        SetKeyBangerOpen();
        timer = 0;
        while (timer <= movePositionAnimationTime + Time.deltaTime)
        {
            var newPosition = Vector3.Lerp(gunCocking.GetEndPoint(), gunCocking.GetStartPoint(), timer / movePositionAnimationTime);

            mover.localPosition = newPosition;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator OpenSlider(Transform mover, Transform goalPosition)
    {
        var startingPosition = mover.localPosition;
        float timer = 0;

        SetKeyBangerClosed();
        while (timer <= movePositionAnimationTime + Time.deltaTime)
        {
            var newPosition = Vector3.Lerp(startingPosition, gunCocking.GetEndPoint(), timer / movePositionAnimationTime);

            mover.localPosition = newPosition;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    public void AnimateSliderOnFire() => StartCoroutine(MoveSlider(slider, sliderGoalPosition));

    public void SetSliderOpen()
    {
        gunCocking.Pause();
        StopAllCoroutines();
        StartCoroutine(OpenSlider(slider, sliderGoalPosition));
    }

    public void SetKeyBangerOpen()
    {
        keyBangor.transform.position = keyBangorOpen.transform.position;
        keyBangor.transform.rotation = keyBangorOpen.transform.rotation;
    }

    public void SetKeyBangerClosed()
    {
        keyBangor.transform.localPosition = keyBangorStartPosition;
        keyBangor.transform.localRotation = keyBangorStartRotation;
    }
}