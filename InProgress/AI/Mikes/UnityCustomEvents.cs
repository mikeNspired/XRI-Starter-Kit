using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class UnityCustomEvents
{
    [System.Serializable]
    public class UnityEventInt : UnityEvent<int>
    {
    }

    [System.Serializable]
    public class UnityEventFloat : UnityEvent<float>
    {
    }
    [System.Serializable]
    public class UnityEventString : UnityEvent<string>
    {
    }

    [System.Serializable]
    public class UnityEventGameObject : UnityEvent<GameObject>
    {
    }

    [System.Serializable]
    public class UnityEventTransform : UnityEvent<Transform>
    {
    }

    [System.Serializable]
    public class UnityEventVector3 : UnityEvent<Vector3>
    {
    } 
    [System.Serializable]
    public class UnityEventVector2 : UnityEvent<Vector3>
    {
    }

    [System.Serializable]
    public class UnityEventQuaternion : UnityEvent<Quaternion>
    {
    }

    [System.Serializable]
    public class UnityEventCollision : UnityEvent<Collision>
    {
    }
    [System.Serializable]
    public class UnityEventRayCastHit : UnityEvent<RaycastHit>
    {
    }
    ////---------------------UNITYXR SPECIFIC-----------------------\\\\

 
    ////---------------------GAME SPECIFIC-----------------------\\\\



}