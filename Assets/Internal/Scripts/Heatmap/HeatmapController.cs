using System;
using System.Collections.Generic;
using UnityEngine;

public class HeatmapController : MonoBehaviour
{
    private const int MAX_POSITIONS = 256;

    #region Inspector Fields

    [Header("Targets")]
    [SerializeField] private Renderer _targetHeatMapRenderer;
    [SerializeField] private Transform _targetContainer;
    [SerializeField] private HeatMapWeight _heatmapWardPrefab;

    [Header("Shader Params")]
    [SerializeField] private float _radius = 5.0f;
    
    [Tooltip("Shader property _Sigma")]
    [SerializeField] private float _sigmaFraction = 0.35f;
    
    [Tooltip("Value used when Auto Update is disabled")]
    [SerializeField] private float _maxIntensity = 1.0f;
    
    [SerializeField] private bool _autoUpdateMaxIntensity = true;
    [SerializeField, Range(0f, 1f)] private float _radiusWeightK = 0.25f;

    [Header("Sampling")]
    [SerializeField] private bool _includeDescendants = true;
    [SerializeField] private bool _leafOnly = true;
    [SerializeField] private float _updateInterval = 0.1f;

    [Header("Auto Scaling")]
    [Tooltip("EMA Smoothing factor")]
    [SerializeField, Range(0f, 1f)] private float _autoLerp = 0.2f;

    [Header("Visibility / Thresholds")]
    [Tooltip("Weights below this value are considered 0")]
    [SerializeField] private float _weightEpsilon = 0.9f;

    #endregion

    #region Private Fields

    // Collections & Buffers
    private readonly List<Vector4> _positions = new();
    private readonly List<Transform> _collected = new();
    private readonly HashSet<HeatMapWeight> _trackedWeights = new();
    private readonly Dictionary<string, HeatMapWeight> _wardById = new();

    // Runtime State
    private Material _mat;
    private int _allocatedArraySize = 0;
    private float _autoMaxIntensity = 1f;
    private bool _lastAutoFlag;
    private bool _heatmapLayerOn = true;

    // Timing & Debounce
    private float _timer;
    private float _dirtyUntilTime;
    private bool _dirty;
    private float _recomputeDebounce = 0.03f;

    #endregion

    #region Shader Properties

    private static readonly int PositionsProperty     = Shader.PropertyToID("_Positions");
    private static readonly int PositionCountProperty = Shader.PropertyToID("_PositionCount");
    private static readonly int RadiusProperty        = Shader.PropertyToID("_Radius");
    private static readonly int SigmaProperty         = Shader.PropertyToID("_Sigma");
    private static readonly int MaxIntensityProperty  = Shader.PropertyToID("_MaxIntensity");

    #endregion

    public int WardCount => _wardById.Count;

    private void OnEnable()
    {
        TryInitMaterial();
    }

    private void OnDisable()
    {
        ClearHeatmap();
    }

    public void GenerateHeatmap(HeatMapDataList dataList)
    {
        if (dataList == null || dataList.heatMaps == null)
        {
            Debug.LogWarning("[HeatmapController] Data list is empty or null.");
            return;
        }

        // 1. 기존에 생성된 와드들 제거
        ClearHeatmap();

        // 2. 데이터 기반으로 와드 생성
        foreach (var model in dataList.heatMaps)
        {
            CreateWard(model);
        }

        // 3. 변경 사항 반영을 위해 Dirty 플래그 설정
        _dirty = true;
            
        // 즉시 업데이트가 필요하면 아래 주석 해제
        // UpdateHeatmap();
    }

    // 기존 와드들을 모두 삭제하는 함수
    public void ClearHeatmap()
    {
        if (_targetContainer == null) return;

        // 자식 오브젝트들을 순회하며 제거 (역순 순회)
        for (int i = _targetContainer.childCount - 1; i >= 0; i--)
        {
            var child = _targetContainer.GetChild(i);
            Destroy(child.gameObject); 
        }

        _wardById.Clear();
        _trackedWeights.Clear();
        _collected.Clear();
        _positions.Clear();
    }

    private void CreateWard(HeatMapModel model)
    {
        if (_heatmapWardPrefab == null)
        {
            Debug.LogError("[HeatmapController] Ward Prefab is not assigned!");
            return;
        }

        // 프리팹 생성 & 부모 설정
        HeatMapWeight newWard = Instantiate(_heatmapWardPrefab, _targetContainer);
            
        // 위치 설정 (Vector3Format -> Vector3 변환 필요)
        newWard.transform.localPosition = new Vector3(model.position.x, model.position.y, model.position.z);
        newWard.transform.localRotation = Quaternion.identity;

        // 데이터(ID, Count) 주입
        // Count를 Weight로 사용한다고 가정합니다.
        newWard.Initialize(model.id, model.count);
    }

