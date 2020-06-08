using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private GameObject decalPrefab = null;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody?.GetComponent<Bullet>()) return;
            collision.rigidbody?.GetComponent<IDamageable>()?.TakeDamage(10);
            SpawnDecal(collision);
            Destroy(this.gameObject);
        }

        void SpawnDecal(Collision hit)
        {

            ContactPoint contact = hit.contacts[0];
            GameObject spawnedDecal = Instantiate(decalPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            spawnedDecal.transform.SetParent(hit.collider.transform);
        }
    }
}