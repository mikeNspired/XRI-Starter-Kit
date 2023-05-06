using UnityEngine;

public class ConnectionPoint : MonoBehaviour
{
    private GameObject connectedObject = null;
    public GameObject ConnectedObject => connectedObject;
    public bool IsConnected => connectedObject != null;

    public void SetConnectedObject(GameObject connector) => connectedObject = connector;
    public void Disconnect() => connectedObject = null;
}