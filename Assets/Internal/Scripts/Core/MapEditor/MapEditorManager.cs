using System;
using UnityEngine;

public class MapEditorManager : MonoBehaviour
{
    [SerializeField] private InputManager _inputManager;
    [SerializeField] private GameObject _selectEffectPrefab;
    private MainPresenter _presenter;
    private MapEditorObjectHandler _objectHandler;
    private MapObjectHistoryController _historyController;

    private Vector3 _moveValue;
    private Camera _mainCamera;
    private Plane _groundPlane = new Plane(Vector3.up, 0);
    
    private MapObjectRemoteActionType _currentMapObjectRemoteActionType = MapObjectRemoteActionType.None;
    
    public void SetPresenter(MainPresenter presenter, MapObjectHistoryController historyController)
    {
        _presenter = presenter;
        _objectHandler = new MapEditorObjectHandler(presenter, _selectEffectPrefab);
        _historyController = historyController;
    }

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        _inputManager.OnAction_RaycastHit += OnLeftClick;
        _inputManager.OnAction_RightClicked += OnRightClick;
    }

    private void OnDisable()
    {
        _inputManager.OnAction_RaycastHit -= OnLeftClick;
        _inputManager.OnAction_RightClicked -= OnRightClick;
    }

    private void Update()
    {
        if (_objectHandler.HasSelection() == true && _currentMapObjectRemoteActionType == MapObjectRemoteActionType.Move)
        {
            _moveValue = _inputManager.PositionValue;
            
            if (_mainCamera == null)
            {
                Debug.LogError("Camera.main이 null입니다!");
                return;
            }
            var ray = _mainCamera.ScreenPointToRay(_moveValue);

            if (_groundPlane.Raycast(ray, out var enter))
            {
                Vector3 worldPoint = ray.GetPoint(enter);
                _objectHandler.CurrentSelected.MoveObject(worldPoint);
            }
        }
    }

    public void SetActionType(MapObjectRemoteActionType actionType)
    {
        var mapObject = _objectHandler.CurrentSelected;
        _currentMapObjectRemoteActionType = actionType;
        switch (actionType)
        {
            case MapObjectRemoteActionType.Move:
            {
                break;
            }
            case MapObjectRemoteActionType.Rotate:
            {
                mapObject.RotateObject(() =>
                {
                    //do something
                });
                break;
            }
            case MapObjectRemoteActionType.Delete:
            {
                mapObject.gameObject.SetActive(false);
                _objectHandler.Deselect();
                
                break;
            }
            default: break;
        }
    }

    private void OnLeftClick(RaycastHit hit)
    {
        var hitObject = hit.collider.gameObject;
        Debug.Log(hitObject.name);
        if (hitObject != null)
        {
            if (hitObject.layer == LayerMask.NameToLayer(Config.InteractableItemLayerName))
            {
                if (_objectHandler.CurrentSelected != null)
                {
                    _objectHandler.Deselect();
                    _currentMapObjectRemoteActionType = MapObjectRemoteActionType.None;
                }
                else
                    _objectHandler.Select(hitObject);
            }
            else
            {
                if (_objectHandler.CurrentSelected != null)
                {
                    _objectHandler.Deselect();
                    _currentMapObjectRemoteActionType = MapObjectRemoteActionType.None;
                }
            }
        }
    }

    private void OnRightClick()
    {
        if (_objectHandler.HasSelection() == true)
        {
            _objectHandler.Deselect();
            _currentMapObjectRemoteActionType = MapObjectRemoteActionType.None;
        }
    }
}
