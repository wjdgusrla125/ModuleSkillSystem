using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InActionState : SkillState
{
    private bool isAutoExecuteType;
    private bool isInstantApplyType;

    protected override void Setup()
    {
        isAutoExecuteType = Entity.ExecutionType == SkillExecutionType.Auto;
        isInstantApplyType = Entity.ApplyType == SkillApplyType.Instant;
    }

    public override void Enter()
    {
        if (!Entity.IsActivated)
            Entity.Activate();

        Entity.StartAction();

        Apply();
    }

    public override void Update()
    {
        Entity.CurrentDuration += Time.deltaTime;
        Entity.CurrentApplyCycle += Time.deltaTime;

        if (Entity.IsToggleType)
            Entity.UseDeltaCost();

        if (isAutoExecuteType && Entity.IsApplicable)
            Apply();
    }

    public override void Exit()
    {
        Entity.CancelSelectTarget();
        Entity.ReleaseAction();
    }

    // Execute Type이 Input일 경우, Skill의 Use 함수를 통해 Use Message가 넘어오면 Apply 함수를 호출함
    // 즉, Skill의 발동이 Update 함수에서 자동(Auto)으로 되는 것이 아니라, 사용자의 입력(Input)을 통해 이 함수에서 됨
    public override bool OnReceiveMessage(int message, object data)
    {
        var stateMessage = (SkillStateMessage)message;
        if (stateMessage != SkillStateMessage.Use || isAutoExecuteType)
            return false;

        if (Entity.IsApplicable)
        {
            if (Entity.IsTargetSelectionTiming(TargetSelectionTimingOption.UseInAction))
            {
                // Skill이 Searching중이 아니라면 SelectTarget 함수로 기준점 검색을 실행,
                // 기준점 검색이 성공하면 OnTargetSelectionCompleted Callback 함수가 호출되어 Apply 함수를 호출함
                if (!Entity.IsSearchingTarget)
                    Entity.SelectTarget(OnTargetSelectionCompleted);
            }
            else
                Apply();

            return true;
        }
        else
            return false;
    }

    private void Apply()
    {
        TrySendCommandToOwner(Entity, EntityStateCommand.ToInSkillActionState, Entity.ActionAnimationParameter);

        if (isInstantApplyType)
            Entity.Apply();
        else if (!isAutoExecuteType)
            Entity.CurrentApplyCount++;
    }

    private void OnTargetSelectionCompleted(Skill skill, TargetSearcher targetSearcher, TargetSelectionResult result)
    {
        if (skill.HasValidTargetSelectionResult)
            Apply();
    }
}