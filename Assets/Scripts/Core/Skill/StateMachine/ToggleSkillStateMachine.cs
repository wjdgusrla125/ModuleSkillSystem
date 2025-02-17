using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleSkillStateMachine : StateMachine<Skill>
{
    protected override void AddStates()
    {
        AddState<ReadyState>();
        AddState<SearchingTargetState>();
        AddState<InActionState>();
        AddState<CooldownState>();

        // Toggle형 Skill의 경우 Skill을 켰을 때도 Cooldown을 적용하기 위함
        // 켠 Skill을 끌려면 Cooldown을 기다려함
        AddState<ReadyState>(1);
        AddState<CooldownState>(1);
    }
    protected override void MakeTransitions()
    {
        // Ready State
        MakeTransition<ReadyState, SearchingTargetState>(SkillExecuteCommand.Use);
        MakeTransition<ReadyState, CooldownState>(state => !Owner.IsCooldownCompleted);

        // SearchingTarget State
        MakeTransition<SearchingTargetState, InActionState>(state => Owner.IsTargetSelectSuccessful);

        // InAction State
        MakeTransition<InActionState, CooldownState>(state => (Owner.IsFinished || !Owner.HasEnoughCost) && Owner.HasCooldown);
        MakeTransition<InActionState, CooldownState>(SkillExecuteCommand.Use, state => Owner.HasCooldown);

        MakeTransition<InActionState, ReadyState>(state => Owner.IsFinished || !Owner.HasEnoughCost);
        MakeTransition<InActionState, ReadyState>(SkillExecuteCommand.Use);

        // Cooldown State
        MakeTransition<CooldownState, ReadyState>(state => Owner.IsCooldownCompleted);

        // Layer 1
        MakeTransition<ReadyState, CooldownState>(SkillExecuteCommand.Use, state => Owner.HasCooldown, 1);

        MakeTransition<CooldownState, ReadyState>(state => Owner.IsCooldownCompleted, 1);

        MakeAnyTransition<CooldownState>(SkillExecuteCommand.CancelImmediately, state => Owner.IsActivated && Owner.HasCooldown);
        MakeAnyTransition<ReadyState>(SkillExecuteCommand.CancelImmediately);
    }
}