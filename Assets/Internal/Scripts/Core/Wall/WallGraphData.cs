using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallGraphData
{
    private Dictionary<int, DrawVertex> _vertices = new Dictionary<int, DrawVertex>();
    private Dictionary<int, DrawEdge> _edges = new Dictionary<int, DrawEdge>();
    private Dictionary<int, DrawEdgePath> _edgePaths = new Dictionary<int, DrawEdgePath>();

    private int _nextVertexId = 0;
    private int _nextEdgeId = 0;
    private int _nextEdgePathId = 0;

    public DrawVertex CreateVertex(Vector2 position)
    {
        int id = _nextVertexId++;
        DrawVertex vertex = new DrawVertex(id, position);
        _vertices[id] = vertex;
        return vertex;
    }
    
    public DrawEdge CreateEdge(DrawVertex start, DrawVertex end)
    {
        int id = _nextEdgeId++;
        DrawEdge edge = new DrawEdge(id, start, end);
        _edges[id] = edge;
        return edge;
    }

    public DrawEdgePath CreateEdgePath()
    {
        int id = _nextEdgePathId++;
        DrawEdgePath edgePath = new DrawEdgePath(id);
        _edgePaths[id] = edgePath;
        return edgePath;
    }

    public DrawVertex GetVertex(int id)
    {
        return _vertices.TryGetValue(id, out var vertex) ? vertex : null;;
    }

    public DrawEdge GetEdge(int id)
    {
        return _edges.TryGetValue(id, out var edge) ? edge : null;
    }
    
    public DrawEdgePath GetEdgePath(int id)
    {
        return _edgePaths.TryGetValue(id, out var edgePath) ? edgePath : null;
    }

    public IEnumerable<DrawVertex> GetAllVertices()
    {
        return _vertices.Values;
    }

    public IEnumerable<DrawEdge> GetAllEdges()
    {
        return _edges.Values;
    }
    
    public IEnumerable<DrawEdgePath> GetAllEdgePaths()
    {
        return _edgePaths.Values;
    }

    public void RemoveVertex(int id)
    {
        if (_vertices.TryGetValue(id, out var vertex))
        {
            List<DrawEdge> edgesToRemove = new List<DrawEdge>(vertex.ConnectedEdges);
            foreach (DrawEdge edge in edgesToRemove)
            {
                RemoveEdge(edge.Id);
            }
            
            _vertices.Remove(id);
        }
    }
    
    public void RemoveEdge(int id)
    {
        if (_edges.TryGetValue(id, out var edge))
        {
            edge.Start.RemoveEdge(edge);
            edge.End.RemoveEdge(edge);

            foreach (DrawEdgePath path in _edgePaths.Values)
            {
                path.RemoveEdge(edge);
            }
            _edges.Remove(id);
        }
    }

    public void RemovePath(int id)
    {
        if (_edgePaths.TryGetValue(id, out var path))
        {
            _edgePaths.Remove(id);
        }
    }

    public void Clear()
    {
        _vertices.Clear();
        _edges.Clear();
        _edgePaths.Clear();
        _nextVertexId = 0;
        _nextEdgeId = 0;
        _nextEdgePathId = 0;
    }

    public DrawEdge FindEdge(DrawVertex start, DrawVertex end)
    {
        foreach (DrawEdge edge in start.ConnectedEdges)
        {
            if((edge.Start == start && edge.End == end) ||
               (edge.Start == end && edge.End == start))
            {
                return edge;
            }
        }

        return null;
    }
}

public class DrawVertex
{
    public int Id { get; private set; }
    public UIDrawPoint VisualPoint { get; set; }
    public Vector2 Position { get; set; }
    public List<DrawEdge> ConnectedEdges { get; private set; } = new List<DrawEdge>();

    public bool IsActive { get; set; } = true;

    public DrawVertex(int id, Vector2 position)
    {
        Id = id;
        Position = position;
    }
    
    public void AddEdge(DrawEdge edge)
    {
        if(ConnectedEdges.Contains(edge) == false)
            ConnectedEdges.Add(edge);
    }
    
    public void RemoveEdge(DrawEdge edge)
    {
        ConnectedEdges.Remove(edge);
    }
}

public class DrawEdge
{
    public int Id { get; private set; }
    public DrawVertex Start { get; set; }
    public DrawVertex End { get; set; }
    public Color EdgeColor { get; set; } = Color.green;
    public float Thickness { get; set; } = 1f;
    
    public bool IsActive { get; set; } = true;
    
    public DrawEdge(int id, DrawVertex start, DrawVertex end)
    {
        Id = id;
        Start = start;
        End = end;
        
        start.AddEdge(this);
        end.AddEdge(this);
    }
    
    public float GetLength()
    {
        return Vector2.Distance(Start.Position, End.Position);
    }

    public Vector2 GetMidPoint()
    {
        return (Start.Position + End.Position) * 0.5f;
    }
}

public class DrawEdgePath
{
    public int Id { get; private set; }
    public List<DrawEdge> Edges { get; private set; } = new List<DrawEdge>();

    public DrawEdgePath(int id)
    {
        Id = id;
    }
    
    public void AddEdge(DrawEdge edge)
    {
        Edges.Add(edge);
    }
    
    public void RemoveEdge(DrawEdge edge)
    {
        Edges.Remove(edge);
    }

    public List<Vector2> GetPathPoints()
    {
        if(Edges.Any() == false) return new List<Vector2>();
        
        List<Vector2> points = new List<Vector2>();
        HashSet<DrawVertex> addedVertices = new HashSet<DrawVertex>();
        // 첫 번째 간선의 시작점 추가
        points.Add(Edges[0].Start.Position);
        addedVertices.Add(Edges[0].Start);
        
        foreach (DrawEdge edge in Edges)
        {
            if (addedVertices.Contains(edge.End) == false)
            {
                points.Add(edge.End.Position);
                addedVertices.Add(edge.End);
            }
        }
        
        return points;
    }
}