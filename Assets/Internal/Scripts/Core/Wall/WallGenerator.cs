using System;
using System.Collections.Generic;
using System.Linq;
using ACadSharp;
using ACadSharp.Types.Units;
using CadToUnityPlugin;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;

public class WallGenerator : MonoBehaviour
{
    [SerializeField] private WallDataSO _wallDataSO;
    [SerializeField] private Transform _wallParent;

    private UIManager _uiManager;
    private WallPathManager _wallPathManager;

    private Vector3 _medianPosition;
    
    #region Wall Creator Values
    
    [SerializeField] private DwgPluginSetting _dwgPluginSetting;

    private GameObject _dwgObject;
    private DwgLoader _dwgLoader;
    private DwgDrawer _dwgDrawer;
    
    private string _previousFileNameOrUrl;
    private Material _dwgMaterial;

    private float _unit = 0;
    
    public bool autoLoad;
    public LoadType loadType;
    public DrawType drawType;
    public bool isBlockPrefab = false;
    public string fileName;
    public string url;
    
    #endregion
    
    private void Awake()
    {
        _wallPathManager = new WallPathManager(_wallDataSO.wallMaterial, _wallParent, _wallDataSO.wallHeight, _wallDataSO.wallThickness, _wallDataSO.magnificationRate);
        if (autoLoad == false) return;
        var fileNameOrUrl = loadType == LoadType.StreamingAssets ? fileName : url;
        _ = MakeDwgAsync(fileNameOrUrl, loadType, drawType);
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
    
    #region DWG Loader

    private void Clear()
    {
        
    }
    
    private async UniTask<GameObject> MakeDwgAsync(string url, LoadType loadType, DrawType drawType)
    {
        var cadDocument = await LoadCadDocumentAsync(url, loadType);
        var dwgRawObject = await DrawDwgObjectAsync(_dwgPluginSetting, cadDocument, drawType);
        
        var allLineRenderer = dwgRawObject.GetComponentsInChildren<LineRenderer>();
        var lrList = _wallPathManager.LineRendererToList(allLineRenderer);
        var convertToVector2 = VertexPointUtil.ConvertListVectorToVector2(lrList);
        var geometricMedian = VertexPointUtil.GeometricMedian(convertToVector2);
        
        _medianPosition = new Vector3(-geometricMedian.x * _wallDataSO.magnificationRate, 0, -geometricMedian.y * _wallDataSO.magnificationRate);
        SetLineRendererSizePosition(allLineRenderer, _medianPosition, _wallDataSO.magnificationRate);
        
        Debug.Log(_medianPosition);
        _wallPathManager.SetMedianPosition(_medianPosition);
        _wallPathManager.CreateWallByDwgFile(lrList);
        _wallPathManager.FixAllIntersections();
        
        _wallParent.position = _medianPosition;
        return dwgRawObject;
    }

    private async UniTask<CadDocument> LoadCadDocumentAsync(string url, LoadType loadType)
    {
        if (_previousFileNameOrUrl == url)
        {
            Debug.LogWarning($"{url} is loading or already loaded.");
            return null;
        }

        if (_previousFileNameOrUrl != null)
        {
            Debug.LogWarning($"Delete {_previousFileNameOrUrl} and Load {url}");
            Clear();
        }
        _previousFileNameOrUrl = url;
        _dwgLoader ??= new DwgLoader();
        
        CadDocument cadDocument;
        
        switch (loadType)
        {
            case LoadType.StreamingAssets:
                cadDocument = await _dwgLoader.LoadStreamingAssetsFolderDwgAsync(url);
                break;
            case LoadType.Download:
                cadDocument = await _dwgLoader.LoadRemoteDwgAsync(url);
                break;
            case LoadType.FilePicker:
                cadDocument = await _dwgLoader.LoadDwgWithFilePickerAsync();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(loadType), loadType, null);
        }
        
        if (cadDocument == null)
        {
            throw new Exception("cadDocument is null");
        }
        return cadDocument;
    }
    
    private async UniTask<GameObject> DrawDwgObjectAsync(DwgPluginSetting newDwgPluginSetting, CadDocument cadDocument, DrawType newDrawType)
    {
        _dwgDrawer ??= new DwgDrawer();
        _unit = GetUnit(cadDocument);
        
        switch (newDrawType)
        {
            case DrawType.Sync:
                _dwgObject = _dwgDrawer.Draw(newDwgPluginSetting, cadDocument, _unit);
                break;
            case DrawType.Async:
                _dwgObject = await _dwgDrawer.DrawAsync(newDwgPluginSetting, cadDocument, _unit, isBlockPrefab);
                break;
            default:
                Debug.LogError("dwgObject is null");
                break;
        }

        if (_dwgObject is null)
        {
            Debug.LogError("dwgObject is null");
            return null;
        }
        return _dwgObject;
    }
    
    private void SetLineRendererSizePosition(LineRenderer[] lineRenderers,Vector3 medianPos, float magnificationRate)
    {
        if (lineRenderers == null || lineRenderers.Length == 0) return;

        foreach (var lr in lineRenderers)
        {
            if (lr == null) continue;

            int count = lr.positionCount;
            if (count <= 0) continue;

            var positions = new Vector3[count];
            lr.GetPositions(positions);

            for (int i = 0; i < count; i++)
            {
                // x, z에만 배율 적용 후 medianPos 이동
                positions[i].x = positions[i].x * magnificationRate + medianPos.x;
                positions[i].z = positions[i].z * magnificationRate + medianPos.z;
                // positions[i].y 그대로 유지 (필요하면 + medianPos.y 추가)
            }

            lr.SetPositions(positions);
            lr.SetColor(Color.green);
            lr.startWidth = magnificationRate;
            lr.endWidth = magnificationRate;
        }
    }
    
    private float GetUnit(CadDocument cadDocument)
    {
        var units = cadDocument.Header.InsUnits;
        Debug.Log($"Units => {units}");
        float unit;
        switch (units)
        {
            case UnitsType.Millimeters:
                unit = 0.0254f;
                break;
            case UnitsType.Centimeters:
                unit = 0.01f;
                break;
            case UnitsType.Meters:
                unit = 1f;
                break;
            case UnitsType.Kilometers:
                unit = 10f;
                break;
            case UnitsType.Unitless:
                unit = 0.0254f;
                break;
            default:
                unit = 0f;
                break;
        }
        Debug.Log($"unit : {unit}");
        return unit;
    }
    #endregion
}

