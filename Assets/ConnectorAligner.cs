using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class ConnectorAligner : MonoBehaviour
{
    [SerializeField] private Transform _connectionPoint = null;
    [SerializeField, ReadOnly] private ConnectionPoint _connectedObject = null;

    [SerializeField] private bool _matchOnlyForwardDirection = false, _maintainConnection;
    [SerializeField] private float _animationSpeed = 0.25f;
    private Coroutine _animationCoroutine;

    public bool IsConnected => _isConnected;
    public ConnectionPoint ConnectedObject => _connectedObject;
    private bool _isConnected = false;
    
    private void OnEnable() => Application.onBeforeRender += OnBeforeRender;
    private void OnDisable() => Application.onBeforeRender -= OnBeforeRender;
    
    [Button()]
    public void Connect()
    {
        _isConnected = true;
        Align();
    }

    [Button()]
    public void Disconnect()
    {
        _connectedObject.Disconnect();
        _connectedObject = null;
        
        _isConnected = false;
    }

    private void Align()
    {
        if (!_isConnected)
            return;

        StopAllCoroutines();

        Quaternion rotation = _matchOnlyForwardDirection ? AlignForwardDirection() : AlignFullRotation();
        Vector3 position = AlignPosition(rotation);

        if (_animationSpeed > 0)
            _animationCoroutine = StartCoroutine(AnimateAlign(rotation, position));
        else
        {
            transform.rotation = rotation;
            transform.position = position;
        }
    }
    [BeforeRenderOrder(102)]
    private void OnBeforeRender()
    {
        if (!_maintainConnection || !_isConnected || _animationCoroutine != null) return;
        
        transform.rotation = _matchOnlyForwardDirection ? AlignForwardDirection() : AlignFullRotation();
        transform.position =  AlignPosition(transform.rotation);;    }
    
    private void Update()
    {
        return;
        if (!_maintainConnection || !_isConnected || _animationCoroutine != null) return;
        
        transform.rotation = _matchOnlyForwardDirection ? AlignForwardDirection() : AlignFullRotation();
        transform.position =  AlignPosition(transform.rotation);;
    }

    private IEnumerator AnimateAlign(Quaternion targetRotation, Vector3 targetPosition)
    {
        Quaternion startRotation = transform.rotation;
        Vector3 startPosition = transform.position;

        float timeElapsed = 0;

        while (timeElapsed < _animationSpeed)
        {
            timeElapsed += Time.deltaTime;

            float t = timeElapsed / _animationSpeed;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        transform.rotation = targetRotation;
        transform.position = targetPosition;
        
        _animationCoroutine = null;
    }

    private Quaternion AlignForwardDirection()
    {
        // Get the forward direction of the connection point in world space
        Vector3 connectionPointForward = _connectionPoint.TransformDirection(Vector3.forward);

        // Calculate the rotation needed to align the connection point's forward direction to the bolt's forward direction
        Quaternion rotation = Quaternion.FromToRotation(connectionPointForward, _connectedObject.transform.forward);

        // Apply the calculated rotation to the transform's rotation
        return rotation * transform.rotation;
    }

    private Quaternion AlignFullRotation()
    {
        // Match the world space rotation of the connection point to the rotation of the bolt
        Quaternion connectionPointWorldRotation = _connectedObject.transform.rotation * Quaternion.Inverse(_connectionPoint.localRotation);

        // Return the matched rotation of the connection point
        return connectionPointWorldRotation;
    }

    private Vector3 AlignPosition(Quaternion quaternion)
    {
        // Temporarily set the transform's rotation to the calculated rotation because the connection point's position is moved after rotating
        var originalRotation = transform.rotation;
        transform.rotation = quaternion;

        // Calculate the position of the transform at the connection point of the bolt
        Vector3 connectionPointToBolt = _connectedObject.transform.position - _connectionPoint.position;
        Vector3 targetPosition = transform.position + connectionPointToBolt;

        // Reset the transform's rotation so it can be animated from its original rotation
        transform.rotation = originalRotation;

        // Return the calculated position
        return targetPosition;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        other.TryGetComponent(out ConnectionPoint connectionPoint);
        if (!connectionPoint || _isConnected) return;
        _connectedObject = connectionPoint;
        Connect();
    }
}