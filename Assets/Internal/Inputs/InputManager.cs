using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private RaycastHit _hit;
    
    public Vector2 PositionValue { get; private set; }
    public event Action<RaycastHit> OnAction_RaycastHit;
    public event Action OnAction_RightClicked;
    
    public void LeftClickAction(InputAction.CallbackContext context)
    {
        bool isUI = PositionValue.IsUITouch();
        if (isUI)
            return;
        bool hasHit = UIUtility.GetRayHit(PositionValue, out _hit);
        if(hasHit)
            OnAction_RaycastHit?.Invoke(_hit);
    }
    
    public void RightClickAction(InputAction.CallbackContext context)
    {
        var phase = context.phase;
        switch (phase)
        {
            case InputActionPhase.Performed:
            {
                OnAction_RightClicked?.Invoke();
            }
                break;
            default:
                break;
        }
    }
    
    public void MousePositionAction(InputAction.CallbackContext context)
    {
        PositionValue = context.ReadValue<Vector2>();
    }
}
