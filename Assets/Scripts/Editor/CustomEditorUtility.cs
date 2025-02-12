using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public static class CustomEditorUtility
{
    private readonly static GUIStyle titleStyle;

    static CustomEditorUtility()
    {
        titleStyle = new GUIStyle("ShurikenModuleTitle")
        {
            font = new GUIStyle(EditorStyles.label).font,
            fontStyle = FontStyle.Bold,
            border = new RectOffset(15, 7, 4, 4),
            fixedHeight = 26f,
            contentOffset = new Vector2(20f, -2f)
        };
    }

    public static bool DrawFoldoutTitle(string title, bool isExpanded, float space = 15f)
    {
        EditorGUILayout.Space(space);

        var rect = GUILayoutUtility.GetLastRect();
        
        GUI.Box(rect, title, titleStyle);
        
        var currentEvent = Event.current;
        var toggleRect = new Rect(rect.x + 4f, rect.y + 4f, 13f, 13f);

        if (currentEvent.type == EventType.Repaint)
        {
            EditorStyles.foldout.Draw(toggleRect, false, false, isExpanded, false);
        }
        else if (currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition))
        {
            isExpanded = !isExpanded;
            currentEvent.Use();
        }
        
        EditorGUILayout.Space(10f);
        
        return isExpanded;
    }

    public static bool DrawFoldoutTitle(IDictionary<string, bool> isFoldoutExpandedesByTitle, string title, float space = 15f)
    {
        if (!isFoldoutExpandedesByTitle.ContainsKey(title))
        {
            isFoldoutExpandedesByTitle[title] = true;
        }
        
        isFoldoutExpandedesByTitle[title] = DrawFoldoutTitle(title, isFoldoutExpandedesByTitle[title], space);
        return isFoldoutExpandedesByTitle[title];
    }

    public static void DrawUnderline(float height = 1f)
    {
        var lastRect = GUILayoutUtility.GetLastRect();
        
        lastRect.y += lastRect.height;
        lastRect.height = height;
        
        EditorGUI.DrawRect(lastRect, Color.gray);
    }
    
    public static void DrawEnumToolbar(SerializedProperty enumProperty)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(enumProperty.displayName);
        enumProperty.enumValueIndex = GUILayout.Toolbar(enumProperty.enumValueIndex, enumProperty.enumDisplayNames);
        EditorGUILayout.EndHorizontal();
    }
    
    // T는 Deep Copy할 객체의 Type
    public static void DeepCopySerializeReference(SerializedProperty property)
    {
        // managedReferenceValue는 SerializeReference Attribute를 적용한 변수
        if (property.managedReferenceValue == null)
            return;

        property.managedReferenceValue = (property.managedReferenceValue as ICloneable).Clone();
    }

    public static void DeepCopySerializeReferenceArray(SerializedProperty property, string fieldName = "")
    {
        for (int i = 0; i < property.arraySize; i++)
        {
            // Array에서 Element를 가져옴
            var elementProperty = property.GetArrayElementAtIndex(i);
            // Element가 일반 class나 struct라서 Element 내부에 SerializeReference 변수가 있을 수 있으므로,
            // fieldName이 Empty가 아니라면 Elenemt에서 fieldName 변수 정보를 찾아옴
            if (!string.IsNullOrEmpty(fieldName))
                elementProperty = elementProperty.FindPropertyRelative(fieldName);

            if (elementProperty.managedReferenceValue == null)
                continue;

            // 찾아온 정보를 이용해서 property의 manageredRefenceValue에서 Clone 함수를 실행시킴
            elementProperty.managedReferenceValue = (elementProperty.managedReferenceValue as ICloneable).Clone();
        }
    }
}
