using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    /// <summary>
    /// An asset that represents a key. Used to check if an object can perform some action
    /// (<see cref="XRClosedSocketInteractor"/> and <see cref="Keychain"/>)
    /// </summary>
    [CreateAssetMenuAttribute(menuName = "XR/Key Lock System/Key")]
    public class Key : ScriptableObject
    { }
}
