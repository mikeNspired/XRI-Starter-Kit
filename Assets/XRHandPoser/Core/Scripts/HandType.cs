// Copyright (c) MikeNspired. All Rights Reserved.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.UnityXRHandPoser
{
    /// <summary>
    /// Determines if using the left hand or right hand. Will probably be removed next update to make use of XRNode controllerNode.
    /// </summary>
    public class HandType : MonoBehaviour
    {
        public LeftRight type;
    }

    public enum LeftRight
    {
        Left,
        Right
    }
}