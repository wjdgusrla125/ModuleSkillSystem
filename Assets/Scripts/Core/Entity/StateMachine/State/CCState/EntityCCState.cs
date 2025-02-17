using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityCCState : State<Entity>
{
    public abstract string Description { get; }
    protected abstract int AnimationHash { get; }

    public override void Enter()
    {
        Entity.Animator?.SetBool(AnimationHash, true);
        Entity.Movement?.Stop();
        //Entity.SkillSystem.CancelAll();

        var playerController = Entity.GetComponent<PlayerController>();
        if (playerController)
            playerController.enabled = false;
    }

    public override void Exit()
    {
        Entity.Animator?.SetBool(AnimationHash, false);

        var playerController = Entity.GetComponent<PlayerController>();
        if (playerController)
            playerController.enabled = true;
    }
}