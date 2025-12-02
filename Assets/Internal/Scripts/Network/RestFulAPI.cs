using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class RestFulAPI
{
	private static void SetBodyData(UnityWebRequest request, string jsonString)
	{
		if (request == null || string.IsNullOrEmpty(jsonString))
			return;

		var data = Encoding.UTF8.GetBytes(jsonString);

		if (data.Length > 0)
		{
			request.uploadHandler = new UploadHandlerRaw(data);
			request.SetRequestHeader("Content-Type", "application/json");
		}
	}

	private static void SetHeaders(UnityWebRequest request, Dictionary<string, string> headers)
	{
		if (request == null || headers == null || headers.Count == 0)
			return;

		foreach(var header in headers)
		{
			var findHeader = request.GetRequestHeader(header.Key);

			if (string.IsNullOrEmpty(findHeader))
				request.SetRequestHeader(header.Key, header.Value);
		}
	}
	
	#region Coroutine
	
	public static IEnumerator ExecuteGetRequest(string url, Dictionary<string, string> headers, Action<UnityWebRequest.Result, DownloadHandlerBuffer> onComplete)
	{
		using (var request = UnityWebRequest.Get(url))
		{
			SetHeaders(request,headers);
			request.downloadHandler = new DownloadHandlerBuffer();
			
			yield return request.SendWebRequest();

			var downloadHandler = (request.result == UnityWebRequest.Result.Success) ? (DownloadHandlerBuffer)request.downloadHandler : null;
			
			onComplete?.Invoke(request.result, downloadHandler);
		}
	}
	
	public static IEnumerator ExecutePostRequest(string url, Dictionary<string, string> headers, string jsonBody, Action<UnityWebRequest.Result, DownloadHandlerBuffer> onComplete)
	{
		yield return ExecuteRequest("POST", url, headers, jsonBody, onComplete);
	}
	
	public static IEnumerator ExecutePostRequest(string url, Dictionary<string, string> headers, List<IMultipartFormSection> multipartFormSections, bool useGenerateBoundary, Action<UnityWebRequest.Result, DownloadHandlerBuffer> onComplete)
	{
		using (var request = useGenerateBoundary
			       ? UnityWebRequest.Post(url, multipartFormSections, UnityWebRequest.GenerateBoundary())
			       : UnityWebRequest.Post(url, multipartFormSections))
		{
			SetHeaders(request,headers);
			request.downloadHandler = new DownloadHandlerBuffer();
		
			yield return request.SendWebRequest();

			var downloadHandler = (request.result == UnityWebRequest.Result.Success) ? (DownloadHandlerBuffer)request.downloadHandler : null;

			onComplete?.Invoke(request.result, downloadHandler);
		}
	}
	
	public static IEnumerator ExecutePutRequest(string url, Dictionary<string, string> headers, string jsonBody, Action<UnityWebRequest.Result, DownloadHandlerBuffer> onComplete)
	{
		yield return ExecuteRequest("PUT", url, headers, jsonBody, onComplete);
	}
	
	public static IEnumerator ExecutePutRequest(string url, Dictionary<string, string> headers, byte[] bodyData, Action<UnityWebRequest.Result, DownloadHandlerBuffer> onComplete)
	{
		using (var request = UnityWebRequest.Put(url, bodyData))
		{
			SetHeaders(request,headers);
			request.downloadHandler = new DownloadHandlerBuffer();
			
			yield return request.SendWebRequest();

			var downloadHandler = (request.result == UnityWebRequest.Result.Success) ? (DownloadHandlerBuffer)request.downloadHandler : null;

			onComplete?.Invoke(request.result, downloadHandler);
		}
	}

	private static IEnumerator ExecuteRequest(string method, string url, Dictionary<string, string> headers, string jsonBody, Action<UnityWebRequest.Result, DownloadHandlerBuffer> onComplete)
	{
		using var request = new UnityWebRequest(url, method);
		SetHeaders(request, headers);
			
		request.downloadHandler = new DownloadHandlerBuffer();
			
		SetBodyData(request, jsonBody);
			
		yield return request.SendWebRequest();
			
		var downloadHandler = (request.result == UnityWebRequest.Result.Success) ? (DownloadHandlerBuffer)request.downloadHandler : null;

		onComplete?.Invoke(request.result, downloadHandler);
	}
	
	public static IEnumerator ExecuteGetTexture(string url, bool nonReadable, Dictionary<string, string> headers, Action<UnityWebRequest.Result, DownloadHandlerTexture> onComplete)
	{
		using (var request = UnityWebRequestTexture.GetTexture(url, nonReadable))
		{
			SetHeaders(request, headers);

			yield return request.SendWebRequest();

			DownloadHandlerTexture downloadHandler = null;

			if (request.result == UnityWebRequest.Result.Success)
				downloadHandler = (DownloadHandlerTexture)request.downloadHandler;

			onComplete?.Invoke(request.result, downloadHandler);
		}
	}

	public static IEnumerator ExecuteGetAudioClip(string url, AudioType type, Dictionary<string, string> headers, Action<UnityWebRequest.Result, DownloadHandlerAudioClip> onComplete)
	{ 
		using (var request = UnityWebRequestMultimedia.GetAudioClip(url, type))
		{
			SetHeaders(request, headers);

			yield return request.SendWebRequest();
			
			DownloadHandlerAudioClip downloadHandler = null;
			
			if (request.result == UnityWebRequest.Result.Success)
				downloadHandler = (DownloadHandlerAudioClip)request.downloadHandler;

			onComplete?.Invoke(request.result, downloadHandler);
		}
	}

	public static IEnumerator ExecuteGetFile(string url, string directory, string fileName, string extension, Dictionary<string, string> headers, Action<UnityWebRequest.Result, string> onComplete)
	{ 
		using (var request = UnityWebRequest.Get(url))
		{
			SetHeaders(request, headers);

			yield return request.SendWebRequest();
			var filePath = "";
			
			if (request.result == UnityWebRequest.Result.Success)
			{
				filePath = Path.Combine(directory, fileName);

				if (string.IsNullOrEmpty(extension) == false)
					filePath = $"{filePath}.{extension}";

				var fileData = request.downloadHandler.data;
				File.WriteAllBytes(filePath, fileData);
			}

			onComplete?.Invoke(request.result, filePath);
		}
	}
	
	#endregion
}

#region RESTfulUtils
public static class RestfulUtils
{
	public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> source, IEnumerable<KeyValuePair<TKey, TValue>> addPairs)
	{
		foreach (var kv in addPairs)
		{
			source.Add(kv);
		}
	}
	
	public static Dictionary<string, string> MergeHeaders(this Dictionary<string, string> headers1, Dictionary<string, string> headers2)
	{
		if (headers1 == null && headers2 == null)
			return null;

		Dictionary<string, string> headers = headers1;

		if (headers1 == null)
			headers = headers2;
		else if (headers2 != null)
			headers = headers1.Concat(headers2).GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => group.First().Value);

		return headers;
	}

	public static bool IsValidJson(this string jsonString)
	{
		if (string.IsNullOrEmpty(jsonString) == false)
		{
			jsonString = jsonString.Trim();
			return (jsonString.StartsWith("{") && jsonString.EndsWith("}")) || (jsonString.StartsWith("[") && jsonString.EndsWith("]"));	
		}

		return false;
	}

	public static string CombineUrlType(this string start, string end)
	{
		if (string.IsNullOrEmpty(start) == true)
			return end;

		var front = start.TrimEnd('/');
		var back = end.TrimStart('/');

		return $"{front}/{back}";
	}
}
#endregion



