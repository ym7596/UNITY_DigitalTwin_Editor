using Unity.Cinemachine;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCameraController : MonoBehaviour
{   
    public Transform RootTransform => _rootTransform;
    public Transform CameraTransform => _cameraTransform;
    public bool IsAnimating => _rotationCoroutine != null;
    
    public event Action OnAnimationCompleted;
    [SerializeField] protected Transform _cameraTransform;
        
    [Space] 
    [Header("[Move]")] 
    [Tooltip("Move Speed per second.")]
    public float moveSpeed = 5f;
        
    [Header("[Rotation]")]
    [Tooltip("Rotation Speed per second.")]
    public float rotationSpeed = 30.0f;
    public LimitValue verticalAngleLimit = new LimitValue(30, 90);
        
    [Header("[Zoom]")]
    [Tooltip("Zoom Speed per second.")]
    public float zoomSpeed = 50.0f;
    public LimitValue zoomDistanceLimit = new LimitValue(5f, 20f);
        
    [Space]
    [Header("[Trigger]")]
    public float triggeredRotationTime = 0.2f;

    private Transform _rootTransform;
    private Transform _trackingTarget;
    private TransformData _baseTransformData;
    private Coroutine _rotationCoroutine;
    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        // RootTransform이 없으면 자동 생성
        if (_rootTransform == null)
        {
            GameObject rootObj = new GameObject("CameraRoot");
            _rootTransform = rootObj.transform;
            _rootTransform.position = Vector3.zero;
            _rootTransform.rotation = Quaternion.Euler(45f, 0f, 0f);
        }

        // CameraTransform이 없으면 현재 Transform 사용
        if (_cameraTransform == null)
        {
            _cameraTransform = transform;
        }

        // 카메라를 루트의 자식으로 설정
        if (_cameraTransform.parent != _rootTransform)
        {
            _cameraTransform.SetParent(_rootTransform);
            _cameraTransform.localPosition = new Vector3(0f, 0f, -20f);
            _cameraTransform.localRotation = Quaternion.identity;
        }
    }
    public void SetTrackingTarget(Transform target)
    {
        _rootTransform.SetParent(target);
            
        if (target)
            _rootTransform.localPosition = Vector3.zero;
            
        _trackingTarget = target;
    }
    
    public void SetPosition(Vector2 moveValue)
    {
        if (moveValue == Vector2.zero)
            return;
        if(_trackingTarget)
            SetTrackingTarget(null);
            
        var localForward = _rootTransform.forward;

        if (localForward == -Vector3.up)
            localForward = _rootTransform.up;
            
        localForward *= moveValue.y;
            
        var localRight = _rootTransform.right * moveValue.x;
        var localMoveDirection = localForward + localRight;
        localMoveDirection.y = 0;

        if (localMoveDirection != Vector3.zero)
            localMoveDirection.Normalize();
            
        _rootTransform.position += localMoveDirection * (moveSpeed * Time.deltaTime);
    }

    public void SetRotation(Vector2 rotationDelta)
    {
        if (_rootTransform == false || _rotationCoroutine != null)
            return;

        var rot = _rootTransform.localEulerAngles;
        if (rot.x > 180f) rot.x -= 360f;
        var rotation = rotationSpeed * Time.deltaTime;

        if (rotationDelta.y != 0)
        {
            rot.x += rotationDelta.y * rotation;
            rot.x = Mathf.Clamp(rot.x, verticalAngleLimit.min, verticalAngleLimit.max);
        }
            
        if(rotationDelta.x != 0)
            rot.y += rotationDelta.x * rotation;
            
        rot.z = 0f;
            
        _rootTransform.localEulerAngles = rot;
    }

    public void SetZoom(float zoomDelta)
    {
        if (zoomDelta == 0f)
            return;
            
        var cameraTransform = _cameraTransform.transform;
       
        var dir = zoomDelta > 0 ? 1 : -1;
       
        cameraTransform.localPosition += Vector3.forward * ( dir * zoomSpeed * Time.deltaTime);
            
        var distance = Vector3.Distance(cameraTransform.position, _rootTransform.position);
            
        if (distance > zoomDistanceLimit.max)
            cameraTransform.localPosition = Vector3.back * zoomDistanceLimit.max;
        else if (distance < zoomDistanceLimit.min)
            cameraTransform.localPosition = Vector3.back * zoomDistanceLimit.min;
    }
    
    public void ResetCamera()
    {
        _rootTransform.localEulerAngles = new Vector3(45f, 0f, 0f);
        _cameraTransform.localPosition = new Vector3(0f, 0f, -20f);
    }

    /// <summary>
    /// 특정 위치로 카메라 이동
    /// </summary>
    public void FocusOnPosition(Vector3 worldPosition)
    {
        if (_trackingTarget)
            SetTrackingTarget(null);
        
        _rootTransform.position = worldPosition;
    }
}


public struct TransformData
{
    public Vector3 position;
    public Vector3 rotation;
    public float zoom;
}

[Serializable]
public struct LimitValue
{
    public LimitValue(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
        
    [Range(0, 360.0f)]
    public float min;
    [Range(0, 1000.0f)]
    public float max;
}