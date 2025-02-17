using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InPrecedingActionState : SkillState
{
    public bool IsPrecedingActionEnded { get; private set; }

    public override void Enter()
    {
        if (!Entity.IsActivated)
            Entity.Activate();

        TrySendCommandToOwner(Entity, EntityStateCommand.ToInSkillPrecedingActionState, Entity.PrecedingActionAnimationParameter);

        Entity.StartPrecedingAction();
    }

    public override void Update()
    {
        IsPrecedingActionEnded = Entity.RunPrecedingAction();
    }

    public override void Exit()
    {
        IsPrecedingActionEnded = false;

        Entity.ReleasePrecedingAction();
    }
}