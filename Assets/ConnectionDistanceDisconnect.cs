using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ConnectionDistanceDisconnect : MonoBehaviour
{
    [SerializeField] private ConnectorAligner connectorAligner;
    [SerializeField] private float _maxDistance = 0.1f;
    
    private XRGrabInteractable _interactable;
    private XRDirectInteractor _directInteractor;
    private void Awake()
    {
        OnValidate();
        _interactable.selectEntered.AddListener(OnSelect);
    }

    private void OnValidate()
    {
        if (!_interactable) _interactable = GetComponent<XRGrabInteractable>();
        if (!connectorAligner) connectorAligner = GetComponent<ConnectorAligner>();
    }

    private void OnSelect(SelectEnterEventArgs arg0)
    {
        _directInteractor = arg0.interactorObject.transform.GetComponent<XRDirectInteractor>();
    }

    private void Update()
    {

        if (!_directInteractor || !connectorAligner.IsConnected) return;
        if (Vector3.Distance(_directInteractor.transform.position, connectorAligner.ConnectedObject.transform.position) > _maxDistance)
        {
            Debug.Log("Disconnected");
            connectorAligner.Disconnect();
        }
    }
}