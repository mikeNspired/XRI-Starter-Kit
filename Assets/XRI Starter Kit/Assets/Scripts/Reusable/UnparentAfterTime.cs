// Author MikeNspired. 
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class UnparentAfterTime : MonoBehaviour
    {
        [SerializeField] private bool UnParentOnStart = false;
        [SerializeField] private bool UnParentAfterTimeDelta = true;
        [SerializeField] private float time = 0;

        void Start()
        {
            if (UnParentOnStart)
                SetParentNull();
            else if (UnParentAfterTimeDelta)
                Invoke(nameof(SetParentNull), Time.deltaTime);
            else
                Invoke(nameof(SetParentNull), time);

        }

        private void SetParentNull() => transform.parent = null;


    }
}