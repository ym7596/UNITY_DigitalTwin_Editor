using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public delegate void PointMoveHandler(UIDrawPoint point, Vector2 newPosition);
public delegate void PointSelectedHandler(UIDrawPoint point);
public delegate void CreateLinePathDele(List<Vector2> path, int pathIndex);
public delegate void UpdateLinePathDelegate(List<Vector2> path, int pathIndex);
public delegate void DeletePointUIEventDelegate();
public delegate void SetDrawActionTypeDelegate(DrawActionType drawActionType);
public delegate void DisableWallPathDelegate(List<Vector2> path, int pathIndex);
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

    private WallGraphData _wallGraph = new WallGraphData();
    private DrawEdgePath _prevEdgePath;
    private bool _shouldShowPreviewText = false;
    private UIDrawPoint _prevVertexPoint = null; // 현재 선택된 정점
    private Vector2 _previewStart;
    private Vector2 _previewEnd;
    
    private DrawActionType _drawActionType = DrawActionType.None;
    private Vector2 _medianValue = Vector2.zero;
    private readonly HashSet<int> _pathsRequireMedianOnExport = new HashSet<int>();

    private List<List<UIDrawPoint>> _allPoints = new List<List<UIDrawPoint>>();
    private List<List<Vector2>> _allPaths = new();
    private Vector2 _mouseLocalPosition;
    private Vector2 _mouseScreenPosition = Vector2.zero;

    private float _uiScale = 15f;
    
    private Vector2 ToUi(Vector2 model) => model - _medianValue;

    private Vector2 ToModel(Vector2 ui, bool addMedian) => addMedian ? (ui + _medianValue) : ui;

    public event Action<Vector2, Vector2> OnAction_LineWall;
    public event Action<List<Vector2>> OnCreateLinePath;
    
    private event CreateLinePathDele OnCreateLineEvent;
    private event UpdateLinePathDelegate OnUpdateLinePath;
    private event DisableWallPathDelegate OnDisableWallPath;

    protected override void Awake()
    {
        base.Awake();
        _drawActionType = DrawActionType.PointCreate;
    }
    
    void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
            _mouseScreenPosition, _uiCamera, out _mouseLocalPosition);
        SetVerticesDirty();
    }
    
    public void ClearAll()
    {
        _prevVertexPoint = null;
        _previewStart = Vector2.zero;
        _previewEnd = Vector2.zero;
        _shouldShowPreviewText = false;
        _prevEdgePath = null;
        ClearAllPaths();
    }
    
    public void SetDrawActionType(DrawActionType drawActionType)
    {
        _drawActionType = drawActionType;

        foreach (DrawVertex vertex in _wallGraph.GetAllVertices())
        {
            if (vertex.VisualPoint != null)
            {
                vertex.VisualPoint.SetActionType(drawActionType);
            }
        }
    }
    
    private List<Vector2> ConvertUiToModel(List<Vector2> uiPoints, bool addMedian)
    {
        var result = new List<Vector2>(uiPoints.Count);
        for (int i = 0; i < uiPoints.Count; i++)
            result.Add(ToModel(uiPoints[i], addMedian));
        return result;
    }
    
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        
        foreach (DrawEdge edge in _wallGraph.GetAllEdges())
        {
            if (edge.IsActive == true && edge.Start.IsActive == true && edge.End.IsActive == true)
            {
                DrawLine(vh, edge.Start.Position, edge.End.Position, _lineThickness, _lineColor);
            }
        }
        
        DrawVertex selectedVertex = _prevVertexPoint?.LinkedVertex;
        if (selectedVertex != null && _drawActionType == DrawActionType.PointCreate)
        {
            DrawLine(vh, selectedVertex.Position, _mouseLocalPosition, _lineThickness, _previewColor);
        
            _shouldShowPreviewText = true;
            _previewStart = selectedVertex.Position;
            _previewEnd = _mouseLocalPosition;
        }
        else
        {
            _shouldShowPreviewText = false;
        }
    }
    
 

    public void SetPathsData(List<List<Vector2>> paths, Vector3 medianPosition)
    {  
        ClearAllPaths();
        _medianValue = medianPosition;
        
        int pathCount = paths.Count;
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            var pointsData = paths[pathIndex];
            int cnt  = pointsData.Count;
            if (pointsData.Count < cnt) continue;

            DrawEdgePath path = _wallGraph.CreateEdgePath();
            _pathsRequireMedianOnExport.Add(path.Id);

            DrawVertex prevVertex = null;

            for (int pointIndex = 0; pointIndex < cnt; pointIndex++)
            {
                Vector2 modelPos = pointsData[pointIndex];
                Vector2 uiPos = ToUi(modelPos);

                DrawVertex vertex;
                UIDrawPoint existingPointUI = RaycastForPoint(uiPos);
                if (existingPointUI != null && existingPointUI.LinkedVertex != null)
                {
                    vertex = existingPointUI.LinkedVertex;
                }
                else
                {
                    vertex = CreateVertex(uiPos);
                }

                if (prevVertex != null)
                {
                    // 같은 간선이 이미 존재하는지 확인
                    DrawEdge existingEdge = _wallGraph.FindEdge(prevVertex, vertex);
                    if (existingEdge == null)
                    {
                        DrawEdge edge = _wallGraph.CreateEdge(prevVertex, vertex);
                        path.AddEdge(edge);
                    }
                    else
                    {
                        path.AddEdge(existingEdge);
                    }
                }
            
                prevVertex = vertex;
            }
        }
        SetVerticesDirty();
        _prevVertexPoint = null;
    }
    
    #region Input Functions
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
        {
            return;
        }
           
        switch (phase)
        {
            case InputActionPhase.Performed:
            {
                HandleLeftButtonDown();
            }
                break;
            case InputActionPhase.Canceled:
            {
                HandleLeftButtonUp();
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
                HandleRightButtonDown();
            }
                break;
        }
    }

    public void OnESCButtonClicked(InputAction.CallbackContext context)
    {
        var phase = context.phase;
        if (phase == InputActionPhase.Performed)
        {
            HandleRightButtonDown();
        }
    }
    
    #endregion
  #region Action Handler

    private void HandleLeftButtonDown()
    {
        switch (_drawActionType)
        {
            case DrawActionType.None:
                break;
            case DrawActionType.PointCreate:
            {
                HandlePointCreate();
            }
                break;
            case DrawActionType.PointEdit:
            {
                HandlePointEdit();
            }
                break;
            case DrawActionType.Viewer:
            {
                HandleViewer();
            }
                break;
        }
    }

    private void HandleLeftButtonUp()
    {
        switch (_drawActionType)
        {
            case DrawActionType.None:
                break;
            case DrawActionType.PointCreate:
            {
                // _prevEdgePath를 직접 사용
                if (_prevEdgePath != null && _prevEdgePath.Edges.Count > 0)
                {
                    List<Vector2> uiPoints = _prevEdgePath.GetPathPoints();

                    // 새로 만든 경로는 median을 더하면 안 됨
                    bool addMedian = _pathsRequireMedianOnExport.Contains(_prevEdgePath.Id);
                    List<Vector2> modelPoints = ConvertUiToModel(uiPoints, addMedian);

                    int pathId = _prevEdgePath.Id;
                    OnCreateLineEvent?.Invoke(modelPoints, pathId);

                    // 방금 생성된 경로는 "UI에서 만든 경로"이므로 median 적용 불필요로 기록 정정
                    _pathsRequireMedianOnExport.Remove(pathId);

                }
                else
                {
                    Debug.Log("경로 생성 안됨: 유효한 경로가 없거나 간선이 없습니다.");
                }
            }
                break;
            case DrawActionType.PointEdit:
            {
                HandlePointEdit();
            }
                break;
            case DrawActionType.Viewer:
            {
                HandleViewer();
            }
                break;
        }
    }

    private void HandleRightButtonDown()
    {
        _prevVertexPoint = null;
        _prevEdgePath = null;
        SetVerticesDirty();
    }
    
    private void HandlePointCreate()
    {
        // RaycastForPoint로 변경
        UIDrawPoint existingPointUI = RaycastForPoint(_mouseScreenPosition);
        
        if (existingPointUI == null)
        {
            DrawVertex newVertex = CreateVertex(_mouseLocalPosition);
            
            if (_prevVertexPoint != null && _prevVertexPoint.LinkedVertex != null)
            {
                DrawVertex prevVertex = _prevVertexPoint.LinkedVertex;
        
                // 같은 간선이 이미 존재하는지 확인
                DrawEdge existingEdge = _wallGraph.FindEdge(prevVertex, newVertex);
                if (existingEdge == null)
                {
                    DrawEdge edge = _wallGraph.CreateEdge(prevVertex, newVertex);
                    
                    if (_prevEdgePath != null)
                    {
                        _prevEdgePath.AddEdge(edge);
                    }
                }
                _prevVertexPoint.SetSelected(false);
            }
            else
            {
                // 첫 점이면 새 경로 생성
                _prevEdgePath = _wallGraph.CreateEdgePath();
            }
            
            UIDrawPoint pointUI = newVertex.VisualPoint;
            if (pointUI != null)  // 안전성 검사
            {
                _prevVertexPoint = pointUI;
                _prevVertexPoint.SetSelected(true);
            }
            else
            {
                Debug.LogError("새 점의 VisualPoint가 null입니다. CreateVertex 메서드를 확인하세요.");
            }
        }
        else
        {
            Debug.Log("Vertex is already exist.");
            return;
        }
        SetVerticesDirty();
    }

    private void HandlePointEdit()
    {
        UIDrawPoint clickedPoint = RaycastForPoint(_mouseScreenPosition);
    
        if (clickedPoint != null)
        {
            // 이전에 선택된 점이 있다면 원래 색상으로 변경
            if (_prevVertexPoint != null && _prevVertexPoint != clickedPoint)
            {
                _prevVertexPoint.SetSelected(false);
            }
            
            // 새 점 선택
            _prevVertexPoint = clickedPoint;
            _prevVertexPoint.SetSelected(true);
        }
        else
        {
            // 빈 공간 클릭 - 선택 해제
            if (_prevVertexPoint != null)
            {
                _prevVertexPoint.SetSelected(false);
                _prevVertexPoint = null;
            }
        }
    }
    
    private void HandlePointMoved(UIDrawPoint pointUI, Vector2 newPosition)
    {
        if (pointUI.LinkedVertex != null)
        {
            pointUI.LinkedVertex.Position = newPosition;
            SetVerticesDirty();

            if (_drawActionType == DrawActionType.PointEdit)
            {
                HashSet<DrawEdgePath> updatedPaths = new HashSet<DrawEdgePath>();
                foreach (DrawEdge edge in pointUI.LinkedVertex.ConnectedEdges)
                {
                    foreach (DrawEdgePath path in _wallGraph.GetAllEdgePaths())
                    {
                        if (path.Edges.Contains(edge) == true && updatedPaths.Add(path) != false)
                        {
                            List<Vector2> uiPoints = path.GetPathPoints();
                            bool addMedian = _pathsRequireMedianOnExport.Contains(path.Id);
                            List<Vector2> modelPoints = ConvertUiToModel(uiPoints, addMedian);

                            OnUpdateLinePath?.Invoke(modelPoints, path.Id);
                        }
                    }
                }
            }
        }
    }

    private void HandlePointSelected(UIDrawPoint pointUI)
    {
        if (_prevVertexPoint != null && _prevVertexPoint != pointUI)
        {
            _prevVertexPoint.SetSelected(false);
        }
        _prevVertexPoint = pointUI;
    }
    
    private void HandleViewer()
    {
        // 뷰어 모드 구현
    }


