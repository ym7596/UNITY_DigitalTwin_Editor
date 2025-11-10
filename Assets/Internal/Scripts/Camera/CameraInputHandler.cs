using UnityEngine;
using UnityEngine.InputSystem;

public class CameraInputHandler : MonoBehaviour
{
    [SerializeField] private OrbitCameraController _cameraController;

   
    private bool _isRotating = false;
    private Vector2 _moveInput = Vector2.zero;
    private Vector2 _rotationInput = Vector2.zero;
    private float _zoomInput = 0f;

    private void Update()
    {
        // 매 프레임 입력 적용
        if (_moveInput != Vector2.zero)
        {
            _cameraController.SetPosition(_moveInput);
        }

        if (_isRotating && _rotationInput != Vector2.zero)
        {
            _cameraController.SetRotation(_rotationInput);
        }

        if (_zoomInput != 0f)
        {
            _cameraController.SetZoom(_zoomInput);
        }
    }

    #region Input Actions

    /// <summary>
    /// WASD 이동 입력
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// 마우스 중간 버튼 (회전 활성화)
    /// </summary>
    public void OnRotateButton(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _isRotating = true;
        }
        else if (context.canceled)
        {
            _isRotating = false;
            _rotationInput = Vector2.zero;
        }
    }

    /// <summary>
    /// 마우스 델타 (회전)
    /// </summary>
    public void OnRotate(InputAction.CallbackContext context)
    {
        if (_isRotating)
        {
            _rotationInput = context.ReadValue<Vector2>();
        }
    }

    /// <summary>
    /// 마우스 휠 (줌)
    /// </summary>
    public void OnZoom(InputAction.CallbackContext context)
    {
        var zoom = context.ReadValue<Vector2>();
        Debug.Log(zoom);
        _zoomInput = zoom.y;
    }

    /// <summary>
    /// 카메라 리셋 (예: R키)
    /// </summary>
    public void OnResetCamera(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _cameraController.ResetCamera();
        }
    }

    #endregion
}

