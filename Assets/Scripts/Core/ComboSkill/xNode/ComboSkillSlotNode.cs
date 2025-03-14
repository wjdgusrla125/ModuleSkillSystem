using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using XNode;

[NodeWidth(300), NodeTint(60, 60, 60)]
public class ComboSkillSlotNode : Node
{
    [SerializeField] private int tier;
    
    [SerializeField] private int index;
    
    [SerializeField] private Skill skill;
    
    [SerializeField] private bool isSkillAutoAcquire;
    
    [Input]
    [SerializeField] private int[] precedingAttacks;
    
    [Output(connectionType = ConnectionType.Override), HideInInspector]
    [SerializeField] private ComboSkillSlotNode thisNode;
    
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
        if (port.fieldName != "thisNode") return null;
        
        return thisNode;
    }
    
    public ComboSkillSlotNode[] GetPrecedingSlotNodes()
        => precedingAttacks.Select((value, index) => GetPrecedingSlotNode(index)).ToArray();
    
    private ComboSkillSlotNode GetPrecedingSlotNode(int index)
        => GetInputValue<ComboSkillSlotNode>("precedingAttacks " + index);
    
    public bool IsSkillAcquirable(Entity entity)
    {
        // Skill 자체가 가진 습득 조건을 충족했는지 확인
        if (!skill.IsAcquirable(entity)) return false;

        // Entity가 선행 SKill들을 가지고 있고, 선행 Skill들이 Level을 충족했는지 확인
        for (int i = 0; i < precedingAttacks.Length; i++)
        {
            var inputNode = GetPrecedingSlotNode(i);
            var entitySkill = entity.SkillSystem.Find(inputNode.Skill);

            if (entitySkill == null || entitySkill.Level < precedingAttacks[i])
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