using UnityEngine;

public class UIEditorRemoteController : MonoBehaviour
{
    [SerializeField] private UIMapObjectEditButton[] _editButtons;
    
    private UIManager _uiManager;

    public void Init(UIManager uiManager)
    {
        _uiManager = uiManager;
        foreach (var btn in _editButtons)
        {
            btn.OnAction_EditObject += HandleEditButtonClick;
        }
    }
    
    private void OnDestroy()
    {
        foreach (var btn in _editButtons)
        {
            btn.OnAction_EditObject -= HandleEditButtonClick;
        }
    }
    
    private void HandleEditButtonClick(MapObjectRemoteActionType actionType)
    {
        _uiManager.OnClick_ObjectActionType(actionType);
    }
    
}
