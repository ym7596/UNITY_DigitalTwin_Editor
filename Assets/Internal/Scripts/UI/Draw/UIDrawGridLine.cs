using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIDrawGridLine : Graphic
{
    [SerializeField] private Camera _uiCamera;
    [SerializeField] private UIDrawPoint _pointPrefab;
    [SerializeField] private RectTransform _drawZone;

    [SerializeField] private float _pointSize = 10f;
    [SerializeField] private float _lineThickness = 10f;
    [SerializeField] private Color _pointColor = Color.red;
    [SerializeField] private Color _lineColor = Color.green;
    [SerializeField] private Color _previewColor = new Color(0, 1, 1, 0.5f);

    private List<List<UIDrawPoint>> _allPoints = new List<List<UIDrawPoint>>();
    private List<List<Vector2>> _allPaths = new();
    private Vector2 _mouseLocalPosition;
    private Vector2 _mouseScreenPosition = Vector2.zero;

    private float _uiScale = 15f;
    public event Action<Vector2, Vector2> OnAction_LineWall;
    public event Action<List<Vector2>> OnCreateLinePath;
    
    
    
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        foreach (var path in _allPaths)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                DrawLine(vh,path[i], path[i+1], _lineThickness, _lineColor);
                OnAction_LineWall?.Invoke(path[i], path[i+1]);
            }
        }

        if (_allPaths.Count > 0 && _allPaths[^1].Count > 0)
        {
            Vector2 last = _allPaths[^1][^1];
            DrawLine(vh,last,_mouseLocalPosition, _lineThickness, _previewColor);
        }
    }

    private void CreatePoint(Vector2 anchoredPosition, Color color)
    {
        var point = Instantiate(_pointPrefab, transform);
        point.rectTransform.anchoredPosition = anchoredPosition;
        point.color = color;
        point.SetAllDirty();
    }
    void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
            _mouseScreenPosition, _uiCamera, out _mouseLocalPosition);
        SetVerticesDirty();
    }

    private bool IsInDrawZone(RectTransform drawZone, Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(drawZone, screenPos, _uiCamera);
    }

    public void SetPathsData(List<List<Vector2>> paths, Vector3 medianPosition)
    {  
        _allPaths.Clear();
        
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        if (paths == null || paths.Count == 0)
            return;
        
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (var path in paths)
        {
            if (path == null) continue;

            foreach (var p in path)
            {
                if (p.x < min.x) min.x = p.x;
                if (p.y < min.y) min.y = p.y;
                if (p.x > max.x) max.x = p.x;
                if (p.y > max.y) max.y = p.y;
            }
        }

        Vector2 boundsCenter = (min + max) * 0.5f;
        
        foreach (var path in paths)
        {
            if (path == null || path.Count == 0)
                continue;

            var newPath = new List<Vector2>();

            foreach (var p in path)
            {
                Vector2 localPos = (p - boundsCenter) * _uiScale;

                newPath.Add(localPos);
                CreatePoint(localPos, _pointColor);
            }

            _allPaths.Add(newPath);
        }

        SetVerticesDirty();
    }

    public void OnMousePosition(InputAction.CallbackContext context)
    {
        var phase = context.phase;

        switch (phase)
        {
            case InputActionPhase.Performed:
            {
                _mouseScreenPosition = context.ReadValue<Vector2>();
            }
                break;
            default:
                break;
        }
    }

    public void OnLeftButtonClicked(InputAction.CallbackContext context)
    {
        var phase = context.phase;

        if (IsInDrawZone(_drawZone, _mouseScreenPosition) == false)
            return;

        switch (phase)
        {
            case InputActionPhase.Performed:
            {
                if (_allPaths.Count == 0 || _allPaths[^1].Count == 0)
                {
                    _allPaths.Add(new List<Vector2>()); // 새 경로 시작
                }

                _allPaths[^1].Add(_mouseLocalPosition);
                CreatePoint(_mouseLocalPosition, _pointColor);

                if (_allPaths[^1].Count >= 2)
                {
                    var path = _allPaths[^1];
                    OnCreateLinePath?.Invoke(path);
                }
                
                SetVerticesDirty();
            }
                break;
            case InputActionPhase.Canceled:
            {
            }
                break;
            default: break;
        }
    }

    public void OnRightButtonClicked(InputAction.CallbackContext context)
    {
        var phase = context.phase;
     
        switch (phase)
        {
            case InputActionPhase.Performed:
            {
                if (_allPaths.Count == 0 || _allPaths[^1].Count > 0)
                {
                    _allPaths.Add(new List<Vector2>());
                }
                SetVerticesDirty();
            }
                break;
        }
    }
  
    private void DrawLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 dir = (end - start).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * (thickness / 2f);

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        v.position = start - normal;
        vh.AddVert(v);
        v.position = start + normal;
        vh.AddVert(v);
        v.position = end + normal;
        vh.AddVert(v);
        v.position = end - normal;
        vh.AddVert(v);

        int startIndex = vh.currentVertCount - 4;
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}
