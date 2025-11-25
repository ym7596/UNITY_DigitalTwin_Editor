using UnityEngine;

public class MapObjectSelectEffect : MonoBehaviour
{
    private Vector2 _originSize;

    private void Awake()
    {
        _originSize = new Vector2(transform.localScale.x, transform.localScale.y);
    }

    public void SetSize(float meanSize)
    {
        if (meanSize <= 1)
            transform.localScale = _originSize;
        transform.localScale = new Vector3(meanSize * _originSize.x, meanSize * _originSize.y, meanSize);
    }
    
    void Update()
    {
        transform.Rotate(Vector3.up, 50 * Time.deltaTime, Space.World);
    }
}
