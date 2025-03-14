using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [Serializable]
// public class ApplyData
// {
//     public int applyCount;
//     
//     [UnderlineTitle("Effect")]
//     public EffectSelector[] effectSelectors;
//     
//     [UnderlineTitle("Animation")]
//     public InSkillActionFinishOption inSkillActionFinishOption;
//     public AnimatorParameter actionAnimatorParameter;
//     
//     [SerializeReference, SubclassSelector]
//     public CustomAction[] customActionsOnAction;
// }

[Serializable]
public struct SkillData
{
    public int level;

    // Skill Level Up을 위한 조건
    [UnderlineTitle("Level Up")]
    [SerializeReference, SubclassSelector]
    public EntityCondition[] levelUpConditions;
    
    // Skill Level Up을 위한 비용
    [SerializeReference, SubclassSelector]
    public Cost[] levelUpCosts;

    // Skill이 실제 사용되기 전 먼저 실행할 Action, 아무 효과 없이 어떤 동작을 수행하기 위해 존재
    // ex. 상대방에게 달려감, 구르기를 함, Jump를 함 등
    [UnderlineTitle("Preceding Action")]
    [SerializeReference, SubclassSelector]
    public SkillPrecedingAction precedingAction;

    // Skill의 사용 방식을 담당하는 Module
    // ex. 투사체 발사, Target에게 즉시 적용, Skill Object Spawn 등
    [UnderlineTitle("Action")]
    [SerializeReference, SubclassSelector]
    public SkillAction action;

    [UnderlineTitle("Setting")]
    public SkillRunningFinishOption runningFinishOption;
    // runningFinishOption이 FinishWhenDurationEnded이고 duration이 0이면 무한 지속
    [Min(0)]
    public float duration;
    // applyCount가 0이면 무한 적용
    [Min(0)]
    public int applyCount;
    // 첫 한번은 효과가 바로 적용될 것이기 때문에, 한번 적용된 후부터 ApplyCycle에 따라 적용됨
    // 예를 들어서, ApplyCycle이 1초라면, 바로 한번 적용된 후 1초마다 적용되게 됨. 
    [Min(0f)]
    public float applyCycle;
    
    // applyCount가 0보다 클 때 각 Apply마다 사용할 데이터
    // [UnderlineTitle("Apply Datas")]
    // public ApplyData[] applyDatas;

    public StatScaleFloat cooldown;

    // Skill의 적용 대상을 찾기 위한 Class
    [UnderlineTitle("Target Searcher")]
    public TargetSearcher targetSearcher;

    // Skill 사용을 위한 비용
    [UnderlineTitle("Cost")]
    [SerializeReference, SubclassSelector]
    public Cost[] costs;

    [UnderlineTitle("Cast")]
    public bool isUseCast;
    public StatScaleFloat castTime;

    [UnderlineTitle("Charge")]
    public bool isUseCharge;
    public SkillChargeFinishActionOption chargeFinishActionOption;
    // Charge의 지속 시간
    [Min(0f)]
    public float chargeDuration;
    // Full Charge까지 걸리는 시간
    [Min(0f)]
    public float chargeTime;
    // Skill을 사용하기 위해 필요한 최소 충전 시간
    [Min(0f)]
    public float needChargeTimeToUse;
    // Charge의 시작 Power
    [Range(0f, 1f)]
    public float startChargePower;
    
    [UnderlineTitle("Effect")]
    public EffectSelector[] effectSelectors;

    [UnderlineTitle("Animation")]
    public InSkillActionFinishOption inSkillActionFinishOption;
    public AnimatorParameter castAnimatorParamter;
    public AnimatorParameter chargeAnimatorParameter;
    public AnimatorParameter precedingActionAnimatorParameter;
    public AnimatorParameter actionAnimatorParameter;

    [SerializeReference, SubclassSelector]
    public CustomAction[] customActionsOnCast;
    [SerializeReference, SubclassSelector]
    public CustomAction[] customActionsOnCharge;
    [SerializeReference, SubclassSelector]
    public CustomAction[] customActionsOnPrecedingAction;
    [SerializeReference, SubclassSelector]
    public CustomAction[] customActionsOnAction;
}