using System;
using UnityEngine;
using UnityEngine.Events;

namespace MikeNspired.XRIStarterKit
{
    [Serializable]
    public class UnityEventFloat : UnityEvent<float>
    {
    }

    [Serializable]
    public class UnityEventVector2 : UnityEvent<Vector2>
    {
    }

    [Serializable]
    public class UnityEventInt : UnityEvent<int>
    {
    }
}