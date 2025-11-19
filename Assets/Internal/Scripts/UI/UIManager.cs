using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIDrawEventBus _drawEventBus;
 
    [SerializeField] private GameObject _drawPanel;
    private MainPresenter _presenter;


    private void Awake()
    {
        _drawEventBus.UpdatePathDelegate(OnUpdateWallPath);
        _drawEventBus.CreatePathDelegate(OnCreateWallPath);
        _drawEventBus.DisablePathDelegate(OnDisablePath);
    }

    private void OnEnable()
    {
      //  _drawGridLine.OnCreateLinePath += WallCreatorEventChain;
    }

    private void OnDisable()
    {
      //  _drawGridLine.OnCreateLinePath -= WallCreatorEventChain;
    }

    public void SetPresenter(MainPresenter presenter)
    {
        _presenter = presenter;
    }

    private void WallCreatorEventChain(List<Vector2> path)
    {
        _presenter.GenerateWallPath(path);
    }
    
    public void ShowDrawPanel(bool isOn)
    {
        _drawPanel.SetActive(isOn);
    }
    
    #region Draw UI

    private void OnCreateWallPath(List<Vector2> path, int pathIndex)
    {
        _presenter?.CreateWallByLineEditor(path, pathIndex);  
    }
        
    private void OnUpdateWallPath(List<Vector2> path, int pathIndex)
    {
        _presenter?.UpdateWallPath(path, pathIndex);
    }

    private void OnDisablePath(List<Vector2> path, int pathIndex)
    {
        _presenter?.DisableWallPath(path, pathIndex);
    }

    public void ResetLineVertex()
    {
        _drawEventBus.ResetLineAndVertex();
    }

    public void SetPathData(List<List<Vector2>> pathData, Vector2 medianPosition)
    {
        _drawEventBus.SetPathData(pathData,medianPosition);
    }

    #endregion
}
