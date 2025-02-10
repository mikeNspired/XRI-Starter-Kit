using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class TankShooting : MonoBehaviour
    {
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public AudioSource m_ShootingAudio;
        public AudioClip m_FireClip;
        public float m_launchForce = 15;
        public float m_launchPushbackForce = 15;

        public void FireWeapon()
        {
            Rigidbody shellInstance =
                Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            shellInstance.linearVelocity = m_launchForce * m_FireTransform.forward;

            m_ShootingAudio.PlayOneShot(m_FireClip);

            GetComponent<Rigidbody>().AddForce(transform.forward * -m_launchPushbackForce);
        }
    }
}