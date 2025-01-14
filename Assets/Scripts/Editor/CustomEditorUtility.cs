using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
}
