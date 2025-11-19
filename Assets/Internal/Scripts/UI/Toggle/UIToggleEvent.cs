using System;
using UnityEngine;

public class UIToggleEvent : MonoBehaviour
{
    [SerializeField] private DrawActionType _drawMode;

    public event Action<DrawActionType> OnAction_DrawModeChanged;
    
    public void OnToggle_InvokeAction(bool isOn)
    {
        if (isOn == true)
        {
            OnAction_DrawModeChanged?.Invoke(_drawMode);
        }
    }
}