    private void TryInitMaterial()
    {
        if (_mat != null) return;

        if (_targetHeatMapRenderer == null)
        {
            Debug.LogError("HeatmapController: Target renderer is null.");
            enabled = false;
            return;
        }

        var srcMat = _targetHeatMapRenderer.sharedMaterial ?? _targetHeatMapRenderer.material;
        _mat = new Material(srcMat);
        _targetHeatMapRenderer.material = _mat;

        _mat.SetFloat(RadiusProperty, Mathf.Max(0.01f, _radius));
        _mat.SetFloat(SigmaProperty, Mathf.Clamp(_sigmaFraction, 0.1f, 1.0f));

        _autoMaxIntensity = Mathf.Max(1e-4f, _maxIntensity);
        _mat.SetFloat(MaxIntensityProperty, _autoUpdateMaxIntensity ? _autoMaxIntensity : Mathf.Max(1e-4f, _maxIntensity));
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // 인스펙터에서 자동/수동 토글이 바뀌었는지 엣지 감지
        if (_lastAutoFlag != _autoUpdateMaxIntensity)
        {
            _lastAutoFlag = _autoUpdateMaxIntensity;
            _dirty = true;
        }

        // 인스펙터에서 반경/시그마를 바꾸면 즉시 반영 + 재계산
        if (_mat != null)
        {
            if (Mathf.Abs(_mat.GetFloat(RadiusProperty) - _radius) > Mathf.Epsilon)
            {
                _mat.SetFloat(RadiusProperty, Mathf.Max(0.01f, _radius));
                _dirty = true;
            }
            if (Mathf.Abs(_mat.GetFloat(SigmaProperty) - _sigmaFraction) > Mathf.Epsilon)
            {
                _mat.SetFloat(SigmaProperty, Mathf.Clamp(_sigmaFraction, 0.1f, 1.0f));
                _dirty = true;
            }
            // 수동 모드에서 수동 강도 변경 시 바로 적용
            if (!_autoUpdateMaxIntensity)
            {
                _mat.SetFloat(MaxIntensityProperty, Mathf.Max(1e-4f, _maxIntensity));
            }
        }

        // 디바운스 마감 or 주기적 갱신 타이밍이면 업데이트
        bool timeToDebouncedRecompute = _dirty && Time.time >= _dirtyUntilTime;
        bool timeToIntervalRecompute  = _timer >= _updateInterval;

        if (timeToDebouncedRecompute || timeToIntervalRecompute)
        {
            UpdateHeatmap();
            _timer = 0f;
            _dirty = false;
        }
    }

    private void UpdateHeatmap()
    {
        if (_targetContainer == null || _mat == null) return;

        if (_heatmapLayerOn == false)
        {
            _mat.SetInt(PositionCountProperty, 0);
            if (_targetHeatMapRenderer != null) _targetHeatMapRenderer.enabled = false;
            return;
        }
        // 1) 포인트 수집
        _positions.Clear();
        _collected.Clear();

        if (_includeDescendants) CollectRecursive(_targetContainer);
        else
        {
            foreach (Transform child in _targetContainer)
                if (child.gameObject.activeSelf) _collected.Add(child);
        }

        if (_leafOnly)
        {
            for (int i = _collected.Count - 1; i >= 0; i--)
            {
                var tf = _collected[i];
                bool hasChild = HasActiveChild(tf);
                bool hasWeight = tf.GetComponent<HeatMapWeight>() != null;
                if (hasChild && !hasWeight) _collected.RemoveAt(i);
            }
        }

        // Weight 컴포넌트 구독 상태 동기화(런타임 생성/파괴 대응)
        SyncWeightSubscriptions();

        // 2) Weight와 함께 위치 수집
        var tempPositions = new List<Vector4>(_collected.Count);
        foreach (var t in _collected)
        {
            if (!t.gameObject.activeInHierarchy) continue;

            float weight = 0f;
            var wComp = t.GetComponent<HeatMapWeight>();
            if (wComp != null)
            {
                weight = Mathf.Max(0f, wComp.Weight); // 선형 가중
            }
            
            if (weight <= _weightEpsilon) continue;

            tempPositions.Add(new Vector4(t.position.x, t.position.y, t.position.z, weight));
        }
        
        if (tempPositions.Count == 0)
        {
            _mat.SetInt(PositionCountProperty, 0);
            if (_targetHeatMapRenderer != null) _targetHeatMapRenderer.enabled = false; // 보이지 않게
            return;
        }
        else
        {
            // 양수 포인트가 하나라도 있으면 보이게
            if (_targetHeatMapRenderer != null && _heatmapLayerOn)
                _targetHeatMapRenderer.enabled = true;
        }
        
        // 3) 256개 초과 시 중요도 기반 필터링
        int realCount = tempPositions.Count;
        if (realCount > MAX_POSITIONS)
        {
            // Weight 기준 내림차순 정렬
            tempPositions.Sort((a, b) => b.w.CompareTo(a.w));
            
            // 상위 256개만 선택
            _positions.AddRange(tempPositions.GetRange(0, MAX_POSITIONS));
            realCount = MAX_POSITIONS;
            
            Debug.LogWarning($"[HeatMap] {tempPositions.Count}개 포인트 중 상위 {MAX_POSITIONS}개만 사용");
        }
        else
        {
            _positions.AddRange(tempPositions);
        }

        // 4) 자동 스케일 계산(고정 R + 선형 weight, 셰이더와 동일 커널)
        if (_autoUpdateMaxIntensity && realCount > 0)
        {
            float target = ComputeAutoTargetOnce(realCount);
            _autoMaxIntensity = Mathf.Lerp(_autoMaxIntensity, target, Mathf.Clamp01(_autoLerp));
            _mat.SetFloat(MaxIntensityProperty, _autoMaxIntensity);
        }
        else
        {
            _mat.SetFloat(MaxIntensityProperty, Mathf.Max(1e-4f, _maxIntensity));
        }

        // 5) GPU 업로드 (WebGL 호환: 고정 크기 배열 사용)
        if (realCount == 0)
        {
            _mat.SetInt(PositionCountProperty, 0);
            return;
        }

        // ⭐ WebGL을 위해 항상 MAX_POSITIONS 크기로 패딩
        while (_positions.Count < MAX_POSITIONS)
        {
            _positions.Add(Vector4.zero);
        }

        _mat.SetVectorArray(PositionsProperty, _positions);
        _mat.SetInt(PositionCountProperty, realCount);
    }


