using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(IdentifiedObject), true)]
public class IdentifiedObjectEditor : Editor
{
    private SerializedProperty categoriesProperty;
    private SerializedProperty iconProperty;
    private SerializedProperty idProperty;
    private SerializedProperty codeNameProperty;
    private SerializedProperty displayNameProperty;
    private SerializedProperty descriptionProperty;

    private ReorderableList categories;
    
    private GUIStyle textAreaStyle;

    private readonly Dictionary<string, bool> isFoldoutExpandedesByTitle = new();

    protected virtual void OnEnable()
    {
        GUIUtility.keyboardControl = 0;
        
        categoriesProperty = serializedObject.FindProperty("categories");
        iconProperty = serializedObject.FindProperty("icon");
        idProperty = serializedObject.FindProperty("id");
        codeNameProperty = serializedObject.FindProperty("codeName");
        displayNameProperty = serializedObject.FindProperty("displayName");
        descriptionProperty = serializedObject.FindProperty("description");
        
        categories = new(serializedObject, categoriesProperty);
        categories.drawHeaderCallback = rect => EditorGUI.LabelField(rect, categoriesProperty.displayName);
        categories.drawElementCallback = (rect, index, active, focused) =>
        {
            rect = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(rect, categoriesProperty.GetArrayElementAtIndex(index), GUIContent.none);
        };
    }

    private void StyleSetup()
    {
        if (textAreaStyle == null)
        {
            textAreaStyle = new(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
        }
    }
    
    protected bool DrawFoldoutTitle(string text)
    => CustomEditorUtility.DrawFoldoutTitle(isFoldoutExpandedesByTitle, text);

    public override void OnInspectorGUI()
    {
        StyleSetup();
        serializedObject.Update();
        categories.DoLayoutList();

        if (DrawFoldoutTitle("Information"))
        {
            EditorGUILayout.BeginHorizontal("HelpBox");
            {
                iconProperty.objectReferenceValue = EditorGUILayout.ObjectField(GUIContent.none, iconProperty.objectReferenceValue,
                    typeof(Sprite), false, GUILayout.Width(65));
                
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUI.enabled = false;
                        EditorGUILayout.PrefixLabel("ID");
                        EditorGUILayout.PropertyField(idProperty, GUIContent.none);
                        GUI.enabled = true;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUI.BeginChangeCheck();
                    var prevCodeName = codeNameProperty.stringValue;
                    EditorGUILayout.DelayedTextField(codeNameProperty);

                    if (EditorGUI.EndChangeCheck())
                    {
                        var assetPath = AssetDatabase.GetAssetPath(target);
                        var newName = $"{target.GetType().Name.ToUpper()}_{codeNameProperty.stringValue}";
                        serializedObject.ApplyModifiedProperties();
                        var message = AssetDatabase.RenameAsset(assetPath, newName);
                        
                        if (string.IsNullOrEmpty(message))
                            target.name = newName;
                        else
                            codeNameProperty.stringValue = prevCodeName;
                    }
                    
                    EditorGUILayout.PropertyField(displayNameProperty);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginVertical("HelpBox");
            {
                EditorGUILayout.LabelField("Description");
                descriptionProperty.stringValue = EditorGUILayout.TextArea(descriptionProperty.stringValue,
                    textAreaStyle, GUILayout.Height(60));
            }
            
            EditorGUILayout.EndVertical();
        }
        
        serializedObject.ApplyModifiedProperties();
        return;
    }
}
