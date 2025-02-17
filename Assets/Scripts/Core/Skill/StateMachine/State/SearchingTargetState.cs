using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchingTargetState : State<Skill>
{
    public override void Enter() => Entity.SelectTarget();
    public override void Exit() => Entity.CancelSelectTarget();

}