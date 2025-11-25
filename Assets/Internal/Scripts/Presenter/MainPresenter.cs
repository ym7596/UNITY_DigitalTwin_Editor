using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainPresenter : IStartable, IInitializable
{
    private WallGenerator _wallGenerator;
    private UIManager _uiManager;
    private MapEditorManager _mapEditorManager;

    public MainPresenter(UIManager uiManager, MapEditorManager mapEditorManager)
    {
        _uiManager = uiManager;
        _mapEditorManager = mapEditorManager;
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
    }

    public SaveDataModel GetSaveData()
    {
        var wallDatas = _wallGenerator.GetPathData;
        SaveDataModel model = new();
        model.id = "sample";
        model.mapWallPathData = wallDatas;
        return model;
    }
}
