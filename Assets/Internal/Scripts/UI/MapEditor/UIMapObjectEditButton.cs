using System;
using UnityEngine;

public class UIMapObjectEditButton : MonoBehaviour
{
    [SerializeField] private MapObjectRemoteActionType _actionType;
    
    public event Action<MapObjectRemoteActionType> OnAction_EditObject;

    public void OnClick()
    {
        OnAction_EditObject?.Invoke(_actionType);
    }
}
