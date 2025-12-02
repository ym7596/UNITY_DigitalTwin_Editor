using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class WebPacket : IWebPacket
{
    private const string AuthorizationKey = "Authorization";
    
    public string AuthorizationToken { get; set; } = "";
    public string Url { get; set; }
    public Dictionary<string, string> Headers => GetHeader();
    
    public WebPackDownloadHandler DownloadHandler { get; protected set; } = null;
    
    private Dictionary<string, string> _headers = new Dictionary<string, string>();

    public WebPacket(string url, string authorizationToken = "")
    {
        Url = url;
        SetAuthorizationToken(authorizationToken);
    }
    public WebPacket AddParameter(string key, object value)
    {
        if (string.IsNullOrEmpty(key))
            return this;

        var dict = new Dictionary<string, string>(1)
        {
            [key] = ConvertToString(value)
        };
        AddParameters(dict); // 기존 메서드 재사용
        return this;
    }

    // 다중 파라미터 추가: 튜플 가변인자
    public WebPacket AddParameters(params (string key, object value)[] items)
    {
        if (items == null || items.Length == 0)
            return this;

        var dict = new Dictionary<string, string>(items.Length);
        for (int i = 0; i < items.Length; i++)
        {
            var (k, v) = items[i];
            if (string.IsNullOrEmpty(k)) continue;
            dict[k] = ConvertToString(v);
        }
        AddParameters(dict); // 기존 메서드 재사용
        return this;
    }

    // 익명객체/POCO에서 프로퍼티를 읽어 파라미터로 추가
    public WebPacket AddParameters(object obj)
    {
        if (obj == null)
            return this;

        var dict = new Dictionary<string, string>();
        var props = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        for (int i = 0; i < props.Length; i++)
        {
            var p = props[i];
            if (!p.CanRead) continue;
            var name = p.Name;
            var val = p.GetValue(obj, null);
            if (val == null) continue;
            dict[name] = ConvertToString(val);
        }
        AddParameters(dict); // 기존 메서드 재사용
        return this;
    }

    // 현재 시각 타임스탬프를 지정 키로 추가(기본 키: currentDateTime)
    public WebPacket AddNowTimestamp(string key = "currentDateTime", string format = "yyyy-MM-dd HH:mm:ss")
    {
        return AddParameter(key, DateTime.Now.ToString(format));
    }

    public void SetAuthorizationToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return;
        
        AuthorizationToken = token;
    }
    
    public void AddHeader(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            return;

        if (Headers.TryAdd(key, value) == false)
            Headers[key] = value;
    }
    
    public void RemoveHeader(string key)
    {
        if (string.IsNullOrEmpty(key) || Headers.ContainsKey(key) == false) 
            return;

        Headers.Remove(key);
    }

    public void AddParameters(Dictionary<string, string> parameters, bool encode = true)
    {
        if (parameters == null || parameters.Count == 0)
            return;

        if (string.IsNullOrEmpty(Url))
            return;

        var sb = new StringBuilder();
        bool hasQuery = Url.IndexOf('?') >= 0;
        bool first = true;

        foreach (var kv in parameters)
        {
            if (string.IsNullOrEmpty(kv.Key))
                continue;

            string key = encode ? Encode(kv.Key) : (kv.Key ?? string.Empty);
            string val = encode ? Encode(kv.Value ?? string.Empty) : (kv.Value ?? string.Empty);

            if (first)
            {
                sb.Append(hasQuery ? '&' : '?');
                first = false;
            }
            else
            {
                sb.Append('&');
            }

            sb.Append(key).Append('=').Append(val);
        }

        if (sb.Length > 0)
            Url += sb.ToString();
    }

    // 이미 만들어진 raw query 스트링을 그대로 추가하고 싶을 때 사용. (예: "a=1&b=2")
    public void AppendRawQuery(string rawQuery)
    {
        if (string.IsNullOrEmpty(rawQuery) || string.IsNullOrEmpty(Url))
            return;

        bool hasQuery = Url.IndexOf('?') >= 0;
        if (rawQuery.StartsWith("?")) rawQuery = rawQuery.Substring(1);
        Url += hasQuery ? "&" : "?";
        Url += rawQuery;
    }

    private static string Encode(string s)
    {
        // RFC 3986 준수 인코딩
        return Uri.EscapeDataString(s ?? string.Empty);
    }


    public Dictionary<string, string> GetHeader()
    {
        if (string.IsNullOrEmpty(AuthorizationToken))
            return _headers;
        
        var headers = new Dictionary<string, string>()
        {
            { AuthorizationKey, $"Bearer {AuthorizationToken}" }
        };
        
        if(_headers.Count > 0)
            headers.AddRange(_headers);
        
        return headers;
    }
    
    public virtual IEnumerator ExecuteRequest(Action<UnityWebRequest.Result> onComplete)
    {
        return RestFulAPI.ExecuteGetRequest(Url, GetHeader(), (result, handler) =>
        {
            if (result == UnityWebRequest.Result.Success)
                SetDownloadHandler(handler);

            onComplete?.Invoke(result);
        });
    }
    
    public async UniTask<UnityWebRequest.Result> ExecuteRequestAsync(CancellationToken ct)
    {
        using (var req = UnityWebRequest.Get(Url))
        {
            foreach (var kv in GetHeader())
                req.SetRequestHeader(kv.Key, kv.Value);
            
            await req.SendWebRequest().WithCancellation(ct);

            if (req.result == UnityWebRequest.Result.Success)
                SetDownloadHandler(req.downloadHandler);

            return req.result;
        }
    }

    public virtual JArray DeserializeArray()
    {
        var jsonString = DownloadHandler?.text;
        
        return string.IsNullOrEmpty(jsonString) ? null : JArray.Parse(jsonString);
    }

    public virtual JObject Deserialize()
    {
        var jsonString = DownloadHandler?.text;

        return string.IsNullOrEmpty(jsonString) ? null : JObject.Parse(jsonString);
    }
    
    public virtual T Deserialize<T>()
    {
        var jsonString = DownloadHandler?.text;

        return string.IsNullOrEmpty(jsonString) ? default : JsonConvert.DeserializeObject<T>(jsonString);
    }
    
    public virtual JObject DeserializeData()
    {
        var jsonString = DownloadHandler != null ? DownloadHandler.text : "";
        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
        
        return string.IsNullOrEmpty(jsonString) ? null : JObject.Parse(jsonData["data"].ToString());
    }
    
    public virtual JArray DeserializeDataArray()
    {
        var jsonString = DownloadHandler?.text;

        if (string.IsNullOrEmpty(jsonString))
            return null;

        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        if (jsonData.TryGetValue("data", out var dataObj) == false)
            return null;

        return JArray.Parse(dataObj.ToString());
    }
    
    public virtual List<T> DeserializeDataArray<T>()
    {
        var jsonString = DownloadHandler?.text;

        if (string.IsNullOrEmpty(jsonString))
            return null;

        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        if (jsonData.TryGetValue("data", out var dataObj) == false)
            return null;

        return JsonConvert.DeserializeObject<List<T>>(dataObj.ToString());
    }
    
    public virtual List<T> DeserializeNestedDataArray<T>(string nestedKey)
    {
        var jsonString = DownloadHandler?.text;

        if (string.IsNullOrEmpty(jsonString))
            return null;

        try
        {
            var rootData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            
            if (!rootData.TryGetValue("data", out var dataObj))
                return null;

            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataObj.ToString());
            
            if (!dataDict.TryGetValue(nestedKey, out var arrayObj))
                return null;

            return JsonConvert.DeserializeObject<List<T>>(arrayObj.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to deserialize nested array '{nestedKey}': {ex.Message}");
            return null;
        }
    }

    public virtual T DeserializeData<T>()
    {
        var jsonString = DownloadHandler?.text;

        if (string.IsNullOrEmpty(jsonString))
            return default;
        
        var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        if (jsonData.TryGetValue("data", out var dataObj) == false)
            return default;
        
        return JsonConvert.DeserializeObject<T>(dataObj.ToString());
    }

    protected void SetDownloadHandler(DownloadHandler downloadHandler)
    {
        DownloadHandler ??= new WebPackDownloadHandler();
        DownloadHandler.SetDownloadHandler(downloadHandler);
    }
    
    private static string ConvertToString(object value)
    {
        if (value == null) return string.Empty;

        switch (value)
        {
            case string s:
                return s;
            case DateTime dt:
                // 서버 요구 형식에 맞춰 기본 포맷 고정
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            case bool b:
                // 서버 요구 사항에 맞게 true/false 혹은 1/0 선택
                return b ? "true" : "false";
            default:
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}


