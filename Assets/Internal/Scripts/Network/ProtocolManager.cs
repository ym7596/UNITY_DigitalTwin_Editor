using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VContainer;

public class ProtocolManager : MonoBehaviour
{
    [Inject] private RestFulAPI _restFulAPI;
    [SerializeField] private EndpointSO _endpointSO;

    private CancellationTokenSource _cts;
    
    public event Action<APICategory, Enum, object> OnRequest_API;
    
    private void Start()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        _ = GetLoadData(_cts.Token);
    }

    private async UniTask GetLoadData(CancellationToken token)
    {
        var packet = new WebPacket(_endpointSO.GetUrl(APICategory.Common, (int)CommonEndPoint.Load));

        Debug.Log(_endpointSO.GetUrl(APICategory.Common, (int)CommonEndPoint.Load));

        var result = await packet.ExecuteRequestAsync(token);

        if (result == UnityWebRequest.Result.Success)
        {
            var json = packet.DeserializeData<SaveDataModel>();
            Debug.Log(json.id);
        }
    }
}
