using System;
using System.Collections.Generic;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [SerializeField] private Transform _wallParent;
    [SerializeField] private float _wallThickness = 2f;
    [SerializeField] private float _wallHeight = 10f;
    [SerializeField] private Material _wallMaterial;
    
    private UIManager _uiManager;
    private WallPathManager _wallPathManager;
    
    private void Awake()
    {

        _wallPathManager = new WallPathManager(_wallMaterial, _wallParent, _wallHeight, _wallThickness);
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

