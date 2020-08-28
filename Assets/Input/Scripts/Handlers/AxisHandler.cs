using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

[CreateAssetMenu(fileName = "NewAxisHandler")]
public class AxisHandler : InputHandler, ISerializationCallbackReceiver
{
    public void OnAfterDeserialize()
    {

    }

    public void OnBeforeSerialize() 
    { 

    }
}
