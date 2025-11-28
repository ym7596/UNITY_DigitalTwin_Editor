using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EditorObjectSO", menuName = "MYSO/EditorObjectSO")]
public class EditorObjectSO : ScriptableObject
{
    [Serializable]
    public class MapObjectPrefab
    {
        public string id;
        public GameObject prefab;
    }
    
    [SerializeField] private List<MapObjectPrefab> _mapObjectPrefabs;
    
    public List<MapObjectPrefab> MapObjectPrefabs => _mapObjectPrefabs;
}
