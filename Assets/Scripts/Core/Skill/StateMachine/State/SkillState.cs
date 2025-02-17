using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillState : State<Skill>
{
    // Skill을 소유한 Owner의 StateMachine에게 상태 전환 Command와 SKill의 정보를 보내는 함수
    protected void TrySendCommandToOwner(Skill skill, EntityStateCommand command, AnimatorParameter animatorParameter)
    {
        var ownerStateMachine = Entity.Owner.StateMachine;
        if (ownerStateMachine != null && animatorParameter.IsValid)
        {
            // 인자로 받은 animatorParameter가 bool Type이면 owner의 StateMachine으로 인자로 받은 command를 보냄
            // Transition이 Command를 받아들였으면, State로 UsingSKill Message와 Skill 정보를 보냄
            if (animatorParameter.type == AnimatorParameterType.Bool && ownerStateMachine.ExecuteCommand(command))
                ownerStateMachine.SendMessage(EntityStateMessage.UsingSkill, (skill, animatorParameter));
            // 인자로 받은 animatorParameter가 trigger Type이면 행동에 제약을 주지 않을 것이므로 ToDefaultState Command를 보내고
            // Transition이 받아들였는지와 상관없이, State로 UsingSkill Message와 skill 정보를 보냄
            else if (animatorParameter.type == AnimatorParameterType.Trigger)
            {
                ownerStateMachine.ExecuteCommand(EntityStateCommand.ToDefaultState);
                ownerStateMachine.SendMessage(EntityStateMessage.UsingSkill, (skill, animatorParameter));
            }
        }
    }
}