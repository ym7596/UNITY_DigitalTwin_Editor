using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainPresenter : IStartable, IInitializable
{
    private WallGenerator _wallGenerator;
    private UIManager _uiManager;

    public MainPresenter(UIManager uiManager)
    {
        _uiManager = uiManager;
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
}
