using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Sprites;
using UnityEngine.UI;

public class UIDrawPoint : Graphic, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private Sprite _pointSprite;
    [SerializeField] private float _pointSize;
    
    public DrawVertex LinkedVertex { get; set; }
   private DrawActionType _currentActionType = DrawActionType.None;
   private RectTransform _rectTransform;
   private bool _isDragging = false;
    
   // 이벤트 시스템
   public event PointMoveHandler OnPointMoved;
   public event PointSelectedHandler OnPointSelected;
    
   // 색상 속성
   public Color normalColor = Color.red;
   public Color hoverColor = Color.yellow;
   public Color selectedColor = Color.green;
   
   protected override void Awake()
   {
      base.Awake();
      _rectTransform = GetComponent<RectTransform>();
      
      _rectTransform.sizeDelta = new Vector2(_pointSize, _pointSize);
   }
   
   protected override void OnPopulateMesh(VertexHelper vh)
   {
      vh.Clear();
      if (_pointSprite == null)
      {
         Debug.LogWarning("No sprite assigned to UI Draw Point");
         return;
      }

      var rect = GetPixelAdjustedRect();
      var uv = DataUtility.GetOuterUV(_pointSprite);
      var color32 = color;

      UIVertex v = UIVertex.simpleVert;
      v.color = color32;

      v.uv0 = new Vector2(uv.x, uv.y); v.position = new Vector2(-_pointSize, -_pointSize); 
      vh.AddVert(v);
      v.uv0 = new Vector2(uv.x, uv.w); v.position = new Vector2(-_pointSize, _pointSize); 
      vh.AddVert(v);
      v.uv0 = new Vector2(uv.z, uv.w); v.position = new Vector2(_pointSize, _pointSize); 
      vh.AddVert(v);
      v.uv0 = new Vector2(uv.z, uv.y); v.position = new Vector2(_pointSize, -_pointSize); 
      vh.AddVert(v);

      vh.AddTriangle(0, 1, 2);
      vh.AddTriangle(2, 3, 0);
   }
   
   public override Texture mainTexture => _pointSprite != null ? _pointSprite.texture : base.mainTexture;

   public void SetActionType(DrawActionType actionType)
   {
      _currentActionType = actionType;
   }
   
   #region Pointer Handler
   
   public void OnPointerEnter(PointerEventData eventData)
   {
      if (_isDragging == false)
      {
         color = hoverColor;
         SetAllDirty();
      }
   }

   public void OnPointerExit(PointerEventData eventData)
   {
      if (_isDragging == false && color != selectedColor)
      {
         color = normalColor;
         SetAllDirty();
      }
   }

   public void OnPointerDown(PointerEventData eventData)
   {
      if (_currentActionType == DrawActionType.PointEdit)
      {
         if (eventData.button == PointerEventData.InputButton.Left)
         {
            _isDragging = true;
            color = selectedColor;
            SetAllDirty();
            OnPointSelected?.Invoke(this);
         }
      }
   }

   public void OnPointerUp(PointerEventData eventData)
   {
      if (eventData.button == PointerEventData.InputButton.Left)
      {
         if(_isDragging == true)
            OnPointMoved?.Invoke(this,  LinkedVertex.Position);
         _isDragging = false;
      }
   }

   public void OnDrag(PointerEventData eventData)
   {
      if (_isDragging && _currentActionType == DrawActionType.PointEdit)
      {
         Vector2 newPosition;
         if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera,
                out newPosition))
         {
            _rectTransform.anchoredPosition = newPosition;
            if(LinkedVertex != null)
               LinkedVertex.Position = newPosition;
           
         }
      }
   }
   
   public void SetSelected(bool selected)
   {
      color = selected ? selectedColor : normalColor;
      SetAllDirty();
   }
   
   #endregion Pointer Handler
}
