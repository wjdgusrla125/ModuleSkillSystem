using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

[CreateAssetMenu(fileName = "Skill Tree", menuName = "Skill/Skill Tree")]
public class SkillTreeGraph : NodeGraph
{
    public SkillTreeSlotNode[] GetSlotNodes()
        => nodes.Where(x => x != null).Cast<SkillTreeSlotNode>().ToArray();
}