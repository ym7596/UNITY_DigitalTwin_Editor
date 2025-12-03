using System;
using UnityEngine;

public interface IDataPresenterHandler
{
    event Action<APICategory, Enum, object> OnAction_DataPresenterResponse;
}
