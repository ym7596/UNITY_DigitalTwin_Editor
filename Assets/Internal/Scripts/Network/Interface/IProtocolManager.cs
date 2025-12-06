using System;
using System.Threading;
using Cysharp.Threading.Tasks;


public interface IProtocolManager
{
    event Action<APICategory, Enum, object> OnAction_APICall;

    UniTask RequestSnapshotAsync(CancellationToken token);
}
