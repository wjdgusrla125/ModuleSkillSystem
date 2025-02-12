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
    
    // Data의 Level과 Data 삭제를 위한 X Button을 그려주는 Foldout Title을 그려줌
    protected bool DrawRemovableLevelFoldout(SerializedProperty datasProperty, SerializedProperty targetProperty,
        int targetIndex, bool isDrawRemoveButton)
    {
        // Data를 삭제했는지에 대한 결과
        bool isRemoveButtonClicked = false;

        EditorGUILayout.BeginHorizontal();
        {
            GUI.color = Color.green;
            var level = targetProperty.FindPropertyRelative("level").intValue;
            // Data의 Level을 보여주는 Foldout GUI를 그려줌
            targetProperty.isExpanded = EditorGUILayout.Foldout(targetProperty.isExpanded, $"Level {level}");
            GUI.color = Color.white;

            if (isDrawRemoveButton)
            {
                GUI.color = Color.red;
                if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    isRemoveButtonClicked = true;
                    // EffectDatas에서 현재 Data를 Index를 이용해 지움
                    datasProperty.DeleteArrayElementAtIndex(targetIndex);
                }
                GUI.color = Color.white;
            }
        }
        EditorGUILayout.EndHorizontal();

        return isRemoveButtonClicked;
    }
    
    protected void DrawAutoSortLevelProperty(SerializedProperty datasProperty, SerializedProperty levelProperty,
    int index, bool isEditable)
{
    if (!isEditable)
    {
        GUI.enabled = false;
        EditorGUILayout.PropertyField(levelProperty);
        GUI.enabled = true;
    }
    else
    {
        // Property가 수정되었는지 감시 시작
        EditorGUI.BeginChangeCheck();
        // 수정전 Level을 기록해둠
        var prevValue = levelProperty.intValue;
        // levelProperty를 Delayed 방식으로 그려줌
        // 키보드 Enter Key를 눌러야 입력한 값이 반영됨, Enter Key를 누르지않고 빠져나가면 원래 값으로 돌아옴.
        EditorGUILayout.DelayedIntField(levelProperty);
        // Property가 수정되었을 경우 true 반환
        if (EditorGUI.EndChangeCheck())
        {
            if (levelProperty.intValue <= 1)
                levelProperty.intValue = prevValue;
            else
            {
                // EffectDatas를 순회하여 같은 level을 가진 data가 이미 있으면 수정 전 level로 되돌림
                for (int i = 0; i < datasProperty.arraySize; i++)
                {
                    // 확인해야하는 Data가 현재 Data와 동일하다면 Skip
                    if (index == i)
                        continue;

                    var element = datasProperty.GetArrayElementAtIndex(i);
                    // Level이 똑같으면 현재 Data의 Level을 수정 전으로 되돌림
                    if (element.FindPropertyRelative("level").intValue == levelProperty.intValue)
                    {
                        levelProperty.intValue = prevValue;
                        break;
                    }
                }

                // Level이 정상적으로 수정되었다면 오름차순 정렬 작업 실행
                if (levelProperty.intValue != prevValue)
                {
                    // 현재 Data의 Level이 i번째 Data의 Level보다 작으면, 현재 Data를 i번째로 옮김
                    // ex. 1 2 4 5 (3) => 1 2 (3) 4 5
                    for (int moveIndex = 1; moveIndex < datasProperty.arraySize; moveIndex++)
                    {
                        if (moveIndex == index)
                            continue;

                        var element = datasProperty.GetArrayElementAtIndex(moveIndex).FindPropertyRelative("level");
                        if (levelProperty.intValue < element.intValue || moveIndex == (datasProperty.arraySize - 1))
                        {
                            datasProperty.MoveArrayElement(index, moveIndex);
                            break;
                        }
                    }
                }
            }
        }
    }
}
}
