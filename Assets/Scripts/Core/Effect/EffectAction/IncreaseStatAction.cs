using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class IncreaseStatAction : EffectAction
{
    [SerializeField]
    private Stat stat;
    [SerializeField]
    private float defaultValue;
    [SerializeField]
    private Stat bonusValueStat;
    [SerializeField]
    private float bonusValueStatFactor;
    [SerializeField]
    private float bonusValuePerLevel;
    [SerializeField]
    private float bonusValuePerStack;
    // 적용할 값을 Stat의 DefaultValue에 더할 것인가? Bonus Value로 추가할 것인가?
    [SerializeField]
    private bool isBonusType = true;
    // 적용한 값을 Release할 때 되돌릴 것인가?
    [SerializeField]
    private bool isUndoOnRelease = true;

    private float totalValue;

    private float GetDefaultValue(Effect effect)
        => defaultValue + (effect.DataBonusLevel * bonusValuePerLevel);

    private float GetStackValue(int stack)
        => (stack - 1) * bonusValuePerStack;

    private float GetBonusStatValue(Entity user)
        => user.Stats.GetValue(bonusValueStat) * bonusValueStatFactor;

    private float GetTotalValue(Effect effect, Entity user, int stack, float scale)
    {
        totalValue = GetDefaultValue(effect) + GetStackValue(stack);
        if (bonusValueStat)
            totalValue += GetBonusStatValue(user);

        totalValue *= scale;

        return totalValue;
    }

    public override bool Apply(Effect effect, Entity user, Entity target, int level, int stack, float scale)
    {
        totalValue = GetTotalValue(effect, user, stack, scale);

        if (isBonusType)
            target.Stats.SetBonusValue(stat, this, totalValue);
        else
            target.Stats.IncreaseDefaultValue(stat, totalValue);

        return true;
    }

    public override void Release(Effect effect, Entity user, Entity target, int level, float scale)
    {
        if (!isUndoOnRelease)
            return;

        if (isBonusType)
            target.Stats.RemoveBonusValue(stat, this);
        else
            target.Stats.IncreaseDefaultValue(stat, -totalValue);
    }

    public override void OnEffectStackChanged(Effect effect, Entity user, Entity target, int level, int stack, float scale)
    {
        if (!isBonusType)
            Release(effect, user, target, level, scale);

        Apply(effect, user, target, level, stack, scale);
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            { "stat", stat.DisplayName },
            { "defaultValue", GetDefaultValue(effect).ToString("0.##") },
            { "bonusDamageStat", bonusValueStat?.DisplayName ?? string.Empty },
            { "bonusDamageStatFactor", (bonusValueStatFactor * 100f).ToString() + "%" },
            { "bonusDamageByLevel", bonusValuePerLevel.ToString() },
            { "bonusDamageByStack", bonusValuePerStack.ToString() },
        };

        if (effect.Owner != null)
        {
            descriptionValuesByKeyword.Add("totalValue",
                GetTotalValue(effect, effect.User, effect.CurrentStack, effect.Scale).ToString("0.##"));
        }

        return descriptionValuesByKeyword;

    }

    public override object Clone()
    {
        return new IncreaseStatAction()
        {
            stat = stat,
            defaultValue = defaultValue,
            bonusValueStat = bonusValueStat,
            bonusValueStatFactor = bonusValueStatFactor,
            bonusValuePerLevel = bonusValuePerLevel,
            bonusValuePerStack = bonusValuePerStack,
            isBonusType = isBonusType,
            isUndoOnRelease = isUndoOnRelease
        };
    }
}
