using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SleepAction : EffectAction
{
    [SerializeField]
    private Category removeTargetCategory;
    [SerializeField]
    private Category dotCategory;

    private Effect effect;

    public override void Start(Effect effect, Entity user, Entity target, int level, float scale)
    {
        this.effect = effect;
        target.onTakeDamage += OnTakeDamage;
    }

    public override bool Apply(Effect effect, Entity user, Entity target, int level, int stack, float scale)
    {
        target.SkillSystem.RemoveEffectAll(x => x != effect && x.HasCategory(removeTargetCategory));
        target.StateMachine.ExecuteCommand(EntityStateCommand.ToSleepingState);
        return true;
    }

    public override void Release(Effect effect, Entity user, Entity target, int level, float scale)
    {
        target.onTakeDamage -= OnTakeDamage;
        target.StateMachine.ExecuteCommand(EntityStateCommand.ToDefaultState);
    }

    public override object Clone()
    {
        return new SleepAction()
        {
            removeTargetCategory = removeTargetCategory,
            dotCategory = dotCategory
        };
    }

    private void OnTakeDamage(Entity entity, Entity instigator, object causer, float damage)
    {
        var causerEffect = causer as Effect;
        if (causerEffect && causerEffect.HasCategory(dotCategory))
            return;

        entity.SkillSystem.RemoveEffect(effect);
    }
}