using System;
using System.Collections.Generic;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [SerializeField] private WallDataSO _wallDataSO;
    [SerializeField] private Transform _wallParent;

    private UIManager _uiManager;
    private WallPathManager _wallPathManager;
    
    private void Awake()
    {
        _wallPathManager = new WallPathManager(_wallDataSO.wallMaterial, _wallParent, _wallDataSO.wallHeight, _wallDataSO.wallThickness);
    }

    public void SetUIManager(UIManager uiManager)
    {
        _uiManager = uiManager;
    }

    public void GenerateWallPath(List<Vector2> path)
    {
        if (path == null || path.Count < 2) return;
        
        _wallPathManager.CreateWallByLineEditor(path);

        _wallPathManager.FixAllIntersections();
    }
}

