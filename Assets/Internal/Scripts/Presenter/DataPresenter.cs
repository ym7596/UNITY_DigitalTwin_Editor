using System;
using System.Threading;
using UnityEngine;
using VContainer.Unity;

public class DataPresenter : IInitializable, IStartable, IDisposable, IDataPresenterHandler
{
    private IProtocolManager _protocolManager;

    private CancellationTokenSource _cts;
    
    public event Action<APICategory, Enum, object> OnAction_DataPresenterResponse;

    public DataPresenter(IProtocolManager handler)
    {
        _protocolManager = handler;
    }
    
    public void Initialize()
    {
       _protocolManager.OnAction_APICall -= HandleAPIResponse;
       _protocolManager.OnAction_APICall += HandleAPIResponse;
    }

    public void Start()
    {
       _cts?.Cancel();
       _cts = new CancellationTokenSource();
       
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _protocolManager.OnAction_APICall -= HandleAPIResponse;
    }

    private void HandleAPIResponse(APICategory category, Enum apiType, object payload)
    {
        if (payload == null || category == APICategory.None)
        {
            Debug.LogWarning("API Response is null");
            return;
        }
        OnAction_DataPresenterResponse?.Invoke(category, apiType, payload);
    }
    
    
}
