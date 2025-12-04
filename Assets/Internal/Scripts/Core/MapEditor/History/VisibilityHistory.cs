using UnityEngine;

public class VisibilityHistory : IHistory
{
    private GameObject _target;

    private bool _beforeVisible;
    private bool _afterVisible;
    public GameObject Target => _target; // 외부에서 접근 가능하게
    public bool IsDeleted => !_afterVisible;
    public VisibilityHistory(GameObject target,bool beforeVisible, bool afterVisible)
    {
        _target = target;
        _beforeVisible = beforeVisible;
        _afterVisible = afterVisible;
    }
    
    public void Undo()
    {
        if (_target != null)
            _target.SetActive(_beforeVisible);
    }

    public void Redo()
    {
        if (_target != null)
            _target.SetActive(_afterVisible);
    }
}
