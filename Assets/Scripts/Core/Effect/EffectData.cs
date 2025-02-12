using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct EffectData
{
    // 현재 Data가 Effect의 몇 Level Data인지에 대한 정보
    // 예를 들어, level이 3일 경우, Effect의 3 Level Data라는 의미
    public int level;

    [UnderlineTitle("Stack")]
    [Min(1)]
    // Effect가 중첩될 수 있는 최대 Stack
    public int maxStack;
    // Stack에 따른 추가 효과들
    public EffectStackAction[] stackActions;

    [UnderlineTitle("Action")]
    [SerializeReference, SubclassSelector]
    // Effect의 실제 효과를 담당하는 Module
    // EffectAction을 통해 공격, 치유, 버프 같은 실제 Effect의 효과가 구현됨
    public EffectAction action;

    [UnderlineTitle("Setting")]
    // Effect를 완료할 시점
    public EffectRunningFinishOption runningFinishOption;
    // Effect의 지속 시간이 만료되었을 때, 남은 적용 횟수가 있다면 모두 적용할 것인지 여부
    public bool isApplyAllWhenDurationExpires;
    // Effect의 지속시간, StatScaleFloat Type이기 때문에
    // 특정 Stat을 통해 지속 시간을 늘리거나 줄일 수 있음
    public StatScaleFloat duration;
    // Effect를 적용할 횟수
    [Min(0)]
    public int applyCount;
    // Effect를 적용할 주기
    // 첫 한번은 효과가 바로 적용될 것이기 때문에, 한번 적용된 후부터 ApplyCycle에 따라 적용됨
    // 예를 들어서, ApplyCycle이 1초라면, 바로 한번 적용된 후 1초마다 적용되게 됨. 
    [Min(0f)]
    public float applyCycle;

    // Effect에 다양한 연출을 주기위한 Module
    // ex. Particle Spawn, Sound 출력, Camera Shake 등
    [UnderlineTitle("Custom Action")]
    [SerializeReference, SubclassSelector]
    public CustomAction[] customActions;
}