using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunningState : EntityCCState
{
    private static readonly int kAnimationHash = Animator.StringToHash("isStunning");

    public override string Description => "기절";
    protected override int AnimationHash => kAnimationHash;
}