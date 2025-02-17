using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargingState : SkillState
{
    // Charge 상태가 종료되었는가? true라면 다른 State로 전이됨
    public bool IsChargeEnded { get; private set; }
    // Charge가 최소 충전량을 채웠고, Skill이 기준점 검색에 성공했는가?(=Charge를 마치고 Skill이 사용되었는가?)
    // 위와 마찬가지로 true라면 다른 State로 전이됨
    public bool IsChargeSuccessed { get; private set; }

    public override void Enter()
    {
        Entity.Activate();

        if (Entity.Owner.IsPlayer)
        {
            Entity.SelectTarget(OnTargetSearchCompleted, false);
        }
        Entity.ShowIndicator();
        Entity.StartCustomActions(SkillCustomActionType.Charge);

        TrySendCommandToOwner(Entity, EntityStateCommand.ToChargingSkillState, Entity.ChargeAnimationParameter);
    }

    public override void Update()
    {
        Entity.CurrentChargeDuration += Time.deltaTime;

        if (!Entity.Owner.IsPlayer && Entity.IsMaxChargeCompleted)
        {
            IsChargeEnded = true;
            Entity.SelectTarget(false);
            TryUse();
        }
        else if (Entity.IsChargeDurationEnded)
        {
            IsChargeEnded = true;
            if (Entity.ChargeFinishActionOption == SkillChargeFinishActionOption.Use)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity))
                    Entity.SelectTargetImmediate(hitInfo.point);

                TryUse();
            }
        }

        Entity.RunCustomActions(SkillCustomActionType.Charge);
    }

    public override void Exit()
    {
        IsChargeEnded = false;
        IsChargeSuccessed = false;

        if (Entity.IsSearchingTarget)
            Entity.CancelSelectTarget();
        else
            Entity.HideIndicator();

        Entity.ReleaseCustomActions(SkillCustomActionType.Charge);
    }

    private bool TryUse()
    {
        if (Entity.IsMinChargeCompleted && Entity.IsTargetSelectSuccessful)
            IsChargeSuccessed = true;

        return IsChargeSuccessed;
    }

    private void OnTargetSearchCompleted(Skill skill, TargetSearcher searcher, TargetSelectionResult result)
    {
        if (!TryUse())
            Entity.SelectTarget(OnTargetSearchCompleted, false);
    }
}
