using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Deque<T> : IEnumerable<T> where T : class
{
    private LinkedList<T> _list = new LinkedList<T>();
    
    public int Count => _list.Count;
    
    public void AddFirst(T item) => _list.AddFirst(item);
    public void AddLast(T item) => _list.AddLast(item);

    public T PopZero()
    {
        if (_list.Any() == false)
        {
            Debug.LogError("Deque is empty");
            return null;
        }

        var value = _list.First.Value;
        _list.RemoveFirst();
        return value;
    }
    
    public T Pop()
    {
        if (_list.Any() == false)
        {
            Debug.LogError("Deque is empty");
            return null;
        }
            
        var value = _list.Last.Value;
        _list.RemoveLast();
        return value;
    }
    
    public T PeekFirst() => _list.First.Value;

    public T PeekLast() => _list.Last.Value;

    public void Clear() => _list.Clear();

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}