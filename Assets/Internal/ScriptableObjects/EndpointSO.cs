using System;
using System.Collections.Generic;
using UnityEngine;

public enum ServerMode
{
    Devel,
    Product
}

public enum APICategory
{
    None,
    Common,
    Map
}

[Serializable]
public class APIEndPoint
{
    public APICategory apiCategory;
    public string url;
    // enum 값 저장용 int
    public int apiTypeValue;
}

[Serializable]
public class DomainEndPoint
{
    public ServerMode mode;
    public string url;
}

[CreateAssetMenu(fileName = "EndpointSO", menuName = "MYSO/EndpointSO")]
public class EndpointSO : ScriptableObject
{
    [field: SerializeField] public ServerMode ServerMode { get; private set; } = ServerMode.Devel;
    
    [SerializeField] private List<DomainEndPoint> _domainEndPoints = new List<DomainEndPoint>();
    [SerializeField] private string _devAuthToken = "";
    [SerializeField] private string _devRefreshToken = "";
    
    [Header("[Base]")]
    [SerializeField] private List<APIEndPoint> _baseApiEndPoints = new List<APIEndPoint>();
    [Header("[Map]")]
    [SerializeField] private List<APIEndPoint> _mapApiEndPoints = new List<APIEndPoint>();
    
    public string GetUrl(APICategory apiCategory, int apiTypeValue)
    {
        var domainEntry = _domainEndPoints.Find(x => x.mode == ServerMode);
        if (domainEntry == null || string.IsNullOrEmpty(domainEntry.url))
        {
            Debug.LogWarning($"[EndpointSO] Domain not configured for mode: {ServerMode}");
            return string.Empty;
        }

        var list = GetEndPointList(apiCategory);
        var endpoint = list.Find(x => x.apiCategory == apiCategory && x.apiTypeValue == apiTypeValue)?.url;

        if (string.IsNullOrEmpty(endpoint))
        {
            Debug.LogWarning($"[EndpointSO] API endpoint not found. Category={apiCategory}, TypeValue={apiTypeValue}");
            return string.Empty;
        }

        // Device/Map/Ward는 별도 프리픽스 없이 단순 조합
        var path = endpoint;
        return $"{domainEntry.url}/{path}";
    }

    private List<APIEndPoint> GetEndPointList(APICategory apiCategory)
    {
        switch (apiCategory)
        {
            case APICategory.Common:
                return _baseApiEndPoints;
            case APICategory.Map:
                return _mapApiEndPoints;
            default:
                return _baseApiEndPoints;
        }
    }
}
