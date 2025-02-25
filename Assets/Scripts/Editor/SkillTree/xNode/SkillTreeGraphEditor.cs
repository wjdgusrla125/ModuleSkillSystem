using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

[CustomNodeGraphEditor(typeof(SkillTreeGraph))]
public class SkillTreeGraphEditor : NodeGraphEditor
{
    // Graph에 존재하는 Node들의 위치를 저장하는 배열.
    // Node들의 저장된 위치와 현재 위치가 다르다면 Node를 Update를 해줄 것임.
    private Vector2[] nodePositions;

    private readonly int spacingBetweenTier = 320;

    // Graph Editor를 열 때 실행되는 함수
    public override void OnOpen()
    {
        target.nodes.Remove(null);
        nodePositions = target.nodes.Select(x => x.position).ToArray();
    }

    // Graph Editor의 GUI를 그려주는 함수
    // 실행해야하는 로직이 있거나 그려주고 싶은 GUI가 있다면 Custom Editor를 작성하던 것과 똑같이 Code를 추가해주면 됨
    public override void OnGUI()
    {
        if (CheckNodePositionUpdate())
            UpdateNodePositionsAndTiers();
    }

    // Graph에 Node를 복사 생성하는 함수 
    public override Node CopyNode(Node original)
    {
        // base.CopyNode 함수를 실행해서 인자로 넘어온 Copy 대상 Node를 복사 생성함
        var newNode = base.CopyNode(original);
        // 새로운 Node가 추가 되었으니 Node들의 Position과 Tier를 Update 함
        UpdateNodePositionsAndTiers();

        return newNode;
    }

    // Graph에 Node를 생성하는 함수
    public override Node CreateNode(Type type, Vector2 position)
    {
        var node = base.CreateNode(type, position);
        UpdateNodePositionsAndTiers();
        return node;
    }

    // Graph에서 Node를 제거하는 함수
    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        if (target.nodes.Count == 0)
            nodePositions = Array.Empty<Vector2>();
        else
            UpdateNodePositionsAndTiers();
    }

    // SkillTreeGraph에서 사용할 Node를 반환하는 함수
    // 인자로는 Project에 정의되어있는 모든 Node Type이 넘어옴
    public override string GetNodeMenuName(Type type)
    {
        if (type.Name == "SkillTreeSlotNode")
            return base.GetNodeMenuName(type);
        else
            return null;
    }

    private bool CheckNodePositionUpdate()
    {
        for (int i = 0; i < nodePositions.Length; i++)
        {
            if (nodePositions[i] != target.nodes[i].position)
                return true;
        }
        return false;
    }
    
    private void UpdateNodePositionsAndTiers()
    {
        if (target.nodes.Count == 0)
            return;

        // x좌표를 기준으로 node들을 오름차순 정렬
        // => Graph에서 왼쪽으로 갈수록 x좌표가 작아지므로 왼쪽에 있는 Node에서 오른쪽에 있는 Node순으로 정렬됨
        target.nodes = target.nodes.OrderBy(x => x.position.x).ToList();
        // 정열된 Node들의 좌표를 저장함
        nodePositions = target.nodes.Select(x => x.position).ToArray();

        int tier = 0;
        var nodes = target.nodes;
        
        var tierField = typeof(SkillTreeSlotNode).GetField("tier", BindingFlags.Instance | BindingFlags.NonPublic);
        tierField.SetValue(nodes[0], tier);

        var firstNodePosition = nodes[0].position;

        for (int i = 1; i < nodes.Count; i++)
        {
            // index에 해당하는 Node와 첫번째 Node와의 거리를 spacingBetweenTier로 나눈 값이 해당 Node의 Tier가 됨 
            tier = (int)(Mathf.Abs(nodes[i].position.x - firstNodePosition.x) / spacingBetweenTier);
            tierField.SetValue(nodes[i], tier);
        }

        var index = 0;
        // y좌표를 기준으로 node들을 내림차순 정렬
        // => Graph에서 위로 갈수록 y좌표가 작아지므로 아래에 있는 Node에서 위에 있는 Node순으로 정렬됨
        var nodesByY = nodes.OrderByDescending(x => x.position.y).ToArray();

        var indexField = typeof(SkillTreeSlotNode).GetField("index", BindingFlags.Instance | BindingFlags.NonPublic);
        indexField.SetValue(nodesByY[0], index);

        for (int i = 1; i < nodes.Count; i++)
        {
            // 위와 수식이 다른 이유는 스킬 트리에서 Tier는 중간이 비어있어도 이상하지 않지만
            // index는 중간이 비어있으면 이상해보이기 때문에 index가 무조건 순서대로 나오게하기 위함
            // 이전 Node와 현재 Node의 거리 차이가 spacingBetweenTier만큼 난다면 index 값을 증가시킴
            if (nodesByY[i - 1].position.y - nodesByY[i].position.y >= spacingBetweenTier)
                index++;

            indexField.SetValue(nodesByY[i], index);
        }
    }
}
