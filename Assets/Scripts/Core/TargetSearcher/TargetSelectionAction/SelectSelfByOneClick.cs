using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// SelectSelf Module과 똑같지만, Mouse 좌 Click을 해줘야함.
[Serializable]
public class SelectSelfByOneClick : SelectTarget
{
    public SelectSelfByOneClick() { }
    public SelectSelfByOneClick(SelectSelfByOneClick copy) : base(copy) { }

    public override object Clone()
        => new SelectSelfByOneClick(this);

    protected override TargetSelectionResult SelectImmediateByPlayer(Vector2 screenPoint, TargetSearcher targetSearcher,
        Entity requesterEntity, GameObject requesterObject)
        => new(requesterObject, SearchResultMessage.FindTarget);

    protected override TargetSelectionResult SelectImmediateByAI(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, Vector3 position)
        => SelectImmediateByPlayer(Vector2.zero, targetSearcher, requesterEntity, requesterObject);
}