using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CooldownState : State<Skill>
{
    public override void Enter()
    {
        if (Layer == 0 && Entity.IsActivated)
            Entity.Deactivate();

        if (Entity.IsCooldownCompleted)
            Entity.CurrentCooldown = Entity.Cooldown;
    }

    public override void Update() => Entity.CurrentCooldown -= Time.deltaTime;
}