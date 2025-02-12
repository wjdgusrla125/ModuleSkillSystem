using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Effect))]
public class EffectEditor : IdentifiedObjectEditor
{
    private SerializedProperty typeProperty;
    private SerializedProperty isAllowDuplicateProperty;
    private SerializedProperty removeDuplicateTargetOptionProperty;

    private SerializedProperty isShowInUIProperty;

    private SerializedProperty isAllowLevelExceedDatasProperty;
    private SerializedProperty maxLevelProperty;
    private SerializedProperty effectDatasProperty;

    protected override void OnEnable()
    {
        base.OnEnable();

        typeProperty = serializedObject.FindProperty("type");
        isAllowDuplicateProperty = serializedObject.FindProperty("isAllowDuplicate");
        removeDuplicateTargetOptionProperty = serializedObject.FindProperty("removeDuplicateTargetOption");

        isShowInUIProperty = serializedObject.FindProperty("isShowInUI");

        isAllowLevelExceedDatasProperty = serializedObject.FindProperty("isAllowLevelExceedDatas");

        maxLevelProperty = serializedObject.FindProperty("maxLevel");
        effectDatasProperty = serializedObject.FindProperty("effectDatas");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        // Lebel(=Inpector창에 표시되는 변수의 이름)의 길이를 늘림;
        float prevLevelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 175f;

        DrawSettings();
        DrawOptions();
        DrawEffectDatas();

        // Label의 길이를 원래대로 되돌림
        EditorGUIUtility.labelWidth = prevLevelWidth;

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSettings()
    {
        if (!DrawFoldoutTitle("Setting"))
            return;
        
        // Enum을 Toolbar 형태로 그려줌
        CustomEditorUtility.DrawEnumToolbar(typeProperty);

        EditorGUILayout.Space();
        CustomEditorUtility.DrawUnderline();
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(isAllowDuplicateProperty);
        // 중복 적용 허용 Option이 true라면 중복 Effect를 지울 필요가 없으므로
        // removeDuplicateTargetOption 변수를 그리지 않음
        if (!isAllowDuplicateProperty.boolValue)
            CustomEditorUtility.DrawEnumToolbar(removeDuplicateTargetOptionProperty);
    }

    private void DrawOptions()
    {
        if (!DrawFoldoutTitle("Option"))
            return;

        EditorGUILayout.PropertyField(isShowInUIProperty);
    }

    private void DrawEffectDatas()
    {
        // Effect의 Data가 아무것도 존재하지 않으면 1개를 자동적으로 만들어줌
        if (effectDatasProperty.arraySize == 0)
        {
            // 배열 길이를 늘려서 새로운 Element를 생성
            effectDatasProperty.arraySize++;
            // 추가한 Data의 Level을 1로 설정
            effectDatasProperty.GetArrayElementAtIndex(0).FindPropertyRelative("level").intValue = 1;
        }

        if (!DrawFoldoutTitle("Data"))
            return;

        EditorGUILayout.PropertyField(isAllowLevelExceedDatasProperty);

        // Level 상한 제한이 없다면 MaxLevel을 그대로 그려주고,
        // 상한 제한이 있다면 MaxLevel을 상한으로 고정 시키는 작업을 함
        if (isAllowLevelExceedDatasProperty.boolValue)
            EditorGUILayout.PropertyField(maxLevelProperty);
        else
        {
            // Property를 수정하지 못하게 GUI Enable의 false로 바꿈
            GUI.enabled = false;
            // 마지막 EffectData(= 가장 높은 Level의 Data)를 가져옴
            var lastEffectData = effectDatasProperty.GetArrayElementAtIndex(effectDatasProperty.arraySize - 1);
            // maxLevel을 마지막 Data의 Level로 고정
            maxLevelProperty.intValue = lastEffectData.FindPropertyRelative("level").intValue;
            // maxLevel Property를 그려줌
            EditorGUILayout.PropertyField(maxLevelProperty);
            GUI.enabled = true;
        }

        // effectDatas를 돌면서 GUI를 그려줌
        for (int i = 0; i < effectDatasProperty.arraySize; i++)
        {
            var property = effectDatasProperty.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("HelpBox");
            {
                // Data의 Level과 Data 삭제를 위한 X Button을 그려주는 Foldout Title을 그려줌
                // 단, 첫번째 Data(= index 0) 지우면 안되기 때문에 X Button을 그려주지 않음
                // X Button을 눌러서 Data가 지워지면 true를 return함
                if (DrawRemovableLevelFoldout(effectDatasProperty, property, i, i != 0))
                {
                    // Data가 삭제되었으며 더 이상 GUI를 그리지 않고 바로 빠져나감
                    // 다음 Frame에 처음부터 다시 그리기 위함
                    EditorGUILayout.EndVertical();
                    break;
                }

                if (property.isExpanded)
                {
                    // 들여쓰기
                    EditorGUI.indentLevel += 1;

                    var levelProperty = property.FindPropertyRelative("level");
                    // Level Property를 그려주면서 Level 값이 수정되면 Level을 기준으로 EffectDatas를 오름차순으로 정렬해줌
                    DrawAutoSortLevelProperty(effectDatasProperty, levelProperty, i, i != 0);

                    var maxStackProperty = property.FindPropertyRelative("maxStack");
                    EditorGUILayout.PropertyField(maxStackProperty);
                    // maxStack의 최소 값을 1 이하로 내리지 못하게 함
                    maxStackProperty.intValue = Mathf.Max(maxStackProperty.intValue, 1);

                    var stackActionsProperty = property.FindPropertyRelative("stackActions");
                    var prevStackActionsSize = stackActionsProperty.arraySize;

                    EditorGUILayout.PropertyField(stackActionsProperty);

                    // stackActions에 Element가 추가됐다면, 새로 추가된 Element의 Soft Copy된 action 변수를 Deep Copy 해줌
                    if (stackActionsProperty.arraySize > prevStackActionsSize)
                    {
                        // 마지막 배열 Element(=새로 만들어진 Element)를 가져옴
                        var lastStackActionProperty = stackActionsProperty.GetArrayElementAtIndex(prevStackActionsSize);
                        // Elememy에서 action Property를 찾아옴
                        var actionProperty = lastStackActionProperty.FindPropertyRelative("action");
                        // Deep Copy 해줌
                        CustomEditorUtility.DeepCopySerializeReference(actionProperty);
                    }

                    // StackAction들의 stack 변수에 입력 가능한 최대 값을 MaxStack 값으로 제한
                    for (int stackActionIndex = 0; stackActionIndex < stackActionsProperty.arraySize; stackActionIndex++)
                    {
                        // Element를 가져옴
                        var stackActionProperty = stackActionsProperty.GetArrayElementAtIndex(stackActionIndex);
                        // Element에서 stack Property를 찾아옴
                        var stackProperty = stackActionProperty.FindPropertyRelative("stack");
                        // 1~MaxStack으로 값 제한
                        stackProperty.intValue = Mathf.Clamp(stackProperty.intValue, 1, maxStackProperty.intValue);
                    }

                    EditorGUILayout.PropertyField(property.FindPropertyRelative("action"));

                    EditorGUILayout.PropertyField(property.FindPropertyRelative("runningFinishOption"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("duration"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("applyCount"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("applyCycle"));

                    EditorGUILayout.PropertyField(property.FindPropertyRelative("customActions"));

                    // 들여쓰기 종료
                    EditorGUI.indentLevel -= 1;
                }
            }
            EditorGUILayout.EndVertical();
        }

        // EffectDatas에 새로운 Data를 추가하는 Button
        if (GUILayout.Button("Add New Level"))
        {
            // 배열 길이를 늘려서 새로운 Element를 생성
            var lastArraySize = effectDatasProperty.arraySize++;
            // 이전 Element Property를 가져옴
            var prevElementProperty = effectDatasProperty.GetArrayElementAtIndex(lastArraySize - 1);
            // 새 Element Property를 가져옴
            var newElementProperty = effectDatasProperty.GetArrayElementAtIndex(lastArraySize);
            // 새 Element의 Level은 이전 Element Level + 1
            var newElementLevel = prevElementProperty.FindPropertyRelative("level").intValue + 1;
            newElementProperty.FindPropertyRelative("level").intValue = newElementLevel;

            // 새 Element의 Soft Copy된 StackActions의 Action들을 Deep Copy함
            CustomEditorUtility.DeepCopySerializeReferenceArray(newElementProperty.FindPropertyRelative("stackActions"), "action");

            // 새 Element의 Soft Copy된 Action을 Deep Copy함
            CustomEditorUtility.DeepCopySerializeReference(newElementProperty.FindPropertyRelative("action"));

            // 새 Element의 Soft Copy된 CustomAction을 Deep Copy함
            CustomEditorUtility.DeepCopySerializeReferenceArray(newElementProperty.FindPropertyRelative("customActions"));
        }
    }
    
    
}
