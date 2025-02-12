using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 검색을 요청한 Object 본인을 선택하는 Module
[Serializable]
public class SelectSelf : TargetSelectionAction
{
    public override object Range => 0f;
    public override object ScaledRange => 0f;
    public override float Angle => 0f;

    public SelectSelf() { }
    public SelectSelf(SelectSelf copy) : base(copy) { }

    protected override TargetSelectionResult SelectImmediateByPlayer(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, Vector3 position)
        => new(requesterObject, SearchResultMessage.FindTarget);

    protected override TargetSelectionResult SelectImmediateByAI(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, Vector3 position)
        => SelectImmediateByPlayer(targetSearcher, requesterEntity, requesterObject, position);

    public override void Select(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, SelectCompletedHandler onSelectCompleted)
        => onSelectCompleted.Invoke(SelectImmediateByPlayer(targetSearcher, requesterEntity,  requesterObject, Vector2.zero));

    public override void CancelSelect(TargetSearcher targetSearcher)
    {
        
    }

    public override bool IsInRange(TargetSearcher targetSearcher, Entity requesterEntity, GameObject requesterObject, Vector3 targetPosition)
        => true;

    public override object Clone()
        => new SelectSelf(this);
}