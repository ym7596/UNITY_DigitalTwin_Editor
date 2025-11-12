using System;
using ACadSharp;
using ACadSharp.Types.Units;
using UnityEngine;
using CadToUnityPlugin;
using Cysharp.Threading.Tasks;

public class DWGLoadDev : MonoBehaviour
{
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


    private void Start()
    {
        if (autoLoad == false) return;
        var fileNameOrUrl = loadType == LoadType.StreamingAssets ? fileName : url;
        _ = MakeDwgAsync(fileNameOrUrl, loadType, drawType);
    }

    private void Clear()
    {
        
    }

    private async UniTask<GameObject> MakeDwgAsync(string url, LoadType loadType, DrawType drawType)
    {
        var cadDocument = await LoadCadDocumentAsync(url, loadType);
        var dwgRawObject = await DrawDwgObjectAsync(_dwgPluginSetting, cadDocument, drawType);
        
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
}
