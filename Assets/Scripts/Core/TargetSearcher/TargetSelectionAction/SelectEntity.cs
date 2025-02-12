using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

[Serializable]
public class SelectEntity : SelectTarget
{
    // 검색을 요청한 Entity도 검색 대상에 포함할 것인가?
    [SerializeField] private bool isIncludeSelf;
    
    // Target이 검색을 요청한 Entity와 같은 Category를 가지고 있어야하는가?
    [SerializeField] private bool isSelectSameCategory;

    public SelectEntity() { }

    public SelectEntity(SelectEntity copy) : base(copy)
    {
        isIncludeSelf = copy.isIncludeSelf;
        isSelectSameCategory = copy.isSelectSameCategory;
    }

    protected override TargetSelectionResult SelectImmediateByPlayer(Vector2 screenPoint, TargetSearcher targetSearcher,
        Entity requesterEntity, GameObject requesterObject)
    {
        var ray = Camera.main.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity))
        {
            var entity = hitInfo.collider.GetComponent<Entity>();
            // Entity가 null이거나, 이미 죽은 상태거나, 검색을 명령한 Entity인데 isIncludeSelf가 true가 아닐 경우 검색 실패
            if (entity == null || entity.IsDead || (entity == requesterEntity && !isIncludeSelf))
                return new TargetSelectionResult(hitInfo.point, SearchResultMessage.Fail);

            if (entity != requesterEntity)
            {
                // Requester와 Entity가 공유하는 Category가 있는지 확인
                var hasCategory = requesterEntity.Categories.Any(x => entity.HasCategory(x));
                // 공유하는 Category가 있지만 isSelectSameCategory가 false거나,
                // 공유하는 Category가 없지만 isSelectSameCategory가 true라면 검색 실패
                if ((hasCategory && !isSelectSameCategory) || (!hasCategory && isSelectSameCategory))
                    return new TargetSelectionResult(hitInfo.point, SearchResultMessage.Fail);
            }

            if (IsInRange(targetSearcher, requesterEntity, requesterObject, hitInfo.point))
                return new TargetSelectionResult(entity.gameObject, SearchResultMessage.FindTarget);
            else
                return new TargetSelectionResult(entity.gameObject, SearchResultMessage.OutOfRange);
        }
        else
        {
            return new TargetSelectionResult(requesterObject.transform.position, SearchResultMessage.Fail);
        }
    }

    protected override TargetSelectionResult SelectImmediateByAI(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, Vector3 position)
    {
        var target = requesterEntity.Target;

        if (!target)
            return new TargetSelectionResult(position, SearchResultMessage.Fail);
        else if (targetSearcher.IsInRange(requesterEntity, requesterObject, target.transform.position))
            return new TargetSelectionResult(target.gameObject, SearchResultMessage.FindTarget);
        else
            return new TargetSelectionResult(target.gameObject, SearchResultMessage.OutOfRange);
    }

    public override object Clone() => new SelectEntity(this);
}
