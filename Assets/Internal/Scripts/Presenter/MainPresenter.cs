using System;
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
    private MapObjectHistoryController _historyController;
    private HeatmapController _heatmapController;
    private IDataPresenterHandler _dataPresenterHandler;

    public MainPresenter(UIManager uiManager, MapEditorManager mapEditorManager,
        MapObjectManager mapObjectManager, IDataPresenterHandler dataPresenterHandler, MapObjectHistoryController historyController)
    {
        _uiManager = uiManager;
        _mapEditorManager = mapEditorManager;
        _mapObjectManager = mapObjectManager;
        _dataPresenterHandler = dataPresenterHandler;
        _historyController = historyController;
    }
    
    [Inject]
    public void InitWallCreator(WallGenerator wallGenerator)
    {
        _wallGenerator = wallGenerator;
    }
    
    [Inject]
    public void InitHeatmapController(HeatmapController heatmapController)
    {
        _heatmapController = heatmapController;
    }
    
    public void Start()
    {
        if (_dataPresenterHandler != null)
        {
            _dataPresenterHandler.OnAction_DataPresenterResponse -= OnAPIResponse;
            _dataPresenterHandler.OnAction_DataPresenterResponse += OnAPIResponse;
        }
    }

    public void Initialize()
    {
        _uiManager?.SetPresenter(this);  
        _wallGenerator?.SetUIManager(_uiManager);
        _mapEditorManager?.SetPresenter(this, _historyController);
    }

    public void ActiveRemoteController(bool isCancel)
    {
        _uiManager?.SetOnRemoteController(isCancel);
    }

    public void SelectActionType(MapObjectRemoteActionType actionType)
    {
        _mapEditorManager?.SetActionType(actionType);
    }
    
    public void Undo()
    {
        _historyController.Undo();
    }

    public void Redo()
    {
        _historyController.Redo();
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

    private void OnAPIResponse(APICategory category, Enum apiType, object payload)
    {
        switch (category)
        {
            case APICategory.Common:
            {
                var type = (CommonEndPoint) apiType;
                Debug.Log($"API Response => {type} : {payload}");
                var model = (SaveDataModel) payload;
                LoadMapData(model);
                break;
            }
            case APICategory.Map:
            {
                var type = (MapEndPoint) apiType;
                switch (type)
                {
                    case MapEndPoint.Heatmap:
                    {
                        var json = (HeatMapDataList) payload;
                        _heatmapController.GenerateHeatmap(json);
                        break;
                    }
                }
                break;
            }
        }
    }
}
