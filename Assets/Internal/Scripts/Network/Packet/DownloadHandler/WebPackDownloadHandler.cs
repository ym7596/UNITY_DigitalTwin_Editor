using UnityEngine.Networking;

public class WebPackDownloadHandler
{
	public string text { get; private set; }
	public byte[] data { get; private set; }
	
	public void SetDownloadHandler(DownloadHandler downloadHandler)
	{
		SetText(downloadHandler);
		SetBytes(downloadHandler);
	}

	private void SetText(DownloadHandler downloadHandler)
	{
		text = downloadHandler?.text;
	}
	
	private void SetBytes(DownloadHandler downloadHandler)
	{
		data = downloadHandler?.data;
	}
}


