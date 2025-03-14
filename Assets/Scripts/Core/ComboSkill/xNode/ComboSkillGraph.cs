using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

[CreateAssetMenu(fileName = "ComboSkill", menuName = "Skill/ComboSkill")]
public class ComboSkillGraph : NodeGraph
{
    public ComboSkillSlotNode[] GetSlotNodes()
        => nodes.Where(x => x != null).Cast<ComboSkillSlotNode>().ToArray();
}
