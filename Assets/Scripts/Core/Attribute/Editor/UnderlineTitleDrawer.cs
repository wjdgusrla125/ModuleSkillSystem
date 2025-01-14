using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(UnderlineTitleAttribute))]
public class UnderlineTitleDrawer : DecoratorDrawer
{
    public override void OnGUI(Rect position)
    {
        var attributeAsUnderlineTitle = attribute as UnderlineTitleAttribute;
        
        position = EditorGUI.IndentedRect(position);
        position.height = EditorGUIUtility.singleLineHeight;
        position.y += attributeAsUnderlineTitle.Space;
        
        GUI.Label(position, attributeAsUnderlineTitle.Title, EditorStyles.boldLabel);
        
        position.y += EditorGUIUtility.singleLineHeight;
        position.height = 1f;
        
        EditorGUI.DrawRect(position, Color.gray);
    }

    public override float GetHeight()
    {
        var attributeAsUnderlineTitle = attribute as UnderlineTitleAttribute;
        
        return EditorGUIUtility.singleLineHeight + attributeAsUnderlineTitle.Space + (EditorGUIUtility.standardVerticalSpacing * 2);
    }
}
