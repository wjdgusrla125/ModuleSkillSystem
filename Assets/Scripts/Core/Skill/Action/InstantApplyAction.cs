using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InstantApplyAction : SkillAction
{
    public override void Apply(Skill skill)
    {
        foreach (var target in skill.Targets)
            target.SkillSystem.Apply(skill);
    }

    public override object Clone() => new InstantApplyAction();
}