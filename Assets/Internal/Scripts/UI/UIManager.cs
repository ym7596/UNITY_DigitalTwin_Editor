using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIDrawEventBus _drawEventBus;
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private GameObject _drawPanel;
    private MainPresenter _presenter;


    private void Awake()
    {
        _drawEventBus.UpdatePathDelegate(OnUpdateWallPath);
        _drawEventBus.CreatePathDelegate(OnCreateWallPath);
        _drawEventBus.DisablePathDelegate(OnDisablePath);
    }

    #region SaveLoad Event

    public void OnClick_Save()
    {
        var data = _presenter.GetSaveData();
        _saveManager.SaveToJson(data);
    }

    public void OnClick_Load()
    {
      _ = Load();   
    }

    private async UniTask Load()
    {
        var loadData = await _saveManager.LoadMapDataAsync(LoadLocationType.StreamingAssets);
        _presenter.LoadMapData(loadData);
    }
    
    #endregion

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
