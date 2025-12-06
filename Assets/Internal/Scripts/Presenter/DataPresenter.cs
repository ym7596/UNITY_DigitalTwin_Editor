using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public class DataPresenter : IInitializable, IStartable, IDisposable, IDataPresenterHandler
{
    private IProtocolManager _protocolManager;

    private CancellationTokenSource _cts;
    
    #region Scheduler
    
    private const int IntervalSec = 10;   // 10초
    private const int JitterMaxSec = 10;   // 군집 요청 방지용 지터
    private const int BackoffMinSec = 30;  // 에러 시 최소 백오프
    private const int BackoffMaxSec = 300; // 에러 시 최대 백오프(5분)

    private bool _inFlight;
    #endregion
    
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
       RunSchedulerAsync(_cts.Token).Forget();
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
    
    
    #region Scheduler Method

    private async UniTaskVoid RunSchedulerAsync(CancellationToken token)
    {
        Debug.Log("[DataPresenter] RunSchedulerAsync Start");

        int backOff = 0;
        
        await SafeSnapshotOnce(token, backOff);

        while (token.IsCancellationRequested == false)
        {
            var baseDelay = backOff > 0 ? backOff : IntervalSec;
            var jitter = UnityEngine.Random.Range(0, JitterMaxSec + 1);
            var totalDelay = baseDelay + jitter;

            Debug.Log($"[DataPresenter]  Next poll in {totalDelay}s");

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(totalDelay),
                    DelayType.UnscaledDeltaTime, 
                    PlayerLoopTiming.Update, 
                    token
                );
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[DataPresenter]  Scheduler cancelled");
                break;
            }

            await SafeSnapshotOnce(token, backOff);
        }
        
        
    }

    private async UniTask SafeSnapshotOnce(CancellationToken token, int backoffSec)
    {
        if (_inFlight == true) return;
        _inFlight = true;

        try
        {
            await _protocolManager.RequestSnapshotAsync(token);
            backoffSec = 0;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DataPresenter] SafeSnapshotOnce Error: {ex.Message}");
            backoffSec = backoffSec == 0 ? BackoffMinSec : Mathf.Min(backoffSec * 2, BackoffMaxSec);
        }
        finally
        {
            _inFlight = false;
        }
    }
    
    #endregion
    
    
}
