using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class SkillAction : ICloneable
{
    public virtual void Start(Skill skill) { }
    public abstract void Apply(Skill skill);
    public virtual void Release(Skill skill) { }

    protected virtual IReadOnlyDictionary<string, string> GetStringsByKeyword() => null;

    public virtual string BuildDescription(string description)
        => TextReplacer.Replace(description, "skillAction", GetStringsByKeyword());

    public abstract object Clone();
}