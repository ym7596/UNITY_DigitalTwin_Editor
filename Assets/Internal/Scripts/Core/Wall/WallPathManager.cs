using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallPathManager
{
    private Material _wallMaterial;
    private Transform _transform;
    private float _wallHeight;
    private float _wallThickness;
    private float _magnification;

    private HashSet<LineKey> _generatedLines;
    private HashSet<Vector2Int> _alreadyCorrectedPoints;
    private Dictionary<Vector2Int, List<WallSegment>> _wallSegmentsByPoint;
    private Dictionary<Vector2Int, List<WallIntersectionData>> _intersectionsByPoint;
    private Vector3 V2ToV3(Vector2 v) => new Vector3(v.x, 0f, v.y);
    private Vector2Int ToGridKey(Vector2 v, float scale = 1000f) =>
        new Vector2Int(Mathf.RoundToInt(v.x * scale), Mathf.RoundToInt(v.y * scale));
    
    public WallPathManager(Material wallMaterial, Transform transform, float wallHeight,float wallThickness, float magnification)
    {
        _wallMaterial = wallMaterial;
        _transform = transform;
        _wallHeight = wallHeight;
        _wallThickness = wallThickness;
        _magnification = magnification;
        _wallSegmentsByPoint = new Dictionary<Vector2Int, List<WallSegment>>();
        _intersectionsByPoint = new Dictionary<Vector2Int, List<WallIntersectionData>>();
        _generatedLines = new HashSet<LineKey>();
        _alreadyCorrectedPoints = new HashSet<Vector2Int>();
    }
    
    public WallSegment CreateWallMeshBasic(Vector3 start, Vector3 end, Vector3 perpendicular, float halfThickness)
    {
        Vector3 up = Vector3.up * _wallHeight;

        // 기본 직사각형 벽 생성
        Vector3 p0 = start - perpendicular * halfThickness;  // 안쪽 시작
        Vector3 p1 = start + perpendicular * halfThickness;  // 바깥쪽 시작
        Vector3 p2 = end + perpendicular * halfThickness;    // 바깥쪽 끝
        Vector3 p3 = end - perpendicular * halfThickness;    // 안쪽 끝

        Vector3 p4 = p0 + up;
        Vector3 p5 = p1 + up;
        Vector3 p6 = p2 + up;
        Vector3 p7 = p3 + up;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7 };
        mesh.triangles = new int[]
        {
            0,1,2, 2,3,0,     // bottom
            4,6,5, 4,7,6,     // top
            0,4,5, 5,1,0,     // front
            3,2,6, 6,7,3,     // back
            0,3,7, 7,4,0,     // left
            1,5,6, 6,2,1      // right
        };
        mesh.RecalculateNormals();

        GameObject wall = new GameObject("WallSegment");
        wall.transform.SetParent(_transform);
        MeshFilter mf = wall.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        wall.AddComponent<MeshRenderer>().material = _wallMaterial;

        return new WallSegment
        {
            go = wall,
            meshFilter = mf,
            vertices = mesh.vertices,
        };
    }
    
    public void CreateWallByLineEditor(List<Vector2> path)
    {
        int cnt = path.Count;
        for (int i = 0; i < cnt - 1; i++)
        {
            Vector2 a = path[i] * _magnification; 
            Vector2 b = path[i + 1] * _magnification;
            var key = new LineKey(a, b);
            if (_generatedLines.Add(key) == false)
                continue;

            Vector3 start = V2ToV3(a);
            Vector3 end = V2ToV3(b);
            Vector2Int keyA = ToGridKey(a);
            Vector2Int keyB = ToGridKey(b);

            // 방향 벡터와 수직 벡터 계산
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized;
            float halfThickness = _wallThickness / 2f;

            // 기본 벽 생성
            WallSegment segment = CreateWallMeshBasic(start, end, perpendicular, halfThickness);
            
            // 시작점과 끝점에 대한 세그먼트 저장
            RegisterSegmentAtPoint(keyA, segment, true);  // 시작점
            RegisterSegmentAtPoint(keyB, segment, false); // 끝점
            
            // 교차점 데이터 저장
            StoreIntersectionData(keyA, start, direction, perpendicular, halfThickness, segment, true);
            StoreIntersectionData(keyB, end, direction, perpendicular, halfThickness, segment, false);
        }
    }
    
    public void FixAllIntersections()
    {
        foreach (var kvp in _intersectionsByPoint)
        {
            Vector2Int pointKey = kvp.Key;
            if (_alreadyCorrectedPoints.Contains(pointKey))
                continue;

            List<WallIntersectionData> intersections = kvp.Value;
            
            // 한 점에 두 개 이상의 벽이 있을 때만 교차점 수정
            if (intersections.Count >= 2)
            {
                ProcessIntersection(pointKey, intersections);
                _alreadyCorrectedPoints.Add(pointKey);
            }
        }
    }
    
      private void ProcessIntersection(Vector2Int pointKey, List<WallIntersectionData> intersections)
    {
        int intersectionCount = intersections.Count;
        // 각 벽 쌍에 대해 교점 계산 및 적용
        for (int i = 0; i < intersectionCount; i++)
        {
            for (int j = i + 1; j < intersectionCount; j++)
            {
                var dataA = intersections[i];
                var dataB = intersections[j];
                
                // 안쪽 평행선 교점
                Vector3 innerA1 = dataA.Point - dataA.Perpendicular * dataA.HalfThickness;
                Vector3 innerA2 = innerA1 + dataA.Direction * 100f;
                Vector3 innerB1 = dataB.Point - dataB.Perpendicular * dataB.HalfThickness;
                Vector3 innerB2 = innerB1 + dataB.Direction * 100f;
                
                if (CalculateLineIntersection(innerA1, innerA2, innerB1, innerB2, out Vector3 innerIntersection))
                {
                    // 안쪽 교점 적용
                    ApplyIntersectionToSegment(dataA, innerIntersection, true);
                    ApplyIntersectionToSegment(dataB, innerIntersection, true);
                }
                
                // 바깥쪽 평행선 교점
                Vector3 outerA1 = dataA.Point + dataA.Perpendicular * dataA.HalfThickness;
                Vector3 outerA2 = outerA1 + dataA.Direction * 100f;
                Vector3 outerB1 = dataB.Point + dataB.Perpendicular * dataB.HalfThickness;
                Vector3 outerB2 = outerB1 + dataB.Direction * 100f;
                
                if (CalculateLineIntersection(outerA1, outerA2, outerB1, outerB2, out Vector3 outerIntersection))
                {
                    // 바깥쪽 교점 적용
                    ApplyIntersectionToSegment(dataA, outerIntersection, false);
                    ApplyIntersectionToSegment(dataB, outerIntersection, false);
                }
            }
        }
    }

    private void ApplyIntersectionToSegment(WallIntersectionData data, Vector3 intersection, bool isInner)
    {
        Vector3 up = Vector3.up * _wallHeight;
        
        if (data.IsStart)
        {
            // 시작점 업데이트
            if (isInner)
            {
                data.Segment.vertices[0] = intersection;     // 안쪽 시작 (바닥)
                data.Segment.vertices[4] = intersection + up; // 안쪽 시작 (천장)
            }
            else
            {
                data.Segment.vertices[1] = intersection;     // 바깥쪽 시작 (바닥)
                data.Segment.vertices[5] = intersection + up; // 바깥쪽 시작 (천장)
            }
        }
        else
        {
            // 끝점 업데이트
            if (isInner)
            {
                data.Segment.vertices[3] = intersection;     // 안쪽 끝 (바닥)
                data.Segment.vertices[7] = intersection + up; // 안쪽 끝 (천장)
            }
            else
            {
                data.Segment.vertices[2] = intersection;     // 바깥쪽 끝 (바닥)
                data.Segment.vertices[6] = intersection + up; // 바깥쪽 끝 (천장)
            }
        }
        
        // 메시 업데이트
        data.Segment.meshFilter.mesh.vertices = data.Segment.vertices;
        data.Segment.meshFilter.mesh.RecalculateNormals();
    }

    private bool CalculateLineIntersection(Vector3 line1Start, Vector3 line1End, 
                                         Vector3 line2Start, Vector3 line2End, out Vector3 intersection)
    {
        intersection = Vector3.zero;

        // 2D 평면에서 계산 (Y축은 무시)
        Vector2 p1 = new Vector2(line1Start.x, line1Start.z);
        Vector2 p2 = new Vector2(line1End.x, line1End.z);
        Vector2 p3 = new Vector2(line2Start.x, line2Start.z);
        Vector2 p4 = new Vector2(line2End.x, line2End.z);

        Vector2 dir1 = (p2 - p1).normalized;
        Vector2 dir2 = (p4 - p3).normalized;

        // 평행선 체크
        float cross = dir1.x * dir2.y - dir1.y * dir2.x;
        if (Mathf.Abs(cross) < 0.0001f) return false;

        // 교점 계산
        Vector2 dp = p3 - p1;
        float t = (dp.x * dir2.y - dp.y * dir2.x) / cross;
        
        Vector2 intersectionPoint = p1 + dir1 * t;
        intersection = new Vector3(intersectionPoint.x, 0, intersectionPoint.y);
        
        return true;
    }
    private void RegisterSegmentAtPoint(Vector2Int key, WallSegment segment, bool isStart)
    {
        if (!_wallSegmentsByPoint.ContainsKey(key))
            _wallSegmentsByPoint[key] = new List<WallSegment>();
        
        _wallSegmentsByPoint[key].Add(segment);
    }
    
    private void StoreIntersectionData(Vector2Int key, Vector3 point, Vector3 direction, Vector3 perpendicular, 
        float halfThickness, WallSegment segment, bool isStart)
    {
        if (_intersectionsByPoint.ContainsKey(key) == false)
            _intersectionsByPoint[key] = new List<WallIntersectionData>();
        
        _intersectionsByPoint[key].Add(new WallIntersectionData {
            Point = point,
            Direction = direction,
            Perpendicular = perpendicular,
            HalfThickness = halfThickness,
            Segment = segment,
            IsStart = isStart
        });
    }
    
    #region LineRenderer Fix
      public List<List<Vector2>> LineRendererToList(LineRenderer[] lineRenderers)
    {
        HashSet<string> uniquePaths = new HashSet<string>();
        List<List<Vector2>> newPaths = new List<List<Vector2>>();
        foreach (var line in lineRenderers)
        {
            List<Vector2> path = new List<Vector2>();

            for (int i = 0; i < line.positionCount; i++)
            {
                Vector3 pos = line.GetPosition(i);
                path.Add(new Vector2(pos.x, pos.z));
            }
            
            if (path.Count >= 2)
            {
                // 각 선의 Start와 End를 정렬하여 고유 키 생성
                Vector2 startPoint = path[0];
                Vector2 endPoint = path[path.Count - 1];

                // start와 end를 정렬하여 중복 방지 (작은 좌표가 항상 먼저 오도록)
                string pathKey = CreatePathKey(startPoint, endPoint);

                // 중복되지 않은 경우만 추가
                if (uniquePaths.Add(pathKey))
                {
                    newPaths.Add(path);
                }
            }
        }

        return MergeConnectedPaths(newPaths);;
    }
    
    private string CreatePathKey(Vector2 point1, Vector2 point2)
    {
        // 두 점을 정렬하여 항상 작은 좌표가 먼저 오도록 키를 생성
        if (point1.x < point2.x || (point1.x == point2.x && point1.y < point2.y))
        {
            return $"{point1.x},{point1.y}:{point2.x},{point2.y}";
        }
        else
        {
            return $"{point2.x},{point2.y}:{point1.x},{point1.y}";
        }
    }
    
    private List<List<Vector2>> MergeConnectedPaths(List<List<Vector2>> paths)
    {
        List<List<Vector2>> mergedPaths = new List<List<Vector2>>();

        // 연결 상태 추적
        while (paths.Count > 0) // 아직 처리하지 않은 경로가 있을 때까지 반복
        {
            var mergedPath = new List<Vector2>(paths[0]);
            paths.RemoveAt(0); // 첫 번째 경로를 처리

            bool merged;

            do
            {
                merged = false;
                for (int i = paths.Count - 1; i >= 0; i--) // 뒤에서부터 검사
                {
                    var path = paths[i];

                    if (CanMergePaths(mergedPath, path, out bool reverse))
                    {
                        if (reverse)
                        {
                            path.Reverse(); // 방향이 반대라면 뒤집음
                        }
                        mergedPath = MergeTwoPaths(mergedPath, path);
                        paths.RemoveAt(i); // 이 경로는 처리되었으므로 제거
                        merged = true;
                    }
                }
            }
            while (merged); // 더 이상 병합할 수 없을 때까지 반복

            mergedPaths.Add(mergedPath); // 병합된 경로를 결과에 추가
        }

        return mergedPaths;

    }

    private bool CanMergePaths(List<Vector2> path1, List<Vector2> path2, out bool reverse)
    {
        reverse = false;

        // 병합 가능 조건: 경로 끝-끝 또는 끝-시작이 일치하는 경우
        if (path1[^1] == path2[0]) // path1의 끝과 path2의 시작
        {
            return true;
        }
        if (path1[0] == path2[^1]) // path1의 시작과 path2의 끝
        {
            reverse = true; // 방향이 반대인 경우
            return true;
        }
        if (path1[^1] == path2[^1]) // path1의 끝과 path2의 끝
        {
            reverse = true; // 방향이 반대인 경우
            return true;
        }
        if (path1[0] == path2[0]) // path1의 시작과 path2의 시작
        {
            reverse = true; // 방향이 동일하지 않은 경우
            return true;
        }

        return false;
    }

    private List<Vector2> MergeTwoPaths(List<Vector2> path1, List<Vector2> path2)
    {
        // 병합 로직: 연결되는 위치에 따라 병합
        if (path1[^1] == path2[0])
        {
            path1.AddRange(path2.Skip(1)); // 중복된 점을 제외하고 추가
        }
        else if (path1[0] == path2[^1])
        {
            path2.AddRange(path1.Skip(1)); // 중복된 점을 제외하고 추가
            return path2; // 새 path2를 반환
        }
        else if (path1[^1] == path2[^1])
        {
            path2.Reverse(); // path2를 뒤집고 병합
            path1.AddRange(path2.Skip(1));
        }
        else if (path1[0] == path2[0])
        {
            path1.Reverse(); // path1을 뒤집고 병합
            path1.AddRange(path2.Skip(1));
        }

        return path1;
    }
    
    #endregion
}

public class WallIntersectionData
{
    public Vector3 Point;          // 교차점 좌표
    public Vector3 Direction;      // 벽의 방향 벡터
    public Vector3 Perpendicular;  // 수직 벡터
    public float HalfThickness;    // 벽 두께의 절반
    public WallSegment Segment;    // 해당 세그먼트
    public bool IsStart;           // true=시작점, false=끝점
}

public class WallSegment
{
    public GameObject go;
    public MeshFilter meshFilter;
    public Vector3[] vertices;
    
    public bool MatchesPath(Vector2 point1, Vector2 point2)
    {
        Vector2 start = (Vector2)vertices[0]; // 시작점
        Vector2 end = (Vector2)vertices[1];   // 끝점

        // 경로의 두 점이 Edge의 두 점과 일치하는지 확인 (순서포함)
        return (start == point1 && end == point2) || (start == point2 && end == point1);
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

