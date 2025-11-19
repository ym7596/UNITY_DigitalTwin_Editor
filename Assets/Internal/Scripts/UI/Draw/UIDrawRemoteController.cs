using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIDrawRemoteController : MonoBehaviour
{
    [SerializeField] private ToggleGroup _toggleGroup;
    [SerializeField] private List<UIToggleEvent> _toggles;
    
    private event SetDrawActionTypeDelegate OnToggleDrawTypeChanged;
    private event DeletePointUIEventDelegate OnButtonDeletePoint;
    private void Start()
    {
        foreach (var tgl in _toggles)
        {
            tgl.OnAction_DrawModeChanged += OnToggleChanged;
        }
    }

    public void SetDrawActionDelegate(SetDrawActionTypeDelegate setDrawActionTypeDelegate, DeletePointUIEventDelegate deletePointUIEventDelegate)
    {
        OnToggleDrawTypeChanged = setDrawActionTypeDelegate;
        OnButtonDeletePoint = deletePointUIEventDelegate;
    }
    
    public void OnToggleChanged(DrawActionType type)
    {
        OnToggleDrawTypeChanged?.Invoke(type);
    }

    public void OnButton_DeletePoint()
    {
        OnButtonDeletePoint?.Invoke();
    }
}
