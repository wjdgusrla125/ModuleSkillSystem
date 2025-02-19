using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Entity가 Skill을 사용할 수 있는 상태인지 확인하는 Action
[System.Serializable]
public class IsEntityReadyCondition : SkillCondition
{
    public override bool IsPass(Skill skill)
    {
        var entity = skill.Owner;
        var skillSystem = entity.SkillSystem;
        // 현재 발동 중인 Skill들 중에서 Entity의 Skill 사용을 제한하는 Skill이 존재하는지 확인
        // 발동 중인 Skill이 Toggle Type과 Passive Type이 아니고, 상태가 InAction State인데, Input Type이 아니라면 True
        // => Toggle Type과 Passive Type 그리고 InAction 상태인 Input Type의 Skill은 Entity의 Skill 사용을 제한하는 Skill로 보지 않음
        var isRunningSkillExist = skillSystem.RunningSkills.Any(x => {
            return !x.IsToggleType && !x.IsPassive &&
                   !(x.IsInState<InActionState>() && x.ExecutionType == SkillExecutionType.Input);
        });

        return entity.IsInState<EntityDefaultState>() && !isRunningSkillExist;
    }

    public override object Clone()
        => new IsEntityReadyCondition();
}