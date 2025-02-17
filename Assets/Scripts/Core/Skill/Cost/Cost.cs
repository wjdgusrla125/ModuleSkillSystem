using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Cost : ICloneable
{
    public abstract string Description { get; }

    public abstract bool HasEnoughCost(Entity entity);
    public abstract void UseCost(Entity entity);
    public abstract void UseDeltaCost(Entity entity);
    public abstract float GetValue(Entity entity);
    public abstract object Clone();
}