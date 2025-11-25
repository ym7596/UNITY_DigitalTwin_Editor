using UnityEngine;

public class Config
{
    public const string InteractableItemLayerName = "Moveable";
}

public enum DrawActionType
{
    None,
    PointEdit,
    PointCreate,
    Viewer
}

public enum MapObjectRemoteActionType
{
    None,
    Move,
    Rotate,
    Delete,
    Info
}