#endregion

#region Delegate Event

public void SetUpDisablePathEvent(DisableWallPathDelegate disablePathData)
{
    OnDisableWallPath = disablePathData;
}
    
public void SetUpdateLineEvent(UpdateLinePathDelegate updateLineData)
{
    OnUpdateLinePath = updateLineData;
}

public void SetUpLineCreateEvent(RectTransform drawZone, CreateLinePathDele createLineData)
{
    _drawZone = drawZone;
    OnCreateLineEvent = createLineData;
}

#endregion
    public void RemovePoint()
    {
        if(_prevVertexPoint == null) 
            return;
            
        DrawVertex linkedVertex = _prevVertexPoint.LinkedVertex;
        var paths = linkedVertex.ConnectedEdges;
            
        if (linkedVertex == null)
            return;
            
        SetVertexActive(linkedVertex, false);
            
        // 연결된 경로(Path) 갱신 및 이벤트 호출
        HashSet<DrawEdgePath> updatedPaths = new HashSet<DrawEdgePath>();
        foreach (DrawEdge edge in linkedVertex.ConnectedEdges)
        {
            foreach (DrawEdgePath path in _wallGraph.GetAllEdgePaths())
            {
                if (path.Edges.Contains(edge) && updatedPaths.Add(path))
                {
                    // 해당 Path의 좌표 리스트를 구함
                    List<Vector2> pathPoints = path.GetPathPoints();

                    // 이벤트를 통해 Path ID와 좌표 정보를 전달
                    OnDisableWallPath?.Invoke(pathPoints, path.Id);
                }
            }
        }
    }
    
    private void ClearAllPaths()
    {
        foreach (DrawVertex vertex in _wallGraph.GetAllVertices())
        {
            if (vertex.VisualPoint != null)
            {
                Destroy(vertex.VisualPoint.gameObject);
            }
        }
        _wallGraph.Clear();
        _prevVertexPoint = null;
        
        SetVerticesDirty();
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
    
    private void SetVertexActive(DrawVertex vertex, bool isActive)
    {
        vertex.IsActive = isActive;
            
        if (vertex.VisualPoint != null)
        {
            vertex.VisualPoint.gameObject.SetActive(isActive);
        }
            
        foreach (var edge in vertex.ConnectedEdges)
        {
            SetEdgeActive(edge, isActive);
        }
    }
        
    private void SetEdgeActive(DrawEdge edge, bool isActive)
    {
        edge.IsActive = isActive;
    }
    
    private bool IsInDrawZone(RectTransform drawZone, Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(drawZone, screenPos, _uiCamera);
    }
    
    private DrawVertex CreateVertex(Vector2 position)
    {
        DrawVertex vertex = _wallGraph.CreateVertex(position);

        UIDrawPoint pointUI = Instantiate(_pointPrefab, transform);
        pointUI.rectTransform.anchoredPosition = position;
        pointUI.color = _pointColor;
        pointUI.LinkedVertex = vertex;
        pointUI.SetActionType(_drawActionType);
        vertex.VisualPoint = pointUI;

        // 이벤트 핸들러 등록
        pointUI.OnPointSelected += HandlePointSelected;
        pointUI.OnPointMoved += HandlePointMoved;
    
        return vertex;
    }
    private UIDrawPoint RaycastForPoint(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        
        List<RaycastResult> results = new List<RaycastResult>();
        
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (RaycastResult result in results)
        {
            UIDrawPoint point = result.gameObject.GetComponent<UIDrawPoint>();
            if (point != null)
            {
                return point;
            }
        }
    
        return null;
    }
}
