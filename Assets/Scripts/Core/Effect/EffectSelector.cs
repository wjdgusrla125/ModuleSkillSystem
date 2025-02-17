using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EffectSelector
{
    [SerializeField] private int level;
    [SerializeField] private Effect effect;

    public int Level => level;
    public Effect Effect => effect;

    public Effect CreateEffect(Skill owner)
    {
        var clone = effect.Clone() as Effect;
        clone.Setup(owner, owner.Owner, level);
        return clone;
    }
}