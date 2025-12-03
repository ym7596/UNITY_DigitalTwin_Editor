using System;


public interface IProtocolManager
{
    event Action<APICategory, Enum, object> OnAction_APICall;
}
