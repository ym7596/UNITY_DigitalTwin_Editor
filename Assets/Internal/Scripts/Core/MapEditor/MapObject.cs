using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MapObject : MonoBehaviour
{
    private float _gridSize = 10f;
    private float _rotateDuration = 0.1f;
    private bool _isRotating = false;
    
    public string Name { get; private set; }
    public int Index { get; private set; }

    public void SetInit(string name, int index)
    {
        Name = name;
        Index = index;
    }
    
    public void MoveObject(Vector3 pos)
    {
        transform.position = new Vector3(
            RoundToNearestGrid(pos.x), 0, RoundToNearestGrid(pos.z)
        );
    }
    
    public void RotateObject(Action endCallback)
    {
        if (_isRotating)
            return;
        _isRotating = true;
        RotateObjectAsync(_rotateDuration).ContinueWith(() =>
        {
            endCallback?.Invoke();
        }).Forget();
    }

    private async UniTask RotateObjectAsync(float duration)
    {
        float time = 0f;
        Vector3 currentEuler = transform.localRotation.eulerAngles;
        
        float startValue = currentEuler.y;
        float targetValue = startValue + 45f;
        
        float fixedX = currentEuler.x;
        float fixedZ = currentEuler.z;

        while (time < duration)
        {
            float t = time / duration;
            float value = Mathf.Lerp(startValue, startValue + 45, t);
            
            transform.localRotation = Quaternion.Euler(fixedX, value, fixedZ);
        
            time += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
        }
        
        transform.localRotation = Quaternion.Euler(fixedX, targetValue, fixedZ);
        _isRotating = false;
    }
    
    private float RoundToNearestGrid(float pos)
    {
        float xDiff = pos % _gridSize;
        pos -= xDiff;
        if (xDiff > (_gridSize / 2))
        {
            pos += _gridSize;
        }
        return pos;
    }
}