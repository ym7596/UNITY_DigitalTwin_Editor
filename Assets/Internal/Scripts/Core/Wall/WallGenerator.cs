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
    
    private Vector3[] _prevEndQuad = null;
    private HashSet<LineKey> _generatedLines = new HashSet<LineKey>();
    private Vector3 V2ToV3(Vector2 v) => new Vector3(v.x, 0f, v.y);
    private Vector2Int ToGridKey(Vector2 v, float scale = 1000f)
    {
        return new Vector2Int(Mathf.RoundToInt(v.x * scale), Mathf.RoundToInt(v.y * scale));
    }
    
    private Dictionary<Vector2, Vector3> _miterNormals = new Dictionary<Vector2, Vector3>();
    

    public void SetUIManager(UIManager uiManager)
    {
        _uiManager = uiManager;
    }

    public void GenerateWallPath(List<Vector2> path)
    {
        if (path == null || path.Count < 2) return;

        _miterNormals.Clear();
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 current = path[i] * 0.1f;

            Vector3 normal;

            if (i == 0)
            {
                // 시작점: 단순한 방향 normal
                Vector3 dir = (V2ToV3(path[1] * 0.1f) - V2ToV3(current)).normalized;
                normal = Vector3.Cross(Vector3.up, dir).normalized * (_wallThickness / 2f);
            }
            else if (i == path.Count - 1)
            {
                // 끝점: 이전 방향 사용
                Vector3 dir = (V2ToV3(current) - V2ToV3(path[i - 1] * 0.1f)).normalized;
                normal = Vector3.Cross(Vector3.up, dir).normalized * (_wallThickness / 2f);
            }
            else
            {
                Vector3 dirA = (V2ToV3(current) - V2ToV3(path[i - 1] * 0.1f)).normalized;
                Vector3 dirB = (V2ToV3(path[i + 1] * 0.1f) - V2ToV3(current)).normalized;

                normal = GetMiterNormal(dirA, dirB, _wallThickness);
            }

            _miterNormals[ToGridKey(current)] = normal;
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 a = path[i] * 0.1f;
            Vector2 b = path[i + 1] * 0.1f;

            var key = new LineKey(a, b);
            if (!_generatedLines.Add(key))
                continue;
            Vector3 start = V2ToV3(a);
            Vector3 end = V2ToV3(b);
            Vector3 normalA = _miterNormals[ToGridKey(a)];
            Vector3 normalB = _miterNormals[ToGridKey(b)];

            _prevEndQuad = CreateWallMeshWithReuse(start, end, normalA, normalB, _prevEndQuad);
            //CreateWallMesh(V2ToV3(a), V2ToV3(b), normalA, normalB);
        }
    }
    
    private Vector3 GetMiterNormal(Vector3 dirA, Vector3 dirB, float thickness)
    {
        Vector3 nA = Vector3.Cross(Vector3.up, dirA).normalized;
        Vector3 nB = Vector3.Cross(Vector3.up, dirB).normalized;

        // 방향의 중간값을 기준으로 보정
        Vector3 miter = (nA + nB).normalized;

        float dot = Vector3.Dot(dirA, dirB);
        float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));

        // 너무 예리한 각도는 제한 (5도 이상)
        float miterLength = thickness / (2f * Mathf.Sin(angle / 2f));
        miterLength = Mathf.Min(miterLength, thickness * 4f); // 튀는 것 방지

        // 🔥 여기서 방향 체크해서 내부로 향하도록 보정
        Vector3 cornerDir = (dirA + dirB).normalized;
        if (Vector3.Dot(miter, cornerDir) < 0)
            miter = -miter;

        return miter * miterLength;
    }
    
    private Vector3[] CreateWallMeshWithReuse(Vector3 a, Vector3 b, Vector3 normalA, Vector3 normalB, Vector3[] reusedStartQuad = null)
    {
        Vector3 up = Vector3.up * _wallHeight;

        Vector3 p0 = reusedStartQuad != null ? reusedStartQuad[0] : a - normalA;
        Vector3 p1 = reusedStartQuad != null ? reusedStartQuad[1] : a + normalA;
        Vector3 p4 = reusedStartQuad != null ? reusedStartQuad[2] : p0 + up;
        Vector3 p5 = reusedStartQuad != null ? reusedStartQuad[3] : p1 + up;

        Vector3 p2 = b + normalB;
        Vector3 p3 = b - normalB;
        Vector3 p6 = p2 + up;
        Vector3 p7 = p3 + up;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7 };
        mesh.triangles = new int[]
        {
            0,1,2, 2,3,0,
            4,6,5, 4,7,6,
            0,4,5, 5,1,0,
            3,2,6, 6,7,3,
            0,3,7, 7,4,0,
            1,5,6, 6,2,1
        };
        mesh.RecalculateNormals();

        GameObject wall = new GameObject("WallSegment");
        wall.transform.SetParent(transform);
        wall.AddComponent<MeshFilter>().mesh = mesh;
        wall.AddComponent<MeshRenderer>().material = _wallMaterial;

        return new Vector3[] { p2, p3, p6, p7 }; // 다음 벽에서 재사용할 End Quad
    }
}

public struct LineKey : IEquatable<LineKey>
{
    private readonly Vector2 _a;
    private readonly Vector2 _b;

    public LineKey(Vector2 a, Vector2 b)
    {
        if (a.x < b.x || (Mathf.Approximately(a.x, b.x) && a.y < b.y))
        {
            _a = a;
            _b = b;
        }
        else
        {
            _a = b;
            _b = a;
        }
    }

    public bool Equals(LineKey other)
    {
        return _a == other._a && _b == other._b;
    }

    public override bool Equals(object obj)
    {
        return obj is LineKey other && Equals(other);
    }

    public override int GetHashCode() => _a.GetHashCode() ^ _b.GetHashCode();
}
