using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatCost : Cost
{
    [SerializeField] private Stat stat;
    [SerializeField] private StatScaleFloat value;

    public override string Description => stat.DisplayName;

    public override bool HasEnoughCost(Entity entity)
        => entity.Stats.GetValue(stat) >= value.GetValue(entity.Stats);

    public override void UseCost(Entity entity)
        => entity.Stats.IncreaseDefaultValue(stat, -value.GetValue(entity.Stats));

    public override void UseDeltaCost(Entity entity)
        => entity.Stats.IncreaseDefaultValue(stat, -value.GetValue(entity.Stats) * Time.deltaTime);

    public override float GetValue(Entity entity) => value.GetValue(entity.Stats);

    public override object Clone()
        => new StatCost() { stat = stat, value = value };
}