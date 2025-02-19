using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

[System.Serializable]
public class RemoveEffectByCategory : EffectAction
{
    // Target Effect가 가지고 있는 category
    [SerializeField]
    private Category category;
    // Category를 가진 Effect들 중 가장 먼저 찾은 Effect만 지울 것인가? 모든 Effect를 지울 것인가?
    [SerializeField]
    private bool isRemoveAll;

    public override bool Apply(Effect effect, Entity user, Entity target, int level, int stack, float scale)
    {
        if (isRemoveAll)
            target.SkillSystem.RemoveEffectAll(category);
        else
            target.SkillSystem.RemoveEffect(category);

        return true;
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
        => new Dictionary<string, string>() { { "category", category.DisplayName } };

    public override object Clone() => new RemoveEffectByCategory() { category = category, isRemoveAll = isRemoveAll };
}