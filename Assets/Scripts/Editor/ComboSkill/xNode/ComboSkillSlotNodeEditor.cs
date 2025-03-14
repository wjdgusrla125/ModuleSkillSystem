using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using XNode;
using XNodeEditor;

[CustomNodeEditor(typeof(ComboSkillSlotNode))]
public class ComboSkillSlotNodeEditor : NodeEditor
{
    // Foldout Title을 그리기위한 Dictionary
    private Dictionary<string, bool> isFoldoutExpandedesByName = new Dictionary<string, bool>();

    // Node의 Title을 어떻게 그릴지 정의하는 함수
    public override void OnHeaderGUI()
    {
        var targetAsSlotNode = target as ComboSkillSlotNode;

        // 개발자 한 눈에 정보를 알 수 있도록 Header에 Node의 Tier와 Index, Node가 가진 Skill의 CodeName을 적어줌
        string header = $"Tier {targetAsSlotNode.Tier} - {targetAsSlotNode.Index} / " + (targetAsSlotNode.Skill?.CodeName ?? target.name);
        GUILayout.Label(header, NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
    }

    // Node의 내부를 어떻게 그릴지 정의하는 함수
    public override void OnBodyGUI()
    {
        serializedObject.Update();

        float originLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 120f;

        // Node 자신을 return하는 Outport를 만듬
        NodePort output = target.GetPort("thisNode");
        NodeEditorGUILayout.PortField(GUIContent.none, output);

        DrawDefault();
        DrawSkill();
        DrawAcquisition();
        DrawPrecedingCondition();

        EditorGUIUtility.labelWidth = originLabelWidth;

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDefault()
    {
        GUI.enabled = false;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("index"));
        GUI.enabled = true;
    }

    private void DrawSkill()
    {
        if (!DrawFoldoutTitle("Skill"))
            return;

        var skillProperty = serializedObject.FindProperty("skill");
        var skill = skillProperty.objectReferenceValue as Skill;
        if (skill?.Icon)
        {
            EditorGUILayout.BeginHorizontal();
            {
                // Node의 넓이(NodeWidth Attribute)를 찾아옴
                var widthAttribute = typeof(ComboSkillSlotNode).GetCustomAttribute<Node.NodeWidthAttribute>();
                // 아래 Icon Texture가 가운데에 그려질 수 있도록 Space를 통해 GUI가 그려지는 위치를 가운데로 이동 
                GUILayout.Space((widthAttribute.width * 0.5f) - 50f);

                var preview = AssetPreview.GetAssetPreview(skill.Icon);
                GUILayout.Label(preview, GUILayout.Width(80), GUILayout.Height(80));
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.PropertyField(skillProperty);
    }

    private void DrawAcquisition()
    {
        if (!DrawFoldoutTitle("Acquisition"))
            return;
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isSkillAutoAcquire"));
    }

    private void DrawPrecedingCondition()
    {
        if (!DrawFoldoutTitle("Preceding Condition"))
            return;

        // List의 각 Element에 Port가 달린 형태로 List를 그려줌
        // Port는 precedingLevels의 각 Element마다 할당되어 생성되므로 배열의 길이가 10이라면 10개의 Port가 생성됨.
        // 여기서는 List를 Default 형태로 그리지 않고, 형태를 Customize하기 위해
        // onCreation 변수로 OnCreateReorderableList Callback 함수를 넘겨줌
        NodeEditorGUILayout.DynamicPortList("precedingLevels", typeof(int), serializedObject,
            NodePort.IO.Input, Node.ConnectionType.Override, onCreation: OnCreatePrecedingLevels);
    }

    // precedingLevels 변수를 ReorderableList 형태로 그려주는 함수
    private void OnCreatePrecedingLevels(ReorderableList list)
    {
        list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Preceding Skills");
        };

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            // 현재 index에 해당하는 Element를 가져옴
            var element = serializedObject.FindProperty("precedingLevels").GetArrayElementAtIndex(index); 
            // 가져온 Element를 그려줌, Need Level이라는 Label을 가진 int Field가 그려짐
            EditorGUI.PropertyField(rect, element, new GUIContent("Need Level"));

            // Element에 할당된 Port를 찾아옴, 이 Port에 다른 Node들이 연결됨
            // GetPort 규칙은 (배열 변수 이름 + 찾아올 Port의 index)
            // ex. precedingLevels의 첫번째 Element에 할당된 Port를 찾아오려면 target.GetPort("precedingLevels 0")
            var port = target.GetPort("precedingLevels " + index);
            // Port와 연결된 Output Port가 있을 때,
            // Output Port의 반환 값이 SkillTreeSlotNode Type이 아니라면 연결을 끊음
            // Node.TypeConstraint.Strict를 직접 구현해준 것임
            if (port.Connection != null && port.Connection.GetOutputValue() is not ComboSkillSlotNode)
                port.Disconnect(port.Connection);

            // Port와 연결된 Output Port의 반환값을 SkillTreeSlotNode Type으로 Casting하여 가져옴
            // GetInputValue() 함수로 object type으로 가져올 수도 있음
            // Node의 ConnectionType이 Multiple일 경우,
            // GetInputValues 함수로 연결된 모든 Port의 Value를 가져올 수 있음
            var inputSlot = port.GetInputValue<ComboSkillSlotNode>();
            // 연결된 Port가 있고, 해당 Port에 Skill 할당되어 있다면, 값을 Node가 가진 Skill의 최대 Level로 제한
            if (inputSlot && inputSlot.Skill)
                element.intValue = Mathf.Clamp(element.intValue, 1, inputSlot.Skill.MaxLevel);

            // 위 Code는 아래 Code로 단축시킬 수 있음
            //if (port.TryGetInputValue<SkillTreeSlotNode>(out var inputSlot))
            //    element.intValue = Mathf.Clamp(element.intValue, 1, inputSlot.Skill.MaxLevel);

            // Rect 좌표을 위에 그린 int Field보다 왼쪽으로 옮김
            var position = rect.position;
            position.x -= 37f;
            // Port를 그려줌
            NodeEditorGUILayout.PortField(position, port);
        };
    }

    private bool DrawFoldoutTitle(string title)
        => CustomEditorUtility.DrawFoldoutTitle(isFoldoutExpandedesByName, title);
}
