// Skill를 언제 끝낼 것인가
// FinishWhenApplyCompleted는 applyCount만큼 모두 적용이되면 종료
// DurationEnded는 applyCount만큼 적용했든, 안했든 시간이 지나면 종료
public enum SkillRunningFinishOption
{
    FinishWhenApplyCompleted,
    FinishWhenDurationEnded,
}

// Skill가 Charge가 끝나면(지속 시간이 끝나면) 어떤 행동을 취할 것인가?
// Use는 Skill을 자동으로 사용함
// Cancel을 Skill의 사용이 취소됨
public enum SkillChargeFinishActionOption
{
    Use,
    Cancel,
}

// 직접 사용해야하는 스킬인가?(Active)
// 자동으로 사용되는 스킬인가?(Passive)
public enum SkillType
{
    Active,
    Passive
}

// 필요한 TargetSearcher 검색 결과 Type은 무엇인가?
public enum NeedSelectionResultType
{
    Target,
    Position
}

// 단발성 스킬인가?(Instant)
// 껐다, 켰다 할 수 있는 Toggle형 스킬인가?
public enum SkillUseType
{
    Instant,
    Toggle
}

// TargetSearcher가 언제 Target을 Search 하는가?
// TargetSelectionCompleted는 Target 선택(Selection)이 되었을 때 바로 Search를 실행함
// (Skill의 Target이 변하지 않고 고정일 때 사용)
// Apply는 Skill이 적용될 때 Search를 실행함
// (Skill이 적용될 때마다 Target이 달라질 수 있을 경우 사용)
public enum TargetSearchTimingOption
{
    // Target 선택(Selection)이 되었을 때
    // (TargetSearcher가 SelectTarget을 완료하면 바로 Search 실행)
    TargetSelectionCompleted,
    // Skill이 적용될 때
    Apply
}

// 언제 사용할 CustomAction인가?
public enum SkillCustomActionType
{
    Cast,
    Charge,
    PrecedingAction,
    Action,
}

// Skill을 사용했을 때 어떤 식으로 실행될 것인가?
// Auto는 Skill이 ApplyCount만큼 자동 실행됨.
// Input은 특정 Key를 눌러야 Skill이 실행됨.
// (ex. 롤 리븐의 Q 스킬 - Q Button을 눌러서 스킬을 #초 동안 최대 3번 사용할 수 있음)
public enum SkillExecutionType
{
    Auto,
    Input
}

// TargetSearcher의 SelectTarget이 실행되는 타이밍이 언제인가?
// Use는 Skill을 사용할 때 선택함
// UseInAction은 위의 ExecutionType이 Input일 때 Skill을 실행시킬 때마다 선택함.
public enum TargetSelectionTimingOption
{
    Use,
    UseInAction,
    Both
}

// Skill의 적용 시점
// Instant는 Skill이 실행되면 바로 적용함
// Animation은 Skill의 적용 시점을 Animation에서 결정하여 적용함.
// 이를 통해 캐릭터가 주먹을 내질렀을 때나, 검이 적을 벨 때 등 Skill의 적용 시점을 정교하게 정할 수 있음.
public enum SkillApplyType
{
    Instant,
    Animation
}

// Skill을 사용하는 Entity의 InSkillActionState를 언제끝낼 것인가?
// FinishOnceApplied는 Skill이 한번 적용되자마자 바로 캐릭터를 제어할 수 있음.
// FinishWhenFullyApplied는 Skill이 ApplyCount만큼 모두 적용되야 캐릭터를 제어할 수 있음.
// FinishWhenAnimationEnded는 Skill의 적용 여부와 상관없이 현재 실행중인 Animation이 끝나야 캐릭터를 제어할 수 있음
// 예를 들어, 양손으로 파이어볼을 날리는 애니메이션일 때,
// 파이어볼을 날리는 애니메이션이 완전히 끝나서 기본 동작으로 돌아오면 캐릭터의 제어가 가능해짐.
public enum InSkillActionFinishOption
{
    FinishOnceApplied,
    FinishWhenFullyApplied,
    FinishWhenAnimationEnded,
}