using UnityEngine;

public class TankMovement : MonoBehaviour
{
    public float m_Speed = 12f, m_TurnSpeed = 180f, m_idlePitch;
    public AudioSource m_MovementAudio;

    private Rigidbody m_Rigidbody;
    private float m_MovementInputValue, m_TurnInputValue, m_StartingVolume;
    private ParticleSystem[] m_particleSystems;

    private void Awake() => m_Rigidbody = GetComponent<Rigidbody>();

    private void OnEnable()
    {
        m_Rigidbody.isKinematic = false;
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;
        m_StartingVolume = m_MovementAudio.volume;
        m_particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (var particle in m_particleSystems)
            particle.Play();
    }

    private void OnDisable()
    {
        m_Rigidbody.isKinematic = true;
        foreach (var particle in m_particleSystems) particle.Stop();
    }

    private void Update() => EngineAudio();

    private void FixedUpdate()
    {
        Move();
        Turn();
    }

    private void EngineAudio()
    {
        // If there is no input (the tank is stationary)...
        if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
        {
            m_MovementAudio.volume = m_StartingVolume * .5f;
            m_MovementAudio.pitch = m_idlePitch;
        }
        else
        {
            m_MovementAudio.volume = Mathf.Lerp(m_StartingVolume * .5f, m_StartingVolume * 2, Mathf.Abs(m_MovementInputValue));
            m_MovementAudio.pitch = Mathf.Lerp(m_idlePitch, m_idlePitch * 1.5f, Mathf.Abs(m_MovementInputValue));
        }
    }

    public void SetSpeed(float speed) => m_MovementInputValue = speed;

    public void SetDirection(float direction) => m_TurnInputValue = direction;

    private void Move()
    {
        Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;
        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }

    private void Turn()
    {
        if (m_MovementInputValue < .1f) return;
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

        // Make this into a rotation in the y axis.
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }
}