using UnityEngine;

public class MapEditorObjectHandler
{
    private MapObjectSelectEffect _selectEffectObject;
    
    private MainPresenter _presenter;

    private MapObject _currentSelected;
    
    public MapObject CurrentSelected => _currentSelected;

    public MapEditorObjectHandler(MainPresenter presenter, GameObject selectEffectObject)
    {
        _presenter = presenter;
        _selectEffectObject = Object.Instantiate(selectEffectObject).GetComponent<MapObjectSelectEffect>();
        _selectEffectObject.gameObject.SetActive(false);
    }
    
    public void Select(GameObject selectedGameObject)
    {
        _currentSelected = selectedGameObject.GetComponent<MapObject>();
        if (_currentSelected == null)
        {
            return;
        }
        
        _selectEffectObject.transform.SetParent(_currentSelected.transform);
        _selectEffectObject.transform.localPosition = new Vector3(0, 0.05f, 0);
        var box = _currentSelected.GetComponent<BoxCollider>();
        if (box != null)
        {
            float mean = (box.size.x + box.size.y + box.size.z) / 3f;
            _selectEffectObject.SetSize(mean);
        }

        _selectEffectObject.gameObject.SetActive(true);
        
        _presenter.ActiveRemoteController(true);
    }

    public void Deselect()
    {
        _currentSelected = null;
        
        _selectEffectObject.transform.SetParent(null);
        _selectEffectObject.gameObject.SetActive(false);
        
        _presenter.ActiveRemoteController(false);
    }

    public bool HasSelection() => _currentSelected != null;
}
