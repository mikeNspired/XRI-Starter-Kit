using UnityEngine;

namespace Complete
{
    public class TankMovement : MonoBehaviour
    {
        public float m_Speed = 12f, m_TurnSpeed = 180f;
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling, m_EngineDriving;
        public float m_PitchRange = 0.2f;

        private Rigidbody m_Rigidbody;
        private float m_MovementInputValue, m_TurnInputValue;
        private float m_OriginalPitch, m_StartingVolume;
        private ParticleSystem[] m_particleSystems;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_OriginalPitch = m_MovementAudio.pitch;
        }

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
            // When the tank is turned off, set it to kinematic so it stops moving.
            m_Rigidbody.isKinematic = true;

            // Stop all particle system so it "reset" it's position to the actual one instead of thinking we moved when spawning
            foreach (var particle in m_particleSystems) particle.Stop();
        }

        private void Update()
        {
            EngineAudio();
        }

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
                m_MovementAudio.volume = m_StartingVolume * .9f;

                // ... and if the audio source is currently playing the driving clip...
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    // ... change the clip to idling and play it.
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                // Otherwise if the tank is moving and if the idling clip is currently playing...
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    // ... change the clip to driving and play.
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
                m_MovementAudio.volume = Mathf.Lerp(m_StartingVolume * .5f, m_StartingVolume * 2, m_MovementInputValue);
            }
        }


        public void Print(float speed) => Debug.Log(speed);
        public void SetSpeed(float speed) => m_MovementInputValue = speed;
        public void SetSpeed2() => m_MovementInputValue = 1;

        public void SetDirection(float direction) => m_TurnInputValue = direction;

        private void Move()
        {
            Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;

            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }

        private void Turn()
        {
            float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

            // Make this into a rotation in the y axis.
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }
    }
}