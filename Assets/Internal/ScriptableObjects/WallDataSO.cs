using UnityEngine;

[CreateAssetMenu(fileName = "WallData", menuName = "MYSO/WallData")]
public class WallDataSO : ScriptableObject
{
    public float wallHeight;
    public float wallThickness;
    public Material wallMaterial;
}