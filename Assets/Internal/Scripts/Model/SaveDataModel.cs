using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SaveDataModel
{
   public string id;
   public List<List<Vector2>> mapWallPathData = new List<List<Vector2>>();
   public List<MapObjectData> mapObjects = new List<MapObjectData>();
}

[Serializable]
public class MapObjectData
{
   public string name;
   public int index = 0;
   public int type = 0;

   public Vector3Format position;
   public Vector3Format rotation;
   public Vector3Format scale;
    
   public MapObjectData(string name, int index, int type)
   {
      this.name = name;
      this.index = index;
      this.type = type;
   }
    
   public void UpdateTransform(Transform transform)
   {
      position = new Vector3Format(transform.localPosition);
      rotation = new Vector3Format(transform.localRotation.eulerAngles);
      scale = new Vector3Format(transform.localScale);
   }
    
   public Vector3 GetVector3(Vector3Format f)
   {
      return new Vector3(f.x, f.y, f.z);
   }
}

public class Vector3Format
{
   public float x;
   public float y;
   public float z;

   public Vector3Format(Vector3 v)
   {
      this.x = v.x;
      this.y = v.y;
      this.z = v.z;
   }
}
