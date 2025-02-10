using System;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    [CreateAssetMenu(fileName = "Float SO", menuName = "ScriptableObject/float")]
    public class FloatSO : ScriptableObject<float>
    {

    }

    public abstract class ScriptableObject<T> : ScriptableObject
    {
        [SerializeField] private T value;
        public Action OnValueChanged = delegate { };


        public T GetValue()
        {
            return value;
        }

        public void SetValue(T input)
        {
            value = input;
            OnValueChanged();
        }
    }
}