using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EntitySkillState : State<Entity>
{
    // 현재 Entity가 실행중인 Skill
    public Skill RunningSkill { get; private set; }
    // Entity가 실행해야할 Animation의 Hash
    protected int AnimatorParameterHash { get; private set; }

    public override void Enter()
    {
        Entity.Movement?.Stop();

        var playerController = Entity.GetComponent<PlayerController>();
        if (playerController)
            playerController.enabled = false;
    }

    public override void Exit()
    {
        Entity.Animator?.SetBool(AnimatorParameterHash, false);

        RunningSkill = null;

        var playerController = Entity.GetComponent<PlayerController>();
        if (playerController)
            playerController.enabled = true;
    }
    
    public override bool OnReceiveMessage(int message, object data)
    {
        if ((EntityStateMessage)message != EntityStateMessage.UsingSkill)
            return false;

        var tupleData = ((Skill, AnimatorParameter))data;
        
        RunningSkill = tupleData.Item1;
        AnimatorParameterHash = tupleData.Item2.Hash;
    
        Debug.Assert(RunningSkill != null,
            $"CastingSkillState({message})::OnReceiveMessage - 잘못된 data가 전달되었습니다.");

        // Skill이 자신이 적용될 기준점을 찾은 상태라면(=TargetSearcher.SelectTarget), 그 방향을 바라봄
        if (RunningSkill.IsTargetSelectSuccessful)
        {
            var selectionResult = RunningSkill.TargetSelectionResult;
            if (selectionResult.selectedTarget != Entity.gameObject)
                Entity.Movement.LookAtImmediate(selectionResult.selectedPosition);
        }

        Entity.Animator?.SetBool(AnimatorParameterHash, true);

        return true;
    }
}