using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIDrawGridLine _drawGridLine;
    
    private MainPresenter _presenter;
    
    void Start()
    {
        
    }

    private void OnEnable()
    {
        _drawGridLine.OnCreateLinePath += WallCreatorEventChain;
    }

    private void OnDisable()
    {
        _drawGridLine.OnCreateLinePath -= WallCreatorEventChain;
    }

    public void SetPresenter(MainPresenter presenter)
    {
        _presenter = presenter;
    }

    private void WallCreatorEventChain(List<Vector2> path)
    {
        _presenter.GenerateWallPath(path);
    }
}
