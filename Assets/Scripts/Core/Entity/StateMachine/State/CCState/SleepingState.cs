using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SleepingState : EntityCCState
{
    private static readonly int kAnimationHash = Animator.StringToHash("isSleeping");

    public override string Description => "수면";
    protected override int AnimationHash => kAnimationHash;
}