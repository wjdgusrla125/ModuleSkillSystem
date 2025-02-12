public enum EffectType
{
    None,
    Buff,
    Debuff
}

// Effect가 중복 적용 허용을 하지 않을 시, 만약 중복 적용이 일어났다면 중복 적용된 Effect 둘 중 무엇을 제거할 것인가?
public enum EffectRemoveDuplicateTargetOption
{
    // 이미 적용중인 Effect를 제거
    Old,
    // 새로 적용된 Effect를 제거
    New,
}

// Effect의 완료 시점이 언제인가?
public enum EffectRunningFinishOption
{
    // Effect가 설정된 적용 횟수만큼 적용된다면 완료되는 Option.
    // 단, 이 Option은 지속 시간(=Duration)이 끝나도 완료됨.
    // 타격을 입힌다던가, 치료를 해주는 Effect에 적합Option
    FinishWhenApplyCompleted,
    // 지속 시간이 끝나면 완료되는 Option.
    // Effect가 설정된 적용 횟수만큼 적용되도, 지속 시간이 남았다면 완료가 안됨.
    // 처음 한번 적용되고, 일정 시간동안 지속되는 Buff나 Debuff Effect에 적합한 Option.
    FinishWhenDurationEnded,
}