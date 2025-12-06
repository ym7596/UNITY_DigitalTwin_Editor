using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VContainer;

public class ProtocolManager : MonoBehaviour, IProtocolManager
{
    [Inject] private RestFulAPI _restFulAPI;
    [SerializeField] private EndpointSO _endpointSO;

    private CancellationTokenSource _cts;
    
    public event Action<APICategory, Enum, object> OnAction_APICall;
    
    public async UniTask RequestSnapshotAsync(CancellationToken token)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: token);
        await GetHeatmapData(token);
    }

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
            OnAction_APICall?.Invoke(APICategory.Common, CommonEndPoint.Load, json);
           
        }
    }

    private async UniTask GetHeatmapData(CancellationToken token)
    {
        var packet = new WebPacket(_endpointSO.GetUrl(APICategory.Map, (int)MapEndPoint.Heatmap));
        var result = await packet.ExecuteRequestAsync(token);

        if (result == UnityWebRequest.Result.Success)
        {
            var json = packet.DeserializeData<HeatMapDataList>();
            foreach (var j in json.heatMaps)
            {
                Debug.Log(j.id);
            }
            OnAction_APICall?.Invoke(APICategory.Map, MapEndPoint.Heatmap, json);
        }
    }

    
}
