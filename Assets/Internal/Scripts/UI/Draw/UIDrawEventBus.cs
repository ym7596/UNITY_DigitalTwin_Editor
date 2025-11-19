using System.Collections.Generic;
using UnityEngine;

public class UIDrawEventBus : MonoBehaviour
{
    [SerializeField] private UIDrawGridLine _drawGridLine;
    [SerializeField] private UIDrawRemoteController _drawRemoteController;
    [SerializeField] private RectTransform _drawPanel;
 
    private void Awake()
    {
        _drawRemoteController?.SetDrawActionDelegate(SetDrawActionType, DeletePointAction);
    }

    public void ResetLineAndVertex()
    {
        _drawGridLine.ClearAll();
    }

    public void SetPathData(List<List<Vector2>> pathData, Vector2 medianPosition)
    {
        _drawGridLine.SetPathsData(pathData, medianPosition);
    }

    public void UpdatePathDelegate(UpdateLinePathDelegate updateDelegate)
    {
        _drawGridLine.SetUpdateLineEvent(updateDelegate);
    }

    public void DisablePathDelegate(DisableWallPathDelegate disableDelegate)
    {
        _drawGridLine.SetUpDisablePathEvent(disableDelegate);
    }

    public void CreatePathDelegate(CreateLinePathDele createDelegate)
    {
        _drawGridLine.SetUpLineCreateEvent(_drawPanel, createDelegate);
    }
    
    private void SetDrawActionType(DrawActionType drawActionType) => _drawGridLine.SetDrawActionType(drawActionType);

    private void DeletePointAction()
    {
        _drawGridLine.RemovePoint();
    }
}
