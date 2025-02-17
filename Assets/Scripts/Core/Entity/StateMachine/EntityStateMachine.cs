using System.Diagnostics;

public class EntityStateMachine : MonoStateMachine<Entity>
{
    protected override void AddStates()
    {
        AddState<EntityDefaultState>();
        AddState<DeadState>();
        AddState<RollingState>();
        
        // Skill이 Casting 중일 때 Entity의 상태
        AddState<CastingSkillState>();
        // Skill이 Charging 중일 때 Entity의 상태
        AddState<ChargingSkillState>();
        // Skill이 Preceding Action 중일 때 Entity의 상태
        AddState<InSkillPrecedingActionState>();
        // Skill이 발동 중일 때 Entity의 상태
        AddState<InSkillActionState>();
        // Entity가 Stun CC기를 맞았을 때의 상태
        AddState<StunningState>();
        // Entity가 Sleep CC기를 맞았을 때의 상태
        AddState<SleepingState>();
    }

    protected override void MakeTransitions()
    {
        // Default State
        MakeTransition<EntityDefaultState, RollingState>(state => Owner.Movement?.IsRolling ?? false);
        MakeTransition<EntityDefaultState, CastingSkillState>(EntityStateCommand.ToCastingSkillState);
        MakeTransition<EntityDefaultState, ChargingSkillState>(EntityStateCommand.ToChargingSkillState);
        MakeTransition<EntityDefaultState, InSkillPrecedingActionState>(EntityStateCommand.ToInSkillPrecedingActionState);
        MakeTransition<EntityDefaultState, InSkillActionState>(EntityStateCommand.ToInSkillActionState);

        // Rolling State
        MakeTransition<RollingState, EntityDefaultState>(state => !Owner.Movement.IsRolling);

        // Skill State
            // Casting State
        MakeTransition<CastingSkillState, InSkillPrecedingActionState>(EntityStateCommand.ToInSkillPrecedingActionState);
        MakeTransition<CastingSkillState, InSkillActionState>(EntityStateCommand.ToInSkillActionState);
        MakeTransition<CastingSkillState, EntityDefaultState>(state => !IsSkillInState<CastingState>(state));

            // Charging State
        MakeTransition<ChargingSkillState, InSkillPrecedingActionState>(EntityStateCommand.ToInSkillPrecedingActionState);
        MakeTransition<ChargingSkillState, InSkillActionState>(EntityStateCommand.ToInSkillActionState);
        MakeTransition<ChargingSkillState, EntityDefaultState>(state => !IsSkillInState<ChargingState>(state));

            // PrecedingAction State
        MakeTransition<InSkillPrecedingActionState, InSkillActionState>(EntityStateCommand.ToInSkillActionState);
        MakeTransition<InSkillPrecedingActionState, EntityDefaultState>(state => !IsSkillInState<InPrecedingActionState>(state));

            //Action State
        MakeTransition<InSkillActionState, EntityDefaultState>(state => (state as InSkillActionState).IsStateEnded);

        // CC State
            // Stuning State
        MakeAnyTransition<StunningState>(EntityStateCommand.ToStunningState);

            // Sleeping State
        MakeAnyTransition<SleepingState>(EntityStateCommand.ToSleepingState);

        MakeAnyTransition<EntityDefaultState>(EntityStateCommand.ToDefaultState);

        MakeAnyTransition<DeadState>(state => Owner.IsDead);
        MakeTransition<DeadState, EntityDefaultState>(state => !Owner.IsDead);
    }

    private bool IsSkillInState<T>(State<Entity> state) where T : State<Skill>
        => (state as EntitySkillState).RunningSkill.IsInState<T>();
}
