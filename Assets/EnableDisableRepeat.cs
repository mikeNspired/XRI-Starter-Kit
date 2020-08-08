using UnityEngine;

public class EnableDisableRepeat : MonoBehaviour
{
    [SerializeField] private float activeTime;
    [SerializeField] private float deactiveTime = .25f;
    [SerializeField] private GameObject[] objectToActive = null;

    private float activeTimer;
    private float deactivateTimer;
    private bool isActive, activeForSingleFrame;


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (activeForSingleFrame)
            activeTime = Time.deltaTime;
        activeTimer += Time.deltaTime;
        deactivateTimer += Time.deltaTime;

        if (isActive && activeTimer >= activeTime)
        {
            activeTimer = 0;
            deactivateTimer = 0;
            Deactivate();
        }

        if (!isActive && deactivateTimer >= deactiveTime)
        {
            activeTimer = 0;
            deactivateTimer = 0;
            Activate();
        }
    }

    private void Activate()
    {
        isActive = true;
        foreach (var objToActivate in objectToActive)
        {
            objToActivate.SetActive(true);
        }
    }

    private void Deactivate()
    {
        isActive = false;

        foreach (var objToActivate in objectToActive)
        {
            objToActivate.SetActive(false);
        }
    }
}