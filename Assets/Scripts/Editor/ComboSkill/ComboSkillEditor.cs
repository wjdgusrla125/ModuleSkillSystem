using UnityEngine;
using UnityEditor;
using XNodeEditor;
using XNode;

[CustomEditor(typeof(ComboSkill))]
public class ComboSkillEditor : IdentifiedObjectEditor
{
    private SerializedProperty graphProperty;

    protected override void OnEnable()
    {
        base.OnEnable();

        graphProperty = serializedObject.FindProperty("graph");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        // graph가 없으면 자동으로 만들어줌
        if (graphProperty.objectReferenceValue == null)
        {
            var targetObject = serializedObject.targetObject;
            var newGraph = CreateInstance<ComboSkillGraph>();
            newGraph.name = "Combo Skill Graph";

            // NodeGraph도 ScriptableObject Type이므로 일반적인 자료형처럼 Serialize를 할 수 없기에
            // IdendifiedObject의 하위 Asset으로 만들어서 불러오는 방식으로 Serialize함
            AssetDatabase.AddObjectToAsset(newGraph, targetObject);
            AssetDatabase.SaveAssets();

            graphProperty.objectReferenceValue = newGraph;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Open Window", GUILayout.Height(50f)))
            NodeEditorWindow.Open(graphProperty.objectReferenceValue as NodeGraph);

        serializedObject.ApplyModifiedProperties();
    }
}
