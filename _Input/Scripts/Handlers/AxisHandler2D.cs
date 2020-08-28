using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

[CreateAssetMenu(fileName = "NewAxisHandler2D")]
public class AxisHandler2D : InputHandler, ISerializationCallbackReceiver
{
    public void OnAfterDeserialize()
    {

    }

    public void OnBeforeSerialize()
    {

    }

    public override void HandleState(XRController controller)
    {

    }

    public Vector2 GetValue(XRController controller)
    {
        return Vector2.zero;
    }
}
