using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RollingAction : SkillPrecedingAction
{
    [SerializeField]
    private float distance;

    public override void Start(Skill skill) => skill.Owner.Movement.Roll(distance);

    public override bool Run(Skill skill) => !skill.Owner.Movement.IsRolling;

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword()
    {
        var dictionary = new Dictionary<string, string>() { { "distance", distance.ToString("0.##") } };
        return dictionary;
    }

    public override object Clone() => new RollingAction() { distance = distance };
}