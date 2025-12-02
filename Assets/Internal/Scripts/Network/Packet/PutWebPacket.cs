using System;
using System.Collections;
using UnityEngine.Networking;

public class PutWebPacket : WebPacket
{
    public string BodyString { get; private set; }

    public PutWebPacket(string url,string bodyString, string authorizationToken = "") : base(url, authorizationToken)
    {
        SetBody(bodyString);
    }

    public override IEnumerator ExecuteRequest(Action<UnityWebRequest.Result> onComplete)
    {
        yield return RestFulAPI.ExecutePutRequest(Url, GetHeader(), BodyString, (result, handler) =>
        {
            if (result == UnityWebRequest.Result.Success)
            {
                SetDownloadHandler(handler);
            }
            onComplete?.Invoke(result);
        });
    }
    
    public bool SetBody(string jsonBody)
    {
        if (string.IsNullOrEmpty(jsonBody) || jsonBody.IsValidJson() == false)
            return false;
		
        BodyString = jsonBody;
		
        return true;
    }
    
}
