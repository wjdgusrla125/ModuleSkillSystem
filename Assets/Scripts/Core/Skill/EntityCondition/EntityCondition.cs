using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class EntityCondition : Condition<Entity>
{
    public abstract string Description { get; }
}