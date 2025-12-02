using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(APIEndPoint))]
public class EndpointDrawer : PropertyDrawer
{

    private const float PaddingV = 6f;
    private const float PaddingH = 8f;
    
    private static readonly Dictionary<APICategory, Type> _categoryTypeCache = new Dictionary<APICategory, Type>();
    private static readonly Dictionary<Type, string[]> _enumNameCache = new Dictionary<Type, string[]>();
    private static readonly Dictionary<Type, int[]> _enumValuesCache = new Dictionary<Type, int[]>();
    
   
    private static readonly GUIContent _fieldApiType = new GUIContent("API Type");
    private static readonly GUIContent _fieldApiTypeValue = new GUIContent("API Type Value");
    private static readonly GUIContent _enumMissingInfo = new GUIContent("엔드포인트 Enum 타입을 찾을 수 없습니다. 숫자 값으로 입력하세요.");

    [InitializeOnLoadMethod]
    private static void ClearCache()
    {
        _categoryTypeCache.Clear();
        _enumNameCache.Clear();
        _enumValuesCache.Clear();   
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        GUI.Box(position, GUIContent.none, EditorStyles.helpBox);

        var contentRect = new Rect(
            position.x + PaddingH,
            position.y + PaddingV,
            position.width - PaddingH * 2f,
            position.height - PaddingV * 2f
        );
        var categoryProp = property.FindPropertyRelative("apiCategory");
        var typeValueProp = property.FindPropertyRelative("apiTypeValue");
        var urlProp = property.FindPropertyRelative("url");
        float line = EditorGUIUtility.singleLineHeight;
        float space = EditorGUIUtility.standardVerticalSpacing;
        Rect row = new Rect(contentRect.x, contentRect.y, contentRect.width, line);

        // 1) Category
        EditorGUI.PropertyField(row, categoryProp);
        row.y += line + space;
        // 2) Type: 카테고리에 따라 Enum 타입 결정
        var category = (APICategory)categoryProp.enumValueIndex;
        var enumType = GetEnumTypeByCategory(category);
        if (enumType != null && enumType.IsEnum)
        {
            var names = GetEnumNames(enumType);
            var values = GetEnumValues(enumType);


            int currentValue = typeValueProp.intValue;
            int currentIndex = IndexOf(values, currentValue);
            if (currentIndex < 0) currentIndex = 0;

            int selectedIndex = EditorGUI.Popup(row, _fieldApiType.text, currentIndex, names);
            typeValueProp.intValue = values[selectedIndex];
            row.y += line + space;
        }
        else
        {
            float helpHeight = EditorStyles.helpBox.CalcHeight(_enumMissingInfo, contentRect.width);
            EditorGUI.HelpBox(new Rect(row.x, row.y, row.width, helpHeight), _enumMissingInfo.text, MessageType.Info);
            row.y += helpHeight + space;

            typeValueProp.intValue = EditorGUI.IntField(new Rect(row.x, row.y, row.width, line), _fieldApiTypeValue, typeValueProp.intValue);
            row.y += line + space;
        }
        EditorGUI.PropertyField(row, urlProp);
        row.y += line + space;
        EditorGUI.EndProperty();
    }
    
    private static Type GetEnumTypeByCategory(APICategory category)
    {
        if (_categoryTypeCache.TryGetValue(category, out var t))
            return t;

        // 카테고리별 명시적 매핑
        switch (category)
        {
            case APICategory.Common:
                t = FindEnumTypeByName("CommonEndPoint");
                break;
            case APICategory.Map:
                t = FindEnumTypeByName("MapEndPoint");
                break;
            default:
                t = null;
                break;
        }
          
        
        if (t != null)
        {
            _categoryTypeCache[category] = t;
        }
        return t;
    }

    private static Type FindEnumTypeByName(string typeName)
    {
        // 빠른 경로: mscorlib 등 제외, 현재 도메인 한 번 순회
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asms.Length; i++)
        {
            Type[] types;
            try { types = asms[i].GetTypes(); }
            catch { continue; }

            for (int j = 0; j < types.Length; j++)
            {
                var tt = types[j];
                if (tt.IsEnum && tt.Name == typeName)
                    return tt;
            }
        }
        return null;
    }

    private static string[] GetEnumNames(Type enumType)
    {
        if (_enumNameCache.TryGetValue(enumType, out var names))
            return names;

        var raw = Enum.GetNames(enumType);
        _enumNameCache[enumType] = raw;
        return raw;
    }

    private static int[] GetEnumValues(Type enumType)
    {
        if (_enumValuesCache.TryGetValue(enumType, out var values))
            return values;

        var raw = Enum.GetValues(enumType);
        int len = raw.Length;
        var ints = new int[len];
        for (int i = 0; i < len; i++)
            ints[i] = Convert.ToInt32(raw.GetValue(i));

        _enumValuesCache[enumType] = ints;
        return ints;
    }

    private static int IndexOf(int[] arr, int value)
    {
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == value) return i;
        return -1;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var categoryProp = property.FindPropertyRelative("apiCategory");
        var category = (APICategory)categoryProp.enumValueIndex;

        float line = EditorGUIUtility.singleLineHeight;
        float space = EditorGUIUtility.standardVerticalSpacing;
        float viewWidth = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 40f);

        float contentHeight = 0f;

        // Category
        contentHeight += line + space;

        // Type
        var enumType = GetEnumTypeByCategory(category);
        if (enumType != null && enumType.IsEnum)
        {
            contentHeight += line + space; // Popup
        }
        else
        {
            float helpHeight = EditorStyles.helpBox.CalcHeight(_enumMissingInfo, viewWidth);
            contentHeight += helpHeight + space; // HelpBox
            contentHeight += line + space;       // IntField
        }

        // URL
        contentHeight += line + space;
        
        // 패딩 포함
        return contentHeight + PaddingV * 2f;
    }

}
