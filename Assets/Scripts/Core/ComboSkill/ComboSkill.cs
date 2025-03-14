using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class ComboSkill : IdentifiedObject
{
    [SerializeField, HideInInspector]
    private ComboSkillGraph graph;
    
    public ComboSkillSlotNode[] GetSlotNodes() => graph.GetSlotNodes();
}
