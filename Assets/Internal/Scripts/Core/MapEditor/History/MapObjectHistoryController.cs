using UnityEngine;

public class MapObjectHistoryController
{
   private HistoryController _historyController;
   private GameObject _target;
   private TransformInfo _beforeTransform;

   public MapObjectHistoryController()
   {
      _historyController = new HistoryController();
   }
   
   public void SetObjectCancel()
   {
      ApplyTransform();
      _target = null;
   }

   public void SetActionItemGameObject(GameObject go)
   {
      _target = go;
      _beforeTransform = new TransformInfo(_target.transform);  
   }
    
   public void SetBeforeTransformInfo()
   {
      _beforeTransform =  new TransformInfo(_target.transform);
   }
    
   public void SaveTransformAfter()
   {
      ApplyTransform();
   }
   
   private void ApplyTransform()
   {
      var afterTransformInfo = new TransformInfo(_target.transform);
      if (afterTransformInfo == _beforeTransform)
         return;
      var history = new TransformHistory(_target, _beforeTransform, afterTransformInfo);
      _historyController.ExecuteHistory(history);
      
      _beforeTransform = afterTransformInfo;
   }
   
   public void ApplyVisibleGameObject(GameObject go,bool before,bool after)
   {
      var history = new VisibilityHistory(go,before,after);
      _historyController.ExecuteHistory(history);
   }
   
   public void Undo()
   {
      _historyController.Undo();
   }

   public void Redo()
   {
      _historyController.Redo();
   }
}
