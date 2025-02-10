using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    public class BottleTargetSpawner : MonoBehaviour
    {
        public Bottle currentItem;
        [SerializeField] private Bottle prefab = null;
        [SerializeField] private float MinSpawnTime = 0;
        [SerializeField] private float ManSpawnTime = 4;
        private TransformStruct startingPosition;

        public UnityEvent OnBottleBroke;

        void Start()
        {
            startingPosition.SetTransformStruct(currentItem.transform.localPosition, currentItem.transform.localRotation, currentItem.transform.localScale);
            currentItem.onHit.AddListener(BottleDestroyed);
        }

        private void BottleDestroyed(float x)
        {
            OnBottleBroke.Invoke();
            StartCoroutine(CreateNewItem());
        }


        private IEnumerator CreateNewItem()
        {
            yield return new WaitForSeconds(Random.Range(MinSpawnTime, ManSpawnTime));
            currentItem = Instantiate(prefab);
            currentItem.transform.parent = transform;
            currentItem.transform.localPosition = startingPosition.position;
            currentItem.transform.localRotation = startingPosition.rotation;
            currentItem.onHit.AddListener(BottleDestroyed);

        }
    }


}