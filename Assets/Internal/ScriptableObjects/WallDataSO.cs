using UnityEngine;

[CreateAssetMenu(fileName = "WallData", menuName = "MYSO/WallData")]
public class WallDataSO : ScriptableObject
{
    public float wallHeight;
    public float wallThickness;
    public Material wallMaterial;
    [Header("DWG / 화면 대비 축소/확대 비율")] 
    public float magnificationRate;
}