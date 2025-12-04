using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HeatMapDataList
{
    public List<HeatMapModel> heatMaps;
}

[Serializable]
public class HeatMapModel
{
    public string id;
    public int count;
    public Vector3Format position;
}
