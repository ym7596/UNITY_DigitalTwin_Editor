using System;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;


public enum LoadLocationType
{
    PersistentData,
    StreamingAssets
}

public class SaveManager : MonoBehaviour
{
    [SerializeField] private string _jsonFileName = "mapData.json";
    [SerializeField] private LoadLocationType _loadLocationType = LoadLocationType.StreamingAssets;
    
    private bool _isSaving = false;

#region Save

    public void SaveToJson(SaveDataModel saveData)
    {
        StartCoroutine(SaveToJsonCo(saveData));
    }

    private IEnumerator SaveToJsonCo(SaveDataModel saveData)
    {
        if(_isSaving) yield break;
        
        _isSaving = true;

        JObject jData = new JObject
        {
            ["id"] = saveData.id
        };
        
        JArray wallPath = new JArray();
        foreach (var wall in saveData.mapWallPathData)
        {
            JArray wallPathData = new JArray();
            foreach (var vertex in wall)
            {
                wallPathData.Add(SetVector2ToJObject(vertex));
            }
            wallPath.Add(wallPathData);
        }

        jData["mapWallPathData"] = wallPath;
        
        var jsonData = JsonConvert.SerializeObject(jData, Formatting.Indented);
        var filePath = $"{Application.persistentDataPath}/{_jsonFileName}";
        
        File.WriteAllText(filePath, jsonData);

        Debug.Log($"[SaveLoadManager] Map 저장 완료: {filePath}");
        
        yield return null;
        _isSaving = false;
    }
    
#endregion
#region Load
    public async UniTask<SaveDataModel> LoadMapDataAsync(LoadLocationType location)
    {
        string jsonData = null;

        try
        {
            switch (location)
            {
                case LoadLocationType.PersistentData:
                    jsonData = LoadFromPersistentData(_jsonFileName);
                    break;
                        
                case LoadLocationType.StreamingAssets:
                    jsonData = await LoadFromStreamingAssetsAsync(_jsonFileName);
                    break;
                
            }

            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogWarning($"[SaveManager] 데이터 파일을 찾을 수 없습니다. Location: {location}");
                return null;
            }

            SaveDataModel datas = JsonConvert.DeserializeObject<SaveDataModel>(jsonData);
            Debug.Log($"[SaveManager]  로드 완료 from {location}");
            return datas;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager]  로드 실패: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }
    
    private string LoadFromPersistentData(string fileName)
    {
        var filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        if (File.Exists(filePath))
        {
            Debug.Log($"[SaveLoadManager] Loading from PersistentData: {filePath}");
            return File.ReadAllText(filePath);
        }

        return null;
    }

    private async UniTask<string> LoadFromStreamingAssetsAsync(string fileName)
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.Log($"[SaveLoadManager] Loading from StreamingAssets (async): {filePath}");

        using (var request = UnityWebRequest.Get(filePath))
        {
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return request.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"[SaveLoadManager] StreamingAssets 로드 실패: {request.error}");
                return null;
            }
        }
    }
    
#endregion
    
    private JObject SetVector2ToJObject(Vector2 val)
    {
        return new JObject
        {
            ["x"] = val.x,
            ["y"] = val.y
        };
    }
}
