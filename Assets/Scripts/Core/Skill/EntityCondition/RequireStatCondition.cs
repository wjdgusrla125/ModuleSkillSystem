using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RequireStatCondition : EntityCondition
{
    [SerializeField]
    private Stat stat;
    [SerializeField]
    private float needValue;

    public override string Description => $"Lv.{needValue}";

    public override bool IsPass(Entity entity)
        => entity.Stats.GetStat(stat)?.Value >= needValue;

    public override object Clone()
        => new RequireStatCondition() { stat = stat, needValue = needValue };
}