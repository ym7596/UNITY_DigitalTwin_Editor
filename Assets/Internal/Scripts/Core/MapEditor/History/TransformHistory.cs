using UnityEngine;

public class TransformHistory : IHistory
{
    private GameObject _target;
    private TransformInfo _beforeTransform;
    private TransformInfo _afterTransform;

    public TransformHistory(GameObject target, TransformInfo beforeTransform, TransformInfo afterTransform)
    {
        _target = target;
        _beforeTransform = beforeTransform;
        _afterTransform = afterTransform;
    }
    
    public void Undo()
    {
        _target.transform.position = _beforeTransform.pos;
        _target.transform.localScale = _beforeTransform.scl;
        _target.transform.rotation = _beforeTransform.rot;
    }

    public void Redo()
    {
        _target.transform.position = _afterTransform.pos;
        _target.transform.localScale = _afterTransform.scl;
        _target.transform.rotation = _afterTransform.rot;
    }
}

public class TransformInfo
{
    public Vector3 pos;
    public Vector3 scl;
    public Quaternion rot;

    public TransformInfo(Transform t)
    {
        pos = t.position;
        scl = t.localScale;
        rot = t.rotation;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is not TransformInfo other) return false;
        return pos == other.pos && scl == other.scl && rot == other.rot;
    }

    public override int GetHashCode()
    {
        return pos.GetHashCode() ^ scl.GetHashCode() ^ rot.GetHashCode();
    }

    public static bool operator ==(TransformInfo a, TransformInfo b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(TransformInfo a, TransformInfo b)
    {
        return !(a == b);
    }
}