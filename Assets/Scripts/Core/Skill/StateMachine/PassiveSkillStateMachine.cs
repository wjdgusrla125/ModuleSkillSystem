using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveSkillStateMachine : StateMachine<Skill>
{
    protected override void AddStates()
    {
        AddState<ReadyState>();
        AddState<SearchingTargetState>();
        AddState<InPrecedingActionState>();
        AddState<InActionState>();
        AddState<CooldownState>();
    }
    protected override void MakeTransitions()
    {
        // Ready State
        MakeTransition<ReadyState, SearchingTargetState>(state => Owner.IsUseable);

        // SearchingTarget State
        MakeTransition<SearchingTargetState, InPrecedingActionState>(state => Owner.IsTargetSelectSuccessful && Owner.HasPrecedingAction);
        MakeTransition<SearchingTargetState, InActionState>(state => Owner.IsTargetSelectSuccessful);

        // InPrecedingAction State
        MakeTransition<InPrecedingActionState, InActionState>(state => ((InPrecedingActionState)state).IsPrecedingActionEnded);

        // InActionState State
        MakeTransition<InActionState, CooldownState>(state => Owner.IsFinished && Owner.HasCooldown);
        MakeTransition<InActionState, ReadyState>(state => Owner.IsFinished);

        // Cooldown State
        MakeTransition<CooldownState, ReadyState>(state => Owner.IsCooldownCompleted);

        MakeAnyTransition<CooldownState>(SkillExecuteCommand.CancelImmediately, state => Owner.IsActivated && Owner.HasCooldown);
        MakeAnyTransition<ReadyState>(SkillExecuteCommand.CancelImmediately);
    }
}