using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Skill))]
public class SkillEditor : IdentifiedObjectEditor
{
    private SerializedProperty typeProperty;
    private SerializedProperty useTypeProperty;

    private SerializedProperty executionTypeProperty;
    private SerializedProperty applyTypeProperty;
    private SerializedProperty needSelectionResultTypeProperty;
    private SerializedProperty targetSelectionTimingOptionProperty;
    private SerializedProperty targetSearchTimingOptionProperty;

    private SerializedProperty acquisitionConditionsProperty;
    private SerializedProperty acquisitionCostsProperty;

    private SerializedProperty useConditionsProperty;

    private SerializedProperty isAllowLevelExceedDatasProperty;
    private SerializedProperty maxLevelProperty;
    private SerializedProperty defaultLevelProperty;
    private SerializedProperty skillDatasProperty;

    // Toolbar Button들의 이름
    private readonly string[] customActionsToolbarList = new[] { "Cast", "Charge", "Preceding", "Action" };
    // Skill Data마다 선택한 Toolbar Button의 Index 값
    private Dictionary<int, int> customActionToolbarIndexesByLevel = new();

    private bool IsPassive => typeProperty.enumValueIndex == (int)SkillType.Passive;
    private bool IsToggleType => useTypeProperty.enumValueIndex == (int)SkillUseType.Toggle;
    // Toggle, Passive Type일 때는 사용하지 않는 변수들을 보여주지 않을 것임
    private bool IsDrawPropertyAll => !IsToggleType && !IsPassive;

