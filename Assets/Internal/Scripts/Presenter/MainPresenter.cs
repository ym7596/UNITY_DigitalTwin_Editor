using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainPresenter : IStartable, IInitializable
{
    private WallGenerator _wallGenerator;
    private UIManager _uiManager;
    private MapEditorManager _mapEditorManager;
    private MapObjectManager _mapObjectManager;

    public MainPresenter(UIManager uiManager, MapEditorManager mapEditorManager, MapObjectManager mapObjectManager)
    {
        _uiManager = uiManager;
        _mapEditorManager = mapEditorManager;
        _mapObjectManager = mapObjectManager;
    }
    
    [Inject]
    public void InitWallCreator(WallGenerator wallGenerator)
    {
        _wallGenerator = wallGenerator;
    }
    
    public void Start()
    {
        
    }

    public void Initialize()
    {
        _uiManager?.SetPresenter(this);  
        _wallGenerator?.SetUIManager(_uiManager);
        _mapEditorManager?.SetPresenter(this);
    }

    public void ActiveRemoteController(bool isCancel)
    {
        _uiManager?.SetOnRemoteController(isCancel);
    }

    public void SelectActionType(MapObjectRemoteActionType actionType)
    {
        _mapEditorManager?.SetActionType(actionType);
    }

    #region Wall
    
    public void GenerateWallPath(List<Vector2> path)
    {
        _wallGenerator?.GenerateWallPath(path);
    }
    
    public void UpdateWallPath(List<Vector2> updatedPath, int pathId)
    {
        _wallGenerator.UpdateWallPath(updatedPath, pathId);
    }

    public void DisableWallPath(List<Vector2> disablePath, int pathId)
    {
        _wallGenerator.DisableWallPath(disablePath, pathId);
    }

    public void CreateWallByLineEditor(List<Vector2> createPath, int pathId)
    {
        _wallGenerator.CreateWallPath(createPath, pathId);
    }

    public void LoadMapData(SaveDataModel model)
    {
        _wallGenerator.LoadWallBySaveData(model.mapWallPathData);
        _mapObjectManager.LoadData(model.mapObjects);
    }
    
    #endregion

    public void CreateObject(string name)
    {
        _mapObjectManager?.Create(name);
    }
    
    public SaveDataModel GetSaveData()
    {
        var wallDatas = _wallGenerator.GetPathData;
        var mapObjectsData = _mapObjectManager.GetAllObjectsData();
        SaveDataModel model = new();
        model.id = "sample";
        model.mapWallPathData = wallDatas;
        model.mapObjects = mapObjectsData;
        return model;
    }
}
