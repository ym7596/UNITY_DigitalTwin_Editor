using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public interface IWebPacket
{
    string Url { get; }
    string AuthorizationToken { get; set; }
    WebPackDownloadHandler DownloadHandler { get; }
    IEnumerator ExecuteRequest(Action<UnityWebRequest.Result> onComplete);
    T Deserialize<T>();
    JObject Deserialize();
}