    protected override void OnEnable()
    {
        base.OnEnable();

        typeProperty = serializedObject.FindProperty("type");
        useTypeProperty = serializedObject.FindProperty("useType");
        
        executionTypeProperty = serializedObject.FindProperty("executionType");
        applyTypeProperty = serializedObject.FindProperty("applyType");
        needSelectionResultTypeProperty = serializedObject.FindProperty("needSelectionResultType");

        targetSelectionTimingOptionProperty = serializedObject.FindProperty("targetSelectionTimingOption");
        targetSearchTimingOptionProperty = serializedObject.FindProperty("targetSearchTimingOption");

        acquisitionConditionsProperty = serializedObject.FindProperty("acquisitionConditions");
        acquisitionCostsProperty = serializedObject.FindProperty("acquisitionCosts");

        useConditionsProperty = serializedObject.FindProperty("useConditions");

        isAllowLevelExceedDatasProperty = serializedObject.FindProperty("isAllowLevelExceedDatas");
        maxLevelProperty = serializedObject.FindProperty("maxLevel");
        defaultLevelProperty = serializedObject.FindProperty("defaultLevel");
        skillDatasProperty = serializedObject.FindProperty("skillDatas");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 220f;

        DrawSettings();
        DrawAcquisition();
        DrawUseConditions();
        DrawSkillDatas();

        EditorGUIUtility.labelWidth = prevLabelWidth;

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSettings()
    {
        if (!DrawFoldoutTitle("Setting"))
            return;

        CustomEditorUtility.DrawEnumToolbar(typeProperty);
        if (!IsPassive)
            CustomEditorUtility.DrawEnumToolbar(useTypeProperty);
        else
            // instant로 고정
            useTypeProperty.enumValueIndex = 0;

        if (IsDrawPropertyAll)
        {
            EditorGUILayout.Space();
            CustomEditorUtility.DrawUnderline();
            EditorGUILayout.Space();

            CustomEditorUtility.DrawEnumToolbar(executionTypeProperty);
            CustomEditorUtility.DrawEnumToolbar(applyTypeProperty);
        }
        else
        {
            // auto로 고정
            executionTypeProperty.enumValueIndex = 0;
            // instant로 고정
            applyTypeProperty.enumValueIndex = 0;
        }

        EditorGUILayout.Space();
        CustomEditorUtility.DrawUnderline();
        EditorGUILayout.Space();

        CustomEditorUtility.DrawEnumToolbar(needSelectionResultTypeProperty);
        CustomEditorUtility.DrawEnumToolbar(targetSelectionTimingOptionProperty);
        CustomEditorUtility.DrawEnumToolbar(targetSearchTimingOptionProperty);
    }

    private void DrawAcquisition()
    {
        if (!DrawFoldoutTitle("Acquisition"))
            return;

        EditorGUILayout.PropertyField(acquisitionConditionsProperty);
        EditorGUILayout.PropertyField(acquisitionCostsProperty);
    }

    private void DrawUseConditions()
    {
        if (!DrawFoldoutTitle("Use Condition"))
            return;

        EditorGUILayout.PropertyField(useConditionsProperty);
    }

    private void DrawSkillDatas()
    {
        // Skill의 Data가 아무것도 존재하지 않으면 1개를 자동적으로 만들어줌
        if (skillDatasProperty.arraySize == 0)
        {
            // 배열 길이를 늘려서 새로운 Element를 생성
            skillDatasProperty.arraySize++;
            // 추가한 Data의 Level을 1로 설정
            skillDatasProperty.GetArrayElementAtIndex(0).FindPropertyRelative("level").intValue = 1;
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
            var lastIndex = skillDatasProperty.arraySize - 1;
            // 마지막 SkillData(= 가장 높은 Level의 Data)를 가져옴
            var lastSkillData = skillDatasProperty.GetArrayElementAtIndex(lastIndex);
            // maxLevel을 마지막 Data의 Level로 고정
            maxLevelProperty.intValue = lastSkillData.FindPropertyRelative("level").intValue;
            // maxLevel Property를 그려줌
            EditorGUILayout.PropertyField(maxLevelProperty);
            GUI.enabled = true;
        }

        EditorGUILayout.PropertyField(defaultLevelProperty);

        for (int i = 0; i < skillDatasProperty.arraySize; i++)
        {
            var property = skillDatasProperty.GetArrayElementAtIndex(i);

            var isUseCastProperty = property.FindPropertyRelative("isUseCast");
            var isUseChargeProperty = property.FindPropertyRelative("isUseCharge");
            var chargeDurationProperty = property.FindPropertyRelative("chargeDuration");
            var chargeTimeProperty = property.FindPropertyRelative("chargeTime");
            var needChargeTimeToUseProperty = property.FindPropertyRelative("needChargeTimeToUse");

            EditorGUILayout.BeginVertical("HelpBox");
            {
                // Data의 Level과 Data 삭제를 위한 X Button을 그려주는 Foldout Title을 그려줌
                // 단, 첫번째 Data(= index 0) 지우면 안되기 때문에 X Button을 그려주지 않음
                // X Button을 눌러서 Data가 지워지면 true를 return함
                if (DrawRemovableLevelFoldout(skillDatasProperty, property, i, i != 0))
                {
                    // Data가 삭제되었으며 더 이상 GUI를 그리지 않고 바로 빠져나감
                    // 다음 Frame에 처음부터 다시 그리기 위함
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUI.indentLevel += 1;

                if (property.isExpanded)
                {
                    // SkillData Property 내부로 들어감 -> Property == level field;
                    property.NextVisible(true);

                    DrawAutoSortLevelProperty(skillDatasProperty, property, i, i != 0);

                    // Level Up
                    for (int j = 0; j < 2; j++)
                    {
                        property.NextVisible(false);
                        EditorGUILayout.PropertyField(property);
                    }

                    // PrecedingAction
                    // Toggle Type일 때는 PrecedingAction을 사용하지 않을 것이므로,
                    // Instant Type일 때만 PrecedingAction 변수를 보여줌
                    property.NextVisible(false);
                    if (useTypeProperty.enumValueIndex == (int)SkillUseType.Instant)
                        EditorGUILayout.PropertyField(property);

                    // Action And Setting
                    for (int j = 0; j < 8; j++)
                    {
                        // 다음 변수의 Property로 이동하면서 그려줌
                        property.NextVisible(false);
                        EditorGUILayout.PropertyField(property);
                    }

                    // Cast
                    property.NextVisible(false);
                    if (IsDrawPropertyAll && !isUseChargeProperty.boolValue)
                        EditorGUILayout.PropertyField(property);
                    else
                        property.boolValue = false;

                    property.NextVisible(false);
                    if (isUseCastProperty.boolValue)
                        EditorGUILayout.PropertyField(property);

                    // Charge
                    property.NextVisible(false);
                    if (IsDrawPropertyAll && !isUseCastProperty.boolValue)
                        EditorGUILayout.PropertyField(property);

                    for (int j = 0; j < 5; j++)
                    {
                        property.NextVisible(false);
                        if (isUseChargeProperty.boolValue)
                            EditorGUILayout.PropertyField(property);
                    }

                    // 최대 chargeTime 값을 chargeDuration 값으로 제한
                    chargeTimeProperty.floatValue = Mathf.Min(chargeTimeProperty.floatValue, chargeDurationProperty.floatValue);

                    // 최대 needChargeTime 값을 chargeTime 값으로 제한
                    needChargeTimeToUseProperty.floatValue = Mathf.Min(chargeTimeProperty.floatValue, needChargeTimeToUseProperty.floatValue);

                    // Effect
                    property.NextVisible(false);
                    EditorGUILayout.PropertyField(property);

                    // EffectSelector의 level 변수를 effect의 최대 level 제한함
                    for (int j = 0; j < property.arraySize; j++)
                    {
                        var effectSelectorProperty = property.GetArrayElementAtIndex(j);
                        // Selector의 level Property를 가져옴
                        var levelProperty = effectSelectorProperty.FindPropertyRelative("level");
                        // Selector가 가진 effect를 가져옴
                        var effect = effectSelectorProperty.FindPropertyRelative("effect").objectReferenceValue as Effect;
                        var maxLevel = effect != null ? effect.MaxLevel : 0;
                        var minLevel = maxLevel == 0 ? 0 : 1;
                        levelProperty.intValue = Mathf.Clamp(levelProperty.intValue, minLevel, maxLevel);
                    }

                    // Animation
                    for (int j = 0; j < 5; j++)
                    {
                        property.NextVisible(false);
                        EditorGUILayout.PropertyField(property);
                    }

                    // Custom Action - UnderlineTitle
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Custom Action", EditorStyles.boldLabel);
                    CustomEditorUtility.DrawUnderline();

                    // Custom Action - Toolbar
                    // 한번에 모든 Array 변수를 다 그리면 보기 번잡하니 Toolbar를 통해 보여줄 Array를 선택할 수 있게함.
                    var customActionToolbarIndex = customActionToolbarIndexesByLevel.ContainsKey(i) ? customActionToolbarIndexesByLevel[i] : 0;
                    // Toolbar는 자동 들여쓰기(EditorGUI.indentLevel)이 먹히지 않아서 직접 들여쓰기를 해줌
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(12);
                        customActionToolbarIndex = GUILayout.Toolbar(customActionToolbarIndex, customActionsToolbarList);
                        customActionToolbarIndexesByLevel[i] = customActionToolbarIndex;
                    }
                    GUILayout.EndHorizontal();

                    // Custom Action
                    for (int j = 0; j < 4; j++)
                    {
                        property.NextVisible(false);
                        if (j == customActionToolbarIndex)
                            EditorGUILayout.PropertyField(property);
                    }
                }
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add New Level"))
        {
            // Level Change
            var lastArraySize = skillDatasProperty.arraySize++;
            var prevElementalProperty = skillDatasProperty.GetArrayElementAtIndex(lastArraySize - 1);
            var newElementProperty = skillDatasProperty.GetArrayElementAtIndex(lastArraySize);
            var newElementLevel = prevElementalProperty.FindPropertyRelative("level").intValue + 1;
            newElementProperty.FindPropertyRelative("level").intValue = newElementLevel;
            newElementProperty.isExpanded = true;

            CustomEditorUtility.DeepCopySerializeReferenceArray(newElementProperty.FindPropertyRelative("levelUpConditions"));
            CustomEditorUtility.DeepCopySerializeReferenceArray(newElementProperty.FindPropertyRelative("levelUpCosts"));

            CustomEditorUtility.DeepCopySerializeReference(newElementProperty.FindPropertyRelative("precedingAction"));

            CustomEditorUtility.DeepCopySerializeReference(newElementProperty.FindPropertyRelative("action"));

            // Costs Deep Copy
            CustomEditorUtility.DeepCopySerializeReferenceArray(newElementProperty.FindPropertyRelative("costs"));

            // TargetSearcher SelectionAction Deep Copy
            CustomEditorUtility.DeepCopySerializeReference(newElementProperty
                .FindPropertyRelative("targetSearcher")
                .FindPropertyRelative("selectionAction"));

            // TargetSearcher SearchAction Deep Copy
            CustomEditorUtility.DeepCopySerializeReference(newElementProperty
               .FindPropertyRelative("targetSearcher")
               .FindPropertyRelative("searchAction"));

            //customActionsOnCast; customActionsOnCharge;
            //customActionsOnPrecedingAction; customActionsOnAction;
            CustomEditorUtility.DeepCopySerializeReferenceArray(newElementProperty.FindPropertyRelative("customActionsOnCast"));
            CustomEditorUtility.DeepCopySerializeReferenceArray(newElementProperty.FindPropertyRelative("customActionsOnCharge"));
            CustomEditorUtility.DeepCopySerializeReferenceArray(newElementProperty.FindPropertyRelative("customActionsOnPrecedingAction"));
            CustomEditorUtility.DeepCopySerializeReferenceArray(newElementProperty.FindPropertyRelative("customActionsOnAction"));
        }
    }
}