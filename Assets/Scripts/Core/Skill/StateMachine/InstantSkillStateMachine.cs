using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantSkillStateMachine : StateMachine<Skill>
{
    protected override void AddStates()
    {
        AddState<ReadyState>();
        AddState<SearchingTargetState>();
        AddState<CastingState>();
        AddState<ChargingState>();
        AddState<InPrecedingActionState>();
        AddState<InActionState>();
        AddState<CooldownState>();
    }
    protected override void MakeTransitions()
    {
        // Ready State
        MakeTransition<ReadyState, ChargingState>(SkillExecuteCommand.Use, state =>  Owner.IsUseCharge);
        MakeTransition<ReadyState, ChargingState>(SkillExecuteCommand.UseImmediately, state => Owner.IsUseCharge);
        MakeTransition<ReadyState, SearchingTargetState>(SkillExecuteCommand.Use, state =>
        Owner.IsTargetSelectionTiming(TargetSelectionTimingOption.Use));
        MakeTransition<ReadyState, CastingState>(SkillExecuteCommand.Use, state => Owner.IsUseCast);
        MakeTransition<ReadyState, CastingState>(SkillExecuteCommand.UseImmediately, state => Owner.IsUseCast);
        MakeTransition<ReadyState, InPrecedingActionState>(SkillExecuteCommand.Use, state => Owner.HasPrecedingAction);
        MakeTransition<ReadyState, InPrecedingActionState>(SkillExecuteCommand.UseImmediately, state => Owner.HasPrecedingAction);
        MakeTransition<ReadyState, InActionState>(SkillExecuteCommand.Use);
        MakeTransition<ReadyState, InActionState>(SkillExecuteCommand.UseImmediately);
        MakeTransition<ReadyState, CooldownState>(state => !Owner.IsCooldownCompleted);

        // Charging State
        MakeTransition<ChargingState, InPrecedingActionState>(state =>
        (state as ChargingState).IsChargeSuccessed && Owner.HasPrecedingAction);
        MakeTransition<ChargingState, InActionState>(state => (state as ChargingState).IsChargeSuccessed);
        MakeTransition<ChargingState, CooldownState>(state => (state as ChargingState).IsChargeEnded);

        // SearchingTargetState State
        MakeTransition<SearchingTargetState, CastingState>(state => Owner.IsTargetSelectSuccessful && Owner.IsUseCast);
        MakeTransition<SearchingTargetState, InPrecedingActionState>(state => Owner.IsTargetSelectSuccessful && Owner.HasPrecedingAction);
        MakeTransition<SearchingTargetState, InActionState>(state => Owner.IsTargetSelectSuccessful);
        MakeTransition<SearchingTargetState, CooldownState>(state => !Owner.IsCooldownCompleted);
        MakeTransition<SearchingTargetState, ReadyState>(state => !Owner.IsSearchingTarget);

        // Casting State
        MakeTransition<CastingState, InPrecedingActionState>(state =>
        Owner.IsCastCompleted && Owner.HasPrecedingAction);
        MakeTransition<CastingState, InActionState>(state => Owner.IsCastCompleted);

        // InPrecedingAction State
        MakeTransition<InPrecedingActionState, InActionState>(state => (state as InPrecedingActionState).IsPrecedingActionEnded);

        // InAction State
        MakeTransition<InActionState, CooldownState>(state => Owner.IsFinished && Owner.HasCooldown);
        MakeTransition<InActionState, ReadyState>(state => Owner.IsFinished);

        // Cooldown State
        MakeTransition<CooldownState, ReadyState>(state => Owner.IsCooldownCompleted);

        // Any State
        MakeAnyTransition<CooldownState>(SkillExecuteCommand.Cancel, state =>
        !(IsInState<InActionState>() && Owner.ExecutionType == SkillExecutionType.Input) && Owner.IsActivated && Owner.HasCooldown);
        MakeAnyTransition<ReadyState>(SkillExecuteCommand.Cancel, state =>
        !(IsInState<InActionState>() && Owner.ExecutionType == SkillExecutionType.Input));

        MakeAnyTransition<CooldownState>(SkillExecuteCommand.CancelImmediately, state => Owner.IsActivated && Owner.HasCooldown);
        MakeAnyTransition<ReadyState>(SkillExecuteCommand.CancelImmediately);
    }
}
