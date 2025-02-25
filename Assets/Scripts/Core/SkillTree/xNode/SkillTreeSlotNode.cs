using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

// NodeWidth: Graph에서 보여지지는 Node의 넓이
// NodeTint: Node의 RGB(255, 255, 255) Color
[NodeWidth(300), NodeTint(60, 60, 60)]
public class SkillTreeSlotNode : Node
{
    // graph에서 몇번째 열 or 행인지 여부
    [SerializeField] private int tier;
    // tier에서 몇번째 Slot인지 여부
    // ex. (tier=1, index=0), (tier=1, index=1), (tier=1, index=2) ...
    // tier와 index를 합쳐서 2차원 배열 형태임
    [SerializeField] private int index;

    // 이 Node가 가지고 있는 Skill
    [SerializeField] private Skill skill;

    // Skill이 조건을 만족했을 때 자동으로 습득할 것인지 여부
    [SerializeField] private bool isSkillAutoAcquire;

    // Skill의 습득하기 위해 필요한 선행 Skill들과 Skill들의 Level을 받는 변수.
    // precedingLevels 자체는 int형 배열이라 필요한 Level 값만 받을 수 있지만,
    // CustomEditor를 통해서 추가되는 Element마다 Input Port를 할당해서 다른 선행 Skill Node가 연결될 수 있도록 할 것임.
    // 즉, Element를 추가하면 필요한 Level을 입력하고 Element에 할당된 Port에 선행 Skill Node를 연결해야 온전히 조건 입력이 완료된 것임.
    [Input]
    [SerializeField] private int[] precedingLevels;

    // 다른 Node의 precdingLevels 변수에 연결할 현재 Node(this)
    [Output(connectionType = ConnectionType.Override), HideInInspector]
    [SerializeField] private SkillTreeSlotNode thisNode;

    public int Tier => tier;
    public int Index => index;
    public Skill Skill => skill;
    public bool IsSkillAutoAcquire => isSkillAutoAcquire;

    protected override void Init()
    {
        thisNode = this;
    }

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != "thisNode")
            return null;
        return thisNode;
    }

    // 필요한 선행 Skill들
    // 위 precdingLevels 변수를 통해 찾아옴
    public SkillTreeSlotNode[] GetPrecedingSlotNodes()
        => precedingLevels.Select((value, index) => GetPrecedingSlotNode(index)).ToArray();

    // precedingLevels의 Element에 할당된 Port에서 Port와 연결된 다른 Node를 찾아옴
    // Element에 할당된 Port를 찾아오는 Naming 규칙은 (변수 이름 + Element의 index)
    // ex. precedingLevels의 첫번째 Element에 할당된 Port를 찾아오고 싶다면 precedingLevels0
    // Port에 연결된 Node가 없다면 null이 반환됨
    private SkillTreeSlotNode GetPrecedingSlotNode(int index)
        => GetInputValue<SkillTreeSlotNode>("precedingLevels " + index);

    public bool IsSkillAcquirable(Entity entity)
    {
        // Skill 자체가 가진 습득 조건을 충족했는지 확인
        if (!skill.IsAcquirable(entity))
            return false;

        // Entity가 선행 SKill들을 가지고 있고, 선행 Skill들이 Level을 충족했는지 확인
        for (int i = 0; i < precedingLevels.Length; i++)
        {
            var inputNode = GetPrecedingSlotNode(i);
            var entitySkill = entity.SkillSystem.Find(inputNode.Skill);

            if (entitySkill == null || entitySkill.Level < precedingLevels[i])
                return false;
        }

        return true;
    }

    public Skill AcquireSkill(Entity entity)
    {
        Debug.Assert(IsSkillAcquirable(entity), "SkillTreeNode::AcquireSkill - Skill 습득 조건을 충족하지 못했습니다.");
        return entity.SkillSystem.Register(skill);
    }
}