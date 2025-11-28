using System.Collections.Generic;
using UnityEngine;

public class MapObjectManager : MonoBehaviour
{
    [SerializeField] private EditorObjectSO _editorObjectSO;
    [SerializeField] private Transform _spawnPoint;
    
    private int _nextIndex = 0;
    private Dictionary<int, MapObject> _mapObjects = new Dictionary<int, MapObject>();

    public List<MapObjectData> GetAllObjectsData()
    {
        var dataList = new List<MapObjectData>();
        foreach (var kvp in _mapObjects)
        {
            var objData = new MapObjectData(kvp.Value.Name, kvp.Value.Index, 0);
            objData.UpdateTransform(kvp.Value.transform);
            dataList.Add(objData);
        }
        return dataList;
    }
    
    public void Create(string id)
    {
        var mo = CreateMapObject(id);
        mo.gameObject.layer = LayerMask.NameToLayer(Config.InteractableItemLayerName);
        _mapObjects[mo.Index] = mo;
    }

    private MapObject CreateMapObject(string id)
    {
        var obj = _editorObjectSO?.MapObjectPrefabs.Find(x => x.id == id);

        if (obj == null)
        {
            Debug.Log($"Cannot find {id} in SO");
            return null;
        }

        var go = Instantiate(obj.prefab, _spawnPoint, true);

        var mo = go.GetComponent<MapObject>();
        if(mo ==null)
            mo = go.AddComponent<MapObject>();
        mo.SetInit(obj.id,_nextIndex);
        _nextIndex++;
        return mo;
    }
}
