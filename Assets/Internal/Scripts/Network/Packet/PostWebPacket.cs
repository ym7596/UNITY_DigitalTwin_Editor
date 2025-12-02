using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class PostWebPacket : WebPacket
{
    public string BodyString { get; private set; }
    public List<IMultipartFormSection> MultipartFormSections { get; private set; }
    public bool UseGenerateBoundary { get; set; } = false;

    private bool _isMultipartFormSections => MultipartFormSections != null;
    
    public PostWebPacket(string url, string bodyString, string authorizationToken = "") : base(url, authorizationToken)
    {
        SetBody(bodyString);
    }
    
    public PostWebPacket(string url, List<IMultipartFormSection> multipartFormSections, bool useGenerateBoundary, string authorizationToken = "") : base(url, authorizationToken)
    {
        if (multipartFormSections != null && multipartFormSections.Count > 0)
           MultipartFormSections = multipartFormSections;
        
        UseGenerateBoundary = useGenerateBoundary;
    }
    
    public void AddMultipartFormSection(IMultipartFormSection section)
    {
        if (MultipartFormSections is null)
            MultipartFormSections = new List<IMultipartFormSection>();

        var index = MultipartFormSections.FindIndex(inSection => inSection.sectionName == section.sectionName);

        if (index == -1)
            MultipartFormSections.Add(section);
        else
            MultipartFormSections[index] = section;
    }

    public void RemoveMultipartFormSection(string sectionName)
    {
        MultipartFormSections.RemoveAll(inSection => inSection.sectionName == sectionName);
    }

    public bool SetBody<T>(T data)
    {
        if (data.Equals(default))
            return false;

        BodyString = JsonConvert.SerializeObject(data);
		
        return true;
    }

    public bool SetBody(string jsonBody)
    {
        if (string.IsNullOrEmpty(jsonBody) || jsonBody.IsValidJson() == false)
            return false;
		
        BodyString = jsonBody;
		
        return true;
    }

    public override IEnumerator ExecuteRequest(Action<UnityWebRequest.Result> onComplete)
    {
        if (_isMultipartFormSections)
        {
            yield return ExecuteMultipartFormRequest(onComplete);
        }
          
        else if (string.IsNullOrEmpty(BodyString) == false)
        {
            yield return ExecuteBodyStringRequest(onComplete);
        } 
           
        else
            onComplete?.Invoke(UnityWebRequest.Result.DataProcessingError);
    }

    private IEnumerator ExecuteMultipartFormRequest(Action<UnityWebRequest.Result> onComplete)
    {
        yield return RestFulAPI.ExecutePostRequest(Url, GetHeader(), MultipartFormSections, UseGenerateBoundary,
            (result, handler) =>
            {
                if(result == UnityWebRequest.Result.Success)
                    SetDownloadHandler(handler);

                onComplete?.Invoke(result);
            });
    }

    private IEnumerator ExecuteBodyStringRequest(Action<UnityWebRequest.Result> onComplete)
    {
        yield return RestFulAPI.ExecutePostRequest(Url, GetHeader(), BodyString,
            (result, handler) =>
            {
                if(result == UnityWebRequest.Result.Success)
                    SetDownloadHandler(handler);

                onComplete?.Invoke(result);
            });
    }
}


