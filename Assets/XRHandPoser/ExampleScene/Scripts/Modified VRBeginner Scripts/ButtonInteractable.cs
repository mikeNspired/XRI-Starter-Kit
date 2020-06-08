using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : MonoBehaviour
{
    [System.Serializable]
    public class ButtonPressedEvent : UnityEvent
    {
    }

    [System.Serializable]
    public class ButtonReleasedEvent : UnityEvent
    {
    }

    public Vector3 Axis = new Vector3(0, -1, 0);
    public float MaxDistance;
    public float ReturnSpeed = 10.0f;
    public float pauseBetweenCanClickAgain = 1f;

    public AudioClip ButtonPressAudioClip;
    public AudioClip ButtonReleaseAudioClip;
    public AudioSource audioSource;
    public ButtonPressedEvent OnButtonPressed;
    public ButtonReleasedEvent OnButtonReleased;

    Vector3 m_StartPosition;
    Rigidbody m_Rigidbody;
    Collider m_Collider;
    private float pauseTimer;
    bool m_Pressed = false;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Collider = GetComponentInChildren<Collider>();
        m_StartPosition = transform.position;
    }

    private Collider hitColliderPushingButton;
    public bool isStillBeingPressed = false;

    void FixedUpdate()
    {
        pauseTimer += Time.deltaTime;
        
        Vector3 worldAxis = transform.TransformDirection(Axis);
        Vector3 end = transform.position + worldAxis * MaxDistance;

        float m_CurrentDistance = (transform.position - m_StartPosition).magnitude;
        RaycastHit info;

        float move = 0.0f;

        m_Rigidbody.SweepTest(-worldAxis, out info, ReturnSpeed * Time.deltaTime + 0.005f);
        
        
        if (info.collider)
        {
       
            move = (ReturnSpeed / 3 * Time.deltaTime) - info.distance;
        }
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, .04f);
        foreach (var col in hitColliders)
        {
            if (col == hitColliderPushingButton)
            {
                isStillBeingPressed = true;
                break;
            }
            isStillBeingPressed = false;
            
        }
        if(info.collider == null)
        {
           
            if (!isStillBeingPressed)
                move -= ReturnSpeed * Time.deltaTime;
        }

        float newDistance = Mathf.Clamp(m_CurrentDistance + move, 0, MaxDistance);

        m_Rigidbody.position = m_StartPosition + worldAxis * newDistance;



        if (!m_Pressed && Mathf.Approximately(newDistance, MaxDistance))
        {
            if (pauseTimer < pauseBetweenCanClickAgain) return;
            //was just pressed
            pauseTimer = 0;
            hitColliderPushingButton = info.collider;
            m_Pressed = true;
            audioSource.clip = ButtonPressAudioClip;
            audioSource.Play();
            OnButtonPressed.Invoke();
        }
        else if (m_Pressed && !Mathf.Approximately(newDistance, MaxDistance))
        {
            //was just released
            m_Pressed = false;
            audioSource.clip = ButtonReleaseAudioClip;
            audioSource.Play();
            OnButtonReleased.Invoke();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Handles.DrawLine(transform.position, transform.position + transform.TransformDirection(Axis).normalized * MaxDistance);
    }
#endif
}