using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StunAction : EffectAction
{
    [SerializeField]
    private Category removeTargetCategory;

    public override bool Apply(Effect effect, Entity user, Entity target, int level, int stack, float scale)
    {
        target.SkillSystem.RemoveEffectAll(removeTargetCategory);
        target.StateMachine.ExecuteCommand(EntityStateCommand.ToStunningState);
        return true;
    }

    public override void Release(Effect effect, Entity user, Entity target, int level, float scale)
        => target.StateMachine.ExecuteCommand(EntityStateCommand.ToDefaultState);

    public override object Clone() => new StunAction() { removeTargetCategory = removeTargetCategory };
}