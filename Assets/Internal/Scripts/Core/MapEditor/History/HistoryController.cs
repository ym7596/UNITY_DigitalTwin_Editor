using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class HistoryController
{
    private Deque<IHistory> _undoDeque = new Deque<IHistory>();
    private Deque<IHistory> _redoDeque = new Deque<IHistory>();

    private int _maxHistoryCount = 20;

    public void ExecuteHistory(IHistory history)
    {
        history.Redo();
        if (_undoDeque.Count >= _maxHistoryCount)
        {
            IHistory oldest = null;
            try
            {
                oldest = _undoDeque.PopZero();
            }
            catch (Exception ex)
            {
                Debug.LogError($"히스토리 처리 중 오류 발생: {ex.Message}");
            }
            
            if (oldest is VisibilityHistory goHistory)
            {
                var target = goHistory.Target;
                if (target != null && target.activeSelf == false)
                {
                    Object.Destroy(target);
                    Debug.Log($"비활성 게임오브젝트 제거  {target.name}");
                }
            }
        }
        
        _undoDeque.AddLast(history);
        _redoDeque.Clear();
    }

    public void Undo()
    {
        if (_undoDeque.Any() == true)
        {
            IHistory history = _undoDeque.Pop();
            history.Undo();
            _redoDeque.AddLast(history);
        }
    }

    public void Redo()
    {
        if (_redoDeque.Any() == true)
        {
            IHistory history = _redoDeque.Pop();
            history.Redo();
            _undoDeque.AddLast(history);
        }
    }
}
