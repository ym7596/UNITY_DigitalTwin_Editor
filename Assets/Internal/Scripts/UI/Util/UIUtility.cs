using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class UIUtility
{
    public static bool GetRayHit(Vector2 pos, out RaycastHit hit)
    {
        hit = default;
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(pos);
            if (Physics.Raycast(ray, out hit))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsUITouch(this Vector2 point)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = point
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData,raycastResults);
        if (raycastResults.Count > 0)
        {
            foreach (var r in raycastResults)
            {
                if (r.gameObject.layer == LayerMask.NameToLayer("UI"))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