    // ===== 내부 유틸 =====

    private float ComputeAutoTargetOnce(int realCount)
    {
        float R = Mathf.Max(0.01f, _radius);
        float sigma = Mathf.Clamp(_sigmaFraction, 0.1f, 1.0f);

        float maxDensity = 0f;
        for (int i = 0; i < realCount; i++)
        {
            Vector3 pi = _positions[i];
            float density = 0f;

            for (int j = 0; j < realCount; j++)
            {
                float wj = _positions[j].w;
                Vector3 pj = _positions[j];

                float effR = R * (1f + _radiusWeightK * Mathf.Sqrt(Mathf.Max(0f, wj)));
                float distN = Vector3.Distance(pi, pj) / effR;

                if (distN < 1f)
                {
                    float g = Mathf.Exp(-(distN * distN) / (2f * sigma * sigma));
                    density += wj * g;
                }
            }
            if (density > maxDensity) maxDensity = density;
        }

        const float k = 1.3f; // 민감도
        return Mathf.Max(1e-4f, maxDensity * k);
    }

    private void CollectRecursive(Transform root)
    {
        foreach (Transform child in root)
        {
            if (!child.gameObject.activeSelf) continue;
            _collected.Add(child);
            CollectRecursive(child);
        }
    }

    private bool HasActiveChild(Transform t)
    {
        for (int i = 0; i < t.childCount; i++)
            if (t.GetChild(i).gameObject.activeSelf) return true;
        return false;
    }

    private void ForEachWard(Action<HeatMapWeight> action)
    {
        if (action == null) return;
        foreach (var kv in _wardById)
        {
            var w = kv.Value;
            if (w == null) continue;
            action(w);
        }
    }
    
    private void SyncWeightSubscriptions()
    {
        foreach (var tf in _collected)
        {
            var w = tf.GetComponent<HeatMapWeight>();
            if (w != null) TrySubscribe(w);
        }

        var toRemove = new List<HeatMapWeight>();
        foreach (var w in _trackedWeights)
        {
            if (w == null || w.gameObject.activeInHierarchy == false) toRemove.Add(w);
        }
        foreach (var w in toRemove)
        {
            if (w != null)
            {
                w.OnWeightChanged -= OnAnyWeightChanged;
                // Dictionary에서도 제거
                if (string.IsNullOrEmpty(w.ID) == false)
                {
                    _wardById.Remove(w.ID);
                }
            }
            _trackedWeights.Remove(w);
        }
    }
    
    private void TrySubscribe(HeatMapWeight w)
    {
        if (_trackedWeights.Contains(w)) return;
        _trackedWeights.Add(w);
        w.OnWeightChanged += OnAnyWeightChanged;
        
        // ID가 있으면 Dictionary에도 등록
        if (string.IsNullOrEmpty(w.ID) == false)
        {
            _wardById[w.ID] = w;
        }
    }
    
    
    private void OnAnyWeightChanged(HeatMapWeight sender, float newValue)
    {
        // Weight가 자주 바뀌어도 디바운스 기간 동안 한 번만 재계산
        _dirty = true;
        _dirtyUntilTime = Time.time + _recomputeDebounce;
    }
}
