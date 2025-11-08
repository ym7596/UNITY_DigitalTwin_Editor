using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

public class UIDrawPoint : Graphic
{
    [SerializeField] private Sprite _pointSprite;
    [SerializeField] private float _pointSize;
    
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        
        var rect = GetPixelAdjustedRect();
        var uv = DataUtility.GetOuterUV(_pointSprite);
        var color32 = color;
        
        UIVertex v = UIVertex.simpleVert;
        v.color = color32;

        v.uv0 = new Vector2(uv.x, uv.y); 
        v.position = new Vector2(- _pointSize, -_pointSize);
        vh.AddVert(v);
        
        v.uv0 = new Vector2(uv.x, uv.w); 
        v.position = new Vector2(- _pointSize, _pointSize);
        vh.AddVert(v);

        v.uv0 = new Vector2(uv.z, uv.w); 
        v.position = new Vector2( _pointSize, _pointSize);
        vh.AddVert(v);

        v.uv0 = new Vector2(uv.z, uv.y); 
        v.position = new Vector2( _pointSize, -_pointSize);
        vh.AddVert(v);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    public override Texture mainTexture => _pointSprite.texture;
}
