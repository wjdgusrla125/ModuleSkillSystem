using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SelectPosition : SelectTarget
{
    [Header("Layer")]
    [SerializeField] private LayerMask layerMask;

    public SelectPosition() { }

    public SelectPosition(SelectPosition copy) : base(copy)
    {
        layerMask = copy.layerMask;
    }

    protected override TargetSelectionResult SelectImmediateByPlayer(Vector2 screenPoint, TargetSearcher targetSearcher,
        Entity requesterEntity, GameObject requesterObject)
    {
        var ray = Camera.main.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, layerMask.value))
        {
            if (IsInRange(targetSearcher, requesterEntity, requesterObject, hitInfo.point))
                return new TargetSelectionResult(hitInfo.point, SearchResultMessage.FindPosition);
            else
                return new TargetSelectionResult(hitInfo.point, SearchResultMessage.OutOfRange);
        }
        else
            return new TargetSelectionResult(requesterObject.transform.position, SearchResultMessage.Fail);
    }

    protected override TargetSelectionResult SelectImmediateByAI(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, Vector3 position)
    {
        var target = requesterEntity.Target;

        if (!target)
            return new TargetSelectionResult(position, SearchResultMessage.Fail);
        else if (targetSearcher.IsInRange(requesterEntity, requesterObject, position))
            return new TargetSelectionResult(position, SearchResultMessage.FindPosition);
        else
            return new TargetSelectionResult(position, SearchResultMessage.OutOfRange);
    }

    public override object Clone() => new SelectPosition(this);
}