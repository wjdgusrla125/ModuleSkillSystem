using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// IdentifiedObject를 상속받는 Skill 클래스 정의
// 게임 내 캐릭터나 엔티티가 사용할 수 있는 스킬을 구현
public class Skill : IdentifiedObject
{
    // 무한대를 표현하기 위한 상수 (0으로 설정됨)
    private const int kInfinity = 0;
    
    // 이벤트 델리게이트 정의 시작
    // 스킬 레벨이 변경될 때 호출될 이벤트
    public delegate void LevelChangedHandler(Skill skill, int currentLevel, int prevLevel);
    // 스킬 상태가 변경될 때 호출될 이벤트
    public delegate void StateChangedHandler(Skill skill, State<Skill> newState, State<Skill> prevState, int layer);
    // 스킬이 적용될 때 호출될 이벤트
    public delegate void AppliedHander(Skill skill, int currentApplyCount);
    // 스킬이 사용될 때 호출될 이벤트
    public delegate void UsedHandler(Skill skill);
    // 스킬이 활성화될 때 호출될 이벤트 (Use 직후)
    public delegate void ActivatedHandler(Skill skill);
    // 스킬이 비활성화될 때 호출될 이벤트 (종료 직후)
    public delegate void DeactivatedHandler(Skill skill);
    // 스킬이 취소될 때 호출될 이벤트
    public delegate void CanceledHandler(Skill skill);
    // 타겟 선택이 완료됐을 때 호출될 이벤트
    public delegate void TargetSelectionCompletedHandler(Skill skill, TargetSearcher targetSearcher, TargetSelectionResult result);
    // 현재 적용 횟수가 변경될 때 호출될 이벤트
    public delegate void CurrentApplyCountChangedHandler(Skill skill, int currentApplyCount, int prevApplyCount);
    
    // 스킬 기본 속성 정의 (Unity 인스펙터에서 설정 가능한 필드들)
    [SerializeField] private SkillType type;                   // 스킬 유형 (액티브/패시브)
    [SerializeField] private SkillUseType useType;             // 스킬 사용 유형 (일반/토글)

    [SerializeField] private SkillExecutionType executionType; // 스킬 실행 유형 (자동/입력)
    [SerializeField] private SkillApplyType applyType;         // 스킬 적용 유형

    [SerializeField] private NeedSelectionResultType needSelectionResultType;           // 필요한 선택 결과 유형
    [SerializeField] private TargetSelectionTimingOption targetSelectionTimingOption;   // 타겟 선택 타이밍 옵션
    [SerializeField] private TargetSearchTimingOption targetSearchTimingOption;         // 타겟 검색 타이밍 옵션

    // 스킬 획득 조건 (서브클래스 선택기 속성을 통해 다양한 타입의 조건 설정 가능)
    [SerializeReference, SubclassSelector]
    private EntityCondition[] acquisitionConditions;
    // 스킬 획득에 필요한 비용 
    [SerializeReference, SubclassSelector]
    private Cost[] acquisitionCosts;

    // 스킬 사용 조건
    [SerializeReference, SubclassSelector]
    private SkillCondition[] useConditions;

    // 스킬 레벨 관련 설정
    [SerializeField] private bool isAllowLevelExceedDatas;  // 데이터에 정의된 레벨을 초과할 수 있는지 여부
    [SerializeField] private int maxLevel;                  // 최대 레벨
    [SerializeField, Min(1)] 
    private int defaultLevel = 1;                           // 기본 레벨 (최소 1)
    [SerializeField] private SkillData[] skillDatas;        // 레벨별 스킬 데이터 배열
    
    // 현재 사용 중인 스킬 데이터
    private SkillData currentData;

    // 현재 스킬 상태 관련 변수들
    private int level;                  // 현재 레벨
    private int currentApplyCount;      // 현재 적용 횟수
    private float currentCastTime;      // 현재 캐스팅 시간
    private float currentCooldown;      // 현재 쿨다운 시간
    private float currentDuration;      // 현재 지속 시간
    private float currentChargePower;   // 현재 차지 파워 (0~1 사이 값)
    private float currentChargeDuration; // 현재 차지 지속 시간

    // 스킬 커스텀 액션을 타입별로 저장하는 딕셔너리
    private readonly Dictionary<SkillCustomActionType, CustomAction[]> customActionsByType = new();

    // 스킬 소유자와 상태 기계 속성
    public Entity Owner { get; private set; }                 // 스킬 소유자 (캐릭터 등)
    public StateMachine<Skill> StateMachine { get; private set; }  // 스킬 상태 관리 기계

    // 각종 스킬 속성에 대한 읽기 전용 접근자 (getter)
    public SkillType Type => type;
    public SkillUseType UseType => useType;

    public SkillExecutionType ExecutionType => executionType;
    public SkillApplyType ApplyType => applyType;

    public IReadOnlyList<EntityCondition> AcquisitionConditions => acquisitionConditions;
    public IReadOnlyList<Cost> AcquisitionCosts => acquisitionCosts;

    public IReadOnlyList<EntityCondition> LevelUpConditions => currentData.levelUpConditions;
    public IReadOnlyList<Cost> LevelUpCosts => currentData.levelUpCosts;

    public IReadOnlyList<SkillCondition> UseConditions => useConditions;

    public IReadOnlyList<Effect> Effects { get; private set; } = Array.Empty<Effect>();

    // 레벨 관련 속성
    public int MaxLevel => maxLevel;
    // 현재 레벨 (set 시 여러 유효성 검사 및 필요한 업데이트 수행)
    public int Level
    {
        get => level;
        set
        {
            Debug.Assert(value >= 1 && value <= MaxLevel, 
                $"Skill.Rank = {value} - value는 1과 MaxLevel({MaxLevel}) 사이 값이여야합니다.");

            if (level == value)
                return;

            int prevLevel = level;
            level = value;

            // 새로운 Level과 가장 가까운 Level Data를 찾아옴
            var newData = skillDatas.Last(x => x.level <= level);
            if (newData.level != currentData.level)
                ChangeData(newData);

            onLevelChanged?.Invoke(this, level, prevLevel);
        }
    }
    // 현재 레벨과 데이터 레벨의 차이 (보너스 레벨)
    public int DataBonusLevel => Mathf.Max(level - currentData.level, 0);
    // 최대 레벨 도달 여부
    public bool IsMaxLevel => level == maxLevel;
    // 레벨업 가능 여부 (최대 레벨이 아니고, 조건 충족, 비용 충분)
    public bool IsCanLevelUp => !IsMaxLevel && LevelUpConditions.All(x => x.IsPass(Owner)) &&
        LevelUpCosts.All(x => x.HasEnoughCost(Owner));

    // 스킬 액션 관련 속성
    private SkillPrecedingAction PrecedingAction => currentData.precedingAction;  // 선행 액션
    private SkillAction Action => currentData.action;                            // 주 액션
    public bool HasPrecedingAction => PrecedingAction != null;                   // 선행 액션 존재 여부

    // 스킬 액션 완료 옵션
    public InSkillActionFinishOption InSkillActionFinishOption => currentData.inSkillActionFinishOption;
    
    // 애니메이션 파라미터 관련 속성들
    public AnimatorParameter CastAnimationParameter
    {
        get
        {
            var constValue = currentData.castAnimatorParamter;
            return constValue;
        }
    }
    public AnimatorParameter ChargeAnimationParameter
    {
        get
        {
            var constValue = currentData.chargeAnimatorParameter;
            return constValue;
        }
    }
    public AnimatorParameter PrecedingActionAnimationParameter
    {
        get
        {
            var constValue = currentData.precedingActionAnimatorParameter;
            return constValue;
        }
    }
    public AnimatorParameter ActionAnimationParameter
    {
        get
        {
            var constValue = currentData.actionAnimatorParameter;
            return constValue;
        }
    }

    // 타겟 검색기 관련 속성
    public TargetSearcher TargetSearcher => currentData.targetSearcher;
    public bool IsSearchingTarget => TargetSearcher.IsSearching;
    public TargetSelectionResult TargetSelectionResult => TargetSearcher.SelectionResult;
    public TargetSearchResult TargetSearchResult => TargetSearcher.SearchResult;
    
    // 타겟 선택 결과가 스킬이 필요로 하는 타입과 일치하는지 확인
    public bool HasValidTargetSelectionResult
    {
        get
        {
            return TargetSelectionResult.resultMessage switch
            {
                SearchResultMessage.FindTarget => needSelectionResultType == NeedSelectionResultType.Target,
                SearchResultMessage.FindPosition => needSelectionResultType == NeedSelectionResultType.Position,
                _ => false
            };
        }
    }
    // 타겟 선택이 성공적으로 완료됐는지 여부
    public bool IsTargetSelectSuccessful => !IsSearchingTarget && HasValidTargetSelectionResult;

    // 비용 관련 속성
    public IReadOnlyList<Cost> Costs => currentData.costs;
    public bool HasCost => Costs.Count > 0;
    public bool HasEnoughCost => Costs.All(x => x.HasEnoughCost(Owner));

    // 쿨다운 관련 속성
    public float Cooldown => currentData.cooldown.GetValue(Owner.Stats);
    public bool HasCooldown => Cooldown > 0f;
    public float CurrentCooldown
    {
        get => currentCooldown;
        set => currentCooldown = Mathf.Clamp(value, 0f, Cooldown);
    }
    public bool IsCooldownCompleted => Mathf.Approximately(0f, CurrentCooldown);

    // 지속시간 관련 속성
    public float Duration => currentData.duration;
    private bool IsTimeless => Mathf.Approximately(Duration, kInfinity);
    public float CurrentDuration
    {
        get => currentDuration;
        set => currentDuration = !IsTimeless ? Mathf.Clamp(value, 0f, Duration) : value;
    }

    // 스킬 종료 조건 옵션
    public SkillRunningFinishOption RunningFinishIption => currentData.runningFinishOption;
    
    // 적용 횟수 관련 속성
    public int ApplyCount => currentData.applyCount;
    private bool IsInfinitelyApplicable => ApplyCount == kInfinity;
    public int CurrentApplyCount
    {
        get => currentApplyCount;
        set
        {
            if (currentApplyCount == value)
                return;

            var prevApplyCount = currentApplyCount;
            currentApplyCount = Mathf.Clamp(value, 0, ApplyCount);

            onCurrentApplyCountChanged?.Invoke(this, currentApplyCount, prevApplyCount);
        }
    }
    
    // 적용 주기 계산 (자동 계산 또는 설정값)
    // currentData의 applyCycle이 0이고 applyCount가 1보다 크면(여러번 적용 가능하면)
    // Skill의 duration을 (ApplyCount - 1)로 나눠서 ApplyCycle을 계산하여 return 함.
    // 아니라면 설정된 currentData의 applyCycle을 그대로 return 함.
    public float ApplyCycle => Mathf.Approximately(currentData.applyCycle, 0f) && ApplyCount > 1 ?
        Duration / (ApplyCount - 1) : currentData.applyCycle;
    public float CurrentApplyCycle { get; set; }

    // 캐스팅 관련 속성
    public bool IsUseCast => currentData.isUseCast;
    public float CastTime => currentData.castTime.GetValue(Owner.Stats);
    public float CurrentCastTime
    {
        get => currentCastTime;
        set => currentCastTime = Mathf.Clamp(value, 0f, CastTime);
    }
    public bool IsCastCompleted => Mathf.Approximately(CastTime, CurrentCastTime);

    // 차지(충전) 관련 속성
    public bool IsUseCharge => currentData.isUseCharge;
    public SkillChargeFinishActionOption ChargeFinishActionOption => currentData.chargeFinishActionOption;
    public float ChargeTime => currentData.chargeTime;
    public float StartChargePower => currentData.startChargePower;
    public float CurrentChargePower
    {
        get => currentChargePower;
        set
        {
            var prevChargePower = currentChargePower;
            currentChargePower = Mathf.Clamp01(value);

            if (Mathf.Approximately(prevChargePower, currentChargePower))
                return;

            // 차지 파워에 따라 타겟 검색기 스케일과 이펙트 스케일 조정
            TargetSearcher.Scale = currentChargePower;

            foreach (var effect in Effects)
                effect.Scale = currentChargePower;
        }
    }
    
    // 충전의 지속 시간
    public float ChargeDuration => currentData.chargeDuration;
    
    // 현재 충전 지속 시간 및 충전 파워 계산
    // IsUseCharge가 false면 1로 고정,
    // true라면 Lerp를 통해서 StartChargePower부터 1까지 currentChargeDuration으로 보간함
    public float CurrentChargeDuration
    {
        get => currentChargeDuration;
        set
        {
            currentChargeDuration = Mathf.Clamp(value, 0f, ChargeDuration);
            CurrentChargePower = !IsUseCharge ? 1f :
                Mathf.Lerp(StartChargePower, 1f, currentChargeDuration / ChargeTime);
        }
    }
    public float NeedChargeTimeToUse => currentData.needChargeTimeToUse;
    // 사용을 위한 필요한 ChargeTime에 도달했는가?
    public bool IsMinChargeCompleted => currentChargeDuration >= NeedChargeTimeToUse;
    // 최대 충전에 도달했는가?
    public bool IsMaxChargeCompleted => currentChargeDuration >= ChargeTime;
    // 충전의 지속 시간이 끝났는가?
    public bool IsChargeDurationEnded => Mathf.Approximately(ChargeDuration, CurrentChargeDuration);

    // 스킬 상태 관련 속성
    public bool IsPassive => type == SkillType.Passive;
    public bool IsToggleType => useType == SkillUseType.Toggle;
    public bool IsActivated { get; private set; }
    public bool IsReady => StateMachine.IsInState<ReadyState>();
    // 발동 횟수가 남았고, ApplyCycle만큼 시간이 지났으면 true를 return
    public bool IsApplicable => (CurrentApplyCount < ApplyCount || IsInfinitelyApplicable) &&
    (CurrentApplyCycle >= ApplyCycle);
    
    // 스킬 사용 가능 여부 판단 (상태에 따라 다른 조건 적용)
    public bool IsUseable
    {
        get
        {
            if (IsReady)
                return HasEnoughCost && useConditions.All(x => x.IsPass(this));
            // SkillExecutionType이 Input일 때, 사용자의 입력을 받을 수 있는 상태라면 true
            else if (StateMachine.IsInState<InActionState>())
                return ExecutionType == SkillExecutionType.Input && IsApplicable && useConditions.All(x => x.IsPass(this));
            // Skill이 Charge 중일 때 최소 사용 충전량을 달성하면 true
            else if (StateMachine.IsInState<ChargingState>())
                return IsMinChargeCompleted;
            else
                return false;
        }
    }

    // 타겟 관련 속성
    public IReadOnlyList<Entity> Targets { get; private set; }
    public IReadOnlyList<Vector3> TargetPositions { get; private set; }

    // 스킬 종료 조건 확인
    private bool IsDurationEnded => !IsTimeless && Mathf.Approximately(Duration, CurrentDuration);
    private bool IsApplyCompleted => !IsInfinitelyApplicable && CurrentApplyCount == ApplyCount;
    // 스킬의 발동이 종료되었는가? (설정된 종료 옵션에 따라 다름)
    public bool IsFinished => currentData.runningFinishOption == SkillRunningFinishOption.FinishWhenDurationEnded ?
        IsDurationEnded : IsApplyCompleted;

    // 스킬 설명 생성 (기본 설명에 여러 키워드 값 및 타겟, 액션, 이펙트 정보 추가)
    public override string Description
    {
        get
        {
            string description = base.Description;

            var stringsByKeyword = new Dictionary<string, string>()
            {
                { "duration", Duration.ToString("0.##") },
                { "applyCount", ApplyCount.ToString() },
                { "applyCycle", ApplyCycle.ToString("0.##") },
                { "castTime", CastTime.ToString("0.##") },
                { "chargeDuration", ChargeDuration.ToString("0.##") },
                { "chargeTime", ChargeTime.ToString("0.##") },
                { "needChargeTimeToUse", NeedChargeTimeToUse.ToString("0.##") }
            };

            description = TextReplacer.Replace(description, stringsByKeyword);
            description = TargetSearcher.BuildDescription(description);

            if (PrecedingAction != null)
                description = PrecedingAction.BuildDescription(description);

            description = Action.BuildDescription(description);

            for (int i = 0; i < Effects.Count; i++)
                description = Effects[i].BuildDescription(description, i);

            return description;
        }
    }
    
    //수정 부분
    // Skill 클래스에 이 속성 추가
    public ApplyData CurrentApplyData
    {
        get
        {
            if (!currentData.useApplySpecificData || currentData.applyDatas == null || currentData.applyDatas.Length == 0)
                return null;
            
            var applyData = Array.Find(currentData.applyDatas, data => data.applyIndex == currentApplyCount);
            return applyData ?? (currentData.applyDatas.Length > 0 ? currentData.applyDatas[currentData.applyDatas.Length - 1] : null);
        }
    }

    // 현재 적용 횟수에 따른 애니메이션 파라미터를 가져오는 메서드 추가
    public AnimatorParameter GetCurrentActionAnimationParameter()
    {
        var applyData = CurrentApplyData;
        return applyData != null ? applyData.actionAnimatorParameter : ActionAnimationParameter;
    }

    
    // 이벤트 선언
    public event LevelChangedHandler onLevelChanged;
    public event StateChangedHandler onStateChanged;
    public event AppliedHander onApplied;
    public event ActivatedHandler onActivated;
    public event DeactivatedHandler onDeactivated;
    public event UsedHandler onUsed;
    public event CanceledHandler onCanceled;
    public event TargetSelectionCompletedHandler onTargetSelectionCompleted;
    public event CurrentApplyCountChangedHandler onCurrentApplyCountChanged;

    // 객체 파괴 시 이펙트도 함께 파괴
    public void OnDestroy()
    {
        foreach (var effect in Effects)
            Destroy(effect);
    }

    // 스킬 초기 설정 (소유자와 레벨 지정)
    public void Setup(Entity owner, int level)
    {
        Debug.Assert(owner != null, $"Skill::Setup - Owner는 Null이 될 수 없습니다.");
        Debug.Assert(level >= 1 && level <= maxLevel, $"Skill::Setup - {level}이 1보다 작거나 {maxLevel}보다 큽니다.");
        Debug.Assert(Owner == null, $"Skill::Setup - 이미 Setup하였습니다.");

        Owner = owner;
        Level = level;

        SetupStateMachine();
    }

    // 기본 레벨로 설정
    public void Setup(Entity owner) => Setup(owner, defaultLevel);

    // 스킬 타입에 맞는 상태 기계 설정
    private void SetupStateMachine()
    {
        if (Type == SkillType.Passive)
            StateMachine = new PassiveSkillStateMachine();
        else if (UseType == SkillUseType.Toggle)
            StateMachine = new ToggleSkillStateMachine();
        else
            StateMachine = new InstantSkillStateMachine();

        StateMachine.Setup(this);
        StateMachine.onStateChanged += (_, newState, prevState, layer)
            => onStateChanged?.Invoke(this, newState, prevState, layer);
    }

    // 스킬 속성 초기화
    public void ResetProperties()
    {
        CurrentCastTime = 0f;
        CurrentCooldown = 0f;
        CurrentDuration = 0f;
        CurrentApplyCycle = 0f;
        CurrentChargeDuration = 0f;
        CurrentApplyCount = 0;
    }

    // 상태 기계 업데이트
    public void Update() => StateMachine.Update();

    // 커스텀 액션 업데이트
    private void UpdateCustomActions()
    {
        customActionsByType[SkillCustomActionType.Cast] = currentData.customActionsOnCast;
        customActionsByType[SkillCustomActionType.Charge] = currentData.customActionsOnCharge;
        customActionsByType[SkillCustomActionType.PrecedingAction] = currentData.customActionsOnPrecedingAction;
        customActionsByType[SkillCustomActionType.Action] = currentData.customActionsOnAction;
    }

    // 현재 이펙트 레벨 업데이트 (데이터 레벨과 스킬 레벨 차이만큼 보너스 레벨 부여)
    private void UpdateCurrentEffectLevels()
    {
        int bonusLevel = DataBonusLevel;
        foreach (var effect in Effects)
            effect.Level = Mathf.Min(effect.Level + bonusLevel, effect.MaxLevel);
    }

    // 스킬 데이터 변경 (레벨업 등으로 인해)
    private void ChangeData(SkillData newData)
    {
        foreach (var effect in Effects)
            Destroy(effect);

        currentData = newData;

        Effects = currentData.effectSelectors.Select(x => x.CreateEffect(this)).ToArray();
        
        // 스킬의 현재 Level이 data의 Level보다 크면, 둘의 Level 차를 Effect의 Bonus Level 줌.
        // 만약 스킬이 2 Level이고, data가 1 level이라면, effect들은 2-1해서 1의 Bonus Level을 받게 됨.
        if (level > currentData.level)
            UpdateCurrentEffectLevels();

        UpdateCustomActions();
    }

    // 레벨업 실행
    public void LevelUp()
    {
        Debug.Assert(IsCanLevelUp, "Skill::LevelUP - Level Up 조건을 충족하지 못했습니다.");

        foreach (var cost in LevelUpCosts)
            cost.UseCost(Owner);

        Level++;
    }

    // 획득 비용 충분 여부 확인
    public bool HasEnoughAcquisitionCost(Entity entity)
        => acquisitionCosts.All(x => x.HasEnoughCost(entity));

    // 스킬 획득 가능 여부 확인 (조건 충족 및 비용 충분)
    public bool IsAcquirable(Entity entity)
        => acquisitionConditions.All(x => x.IsPass(entity)) && HasEnoughAcquisitionCost(entity);

    // 획득 비용 사용
    public void UseAcquisitionCost(Entity entity)
    {
        foreach (var cost in acquisitionCosts)
            cost.UseCost(entity);
    }

    // 타겟 표시기 표시
    public void ShowIndicator()
        => TargetSearcher.ShowIndicator(Owner.gameObject);

    // 타겟 표시기 숨김
    public void HideIndicator()
        => TargetSearcher.HideIndicator();

    // 타겟 선택 (콜백 함수 지정 가능)
    public void SelectTarget(Action<Skill, TargetSearcher, TargetSelectionResult> onSelectCompletedOrNull, bool isShowIndicator = true)
    {
        CancelSelectTarget();

        if (isShowIndicator)
            ShowIndicator();

        TargetSearcher.SelectTarget(Owner, Owner.gameObject, (targetSearcher, result) =>
        {
            if (isShowIndicator)
                HideIndicator();

            // 스킬이 필요로 하는 Type의 기준점 검색에 성공했고,
            // SearchTiming이 기준점 검색 직후라면(TargetSelectionCompleted) Target 검색 실행
            if (IsTargetSelectSuccessful && targetSearchTimingOption == TargetSearchTimingOption.TargetSelectionCompleted)
                SearchTargets();

            onSelectCompletedOrNull?.Invoke(this, targetSearcher, result);
            onTargetSelectionCompleted?.Invoke(this, targetSearcher, result);
        });
    }

    // 기본 콜백 없이 타겟 선택
    public void SelectTarget(bool isShowIndicator = true) => SelectTarget(null, isShowIndicator);

    // 타겟 선택 취소
    public void CancelSelectTarget(bool isHideIndicator = true)
    {
        if (!TargetSearcher.IsSearching)
            return;

        TargetSearcher.CancelSelect();

        if (isHideIndicator)
            HideIndicator();
    }

    // 타겟 검색 실행
    public void SearchTargets()
    {
        var result = TargetSearcher.SearchTargets(Owner, Owner.gameObject);
        Targets = result.targets.Select(x => x.GetComponent<Entity>()).ToArray();
        TargetPositions = result.positions;
    }

    // 즉시 타겟 선택 (위치 지정)
    public TargetSelectionResult SelectTargetImmediate(Vector3 position)
    {
        CancelSelectTarget();

        var result = TargetSearcher.SelectImmediate(Owner, Owner.gameObject, position);
        if (IsTargetSelectSuccessful && targetSearchTimingOption == TargetSearchTimingOption.TargetSelectionCompleted)
            SearchTargets();

        return result;
    }

    // 위치가 스킬 범위 내인지 확인
    public bool IsInRange(Vector3 position)
        => TargetSearcher.IsInRange(Owner, Owner.gameObject, position);

    // 스킬 사용
    public bool Use()
    {
        Debug.Assert(IsUseable, "Skill::Use - 사용 조건을 만족하지 못했습니다.");

        bool isUsed = StateMachine.ExecuteCommand(SkillExecuteCommand.Use) || StateMachine.SendMessage(SkillStateMessage.Use);
        if (isUsed)
            onUsed?.Invoke(this);

        return isUsed;
    }

    // 지정 위치에 즉시 스킬 사용
    public bool UseImmediately(Vector3 position)
    {
        Debug.Assert(IsUseable, "Skill::UseImmediately - 사용 조건을 만족하지 못했습니다.");

        SelectTargetImmediate(position);

        bool isUsed = StateMachine.ExecuteCommand(SkillExecuteCommand.UseImmediately) || StateMachine.SendMessage(SkillStateMessage.Use);
        if (isUsed)
            onUsed?.Invoke(this);

        return isUsed;
    }
    
    // Cancel 메소드: 스킬 실행을 취소하는 기능
    // isForce 매개변수로 강제 취소 여부를 결정
    public bool Cancel(bool isForce = false)
    {
        // 패시브 스킬은 취소할 수 없으므로 디버그 어서션으로 확인
        Debug.Assert(!IsPassive, "Skill::Cancel - Passive Skill은 Cancel 할 수 없습니다.");

        // isForce가 true면 즉시 취소 명령을, 아니면 일반 취소 명령을 상태 기계에 전달
        var isCanceled = isForce ? StateMachine.ExecuteCommand(SkillExecuteCommand.CancelImmediately) :
            StateMachine.ExecuteCommand(SkillExecuteCommand.Cancel);

        // 취소 성공시 onCanceled 이벤트 발생
        if (isCanceled)
            onCanceled?.Invoke(this);

        // 취소 성공 여부 반환
        return isCanceled;
    }

    // UseCost 메소드: 스킬 사용에 필요한 비용을 소모
    public void UseCost()
    {
        // 충분한 비용이 있는지 확인
        Debug.Assert(HasEnoughCost, "Skill::UseCost - 사용할 Cost가 부족합니다.");

        // 모든 비용 항목에 대해 비용 사용 처리
        foreach (var cost in Costs)
            cost.UseCost(Owner);
    }

    // UseDeltaCost 메소드: 스킬 사용에 필요한 증분 비용 소모
    // (지속 스킬 등에서 시간에 따른 비용 소모 시 사용)
    public void UseDeltaCost()
    {
        // 충분한 비용이 있는지 확인
        Debug.Assert(HasEnoughCost, "Skill::UseDeltaCost - 사용할 Cost가 부족합니다.");

        // 모든 비용 항목에 대해 증분 비용 사용 처리
        foreach (var cost in Costs)
            cost.UseDeltaCost(Owner);
    }

    // Activate 메소드: 스킬을 활성화 상태로 전환
    public void Activate()
    {
        // 이미 활성화된 상태인지 확인
        Debug.Assert(!IsActivated, "Skill::Activate - 이미 활성화되어 있습니다.");

        // 스킬 사용 비용 소모
        UseCost();

        // 활성화 상태로 설정하고 이벤트 발생
        IsActivated = true;
        onActivated?.Invoke(this);
    }

    // Deactivate 메소드: 스킬의 활성화 상태를 해제
    public void Deactivate()
    {
        // 활성화 상태인지 확인
        Debug.Assert(IsActivated, "Skill::Activate - Skill이 활성화되어있지 않습니다.");

        // 비활성화 상태로 설정하고 이벤트 발생
        IsActivated = false;
        onDeactivated?.Invoke(this); 
    }

    // StartCustomActions 메소드: 특정 타입의 커스텀 액션 시작
    public void StartCustomActions(SkillCustomActionType type)
    {
        // 해당 타입의 모든 커스텀 액션에 대해 시작 메소드 호출
        foreach (var customAction in customActionsByType[type])
            customAction.Start(this);
    }

    // RunCustomActions 메소드: 특정 타입의 커스텀 액션 실행
    public void RunCustomActions(SkillCustomActionType type)
    {
        // 해당 타입의 모든 커스텀 액션에 대해 실행 메소드 호출
        foreach (var customAction in customActionsByType[type])
            customAction.Run(this);
    }

    // ReleaseCustomActions 메소드: 특정 타입의 커스텀 액션 해제
    public void ReleaseCustomActions(SkillCustomActionType type)
    {
        // 해당 타입의 모든 커스텀 액션에 대해 해제 메소드 호출
        foreach (var customAction in customActionsByType[type])
            customAction.Release(this);
    }

    // StartPrecedingAction 메소드: 선행 액션 시작
    public void StartPrecedingAction()
    {
        // 선행 액션 관련 커스텀 액션 시작
        StartCustomActions(SkillCustomActionType.PrecedingAction);
        // 선행 액션 자체 시작
        PrecedingAction.Start(this);
    }

    // RunPrecedingAction 메소드: 선행 액션 실행
    // 반환값은 액션의 실행 결과(완료 여부 등)
    public bool RunPrecedingAction()
    {
        // 선행 액션 관련 커스텀 액션 실행
        RunCustomActions(SkillCustomActionType.PrecedingAction);
        // 선행 액션 자체 실행 및 결과 반환
        return PrecedingAction.Run(this);
    }

    // ReleasePrecedingAction 메소드: 선행 액션 해제
    public void ReleasePrecedingAction()
    {
        // 선행 액션 관련 커스텀 액션 해제
        ReleaseCustomActions(SkillCustomActionType.PrecedingAction);
        // 선행 액션 자체 해제
        PrecedingAction.Release(this);
    }

    // StartAction 메소드: 주 액션 시작
    public void StartAction()
    {
        // 주 액션 관련 커스텀 액션 시작
        StartCustomActions(SkillCustomActionType.Action);
        // 주 액션 자체 시작
        Action.Start(this); 
    }

    // ReleaseAction 메소드: 주 액션 해제
    public void ReleaseAction()
    {
        // 주 액션 관련 커스텀 액션 해제
        ReleaseCustomActions(SkillCustomActionType.Action);
        // 주 액션 자체 해제
        Action.Release(this);
    }

    // Apply 메소드: 스킬 효과 적용
    // isConsumeApplyCount 매개변수로 적용 횟수 소모 여부 제어
    /*public void Apply(bool isConsumeApplyCount = true)
    {
        // 무한 적용 가능하거나, 횟수를 소모하지 않거나, 적용 횟수가 남아있는지 확인
        Debug.Assert(IsInfinitelyApplicable || !isConsumeApplyCount || (CurrentApplyCount < ApplyCount),
            $"Skill({CodeName})의 최대 적용 횟수({ApplyCount})를 초과해서 적용할 수 없습니다.");

        // 타겟 검색 타이밍이 적용 시점이라면 타겟 검색 실행
        if (targetSearchTimingOption == TargetSearchTimingOption.Apply)
            SearchTargets();

        // 액션 관련 커스텀 액션 실행
        RunCustomActions(SkillCustomActionType.Action);

        // 주 액션의 적용 메소드 호출
        Action.Apply(this);

        // 실행 타입에 따라 적용 주기 관리
        // Auto인 경우: 지속시간과의 오차를 남기기 위해 나머지 연산 사용
        // Input인 경우: 사용자 입력으로 실행되므로 0으로 리셋
        if (executionType == SkillExecutionType.Auto)
            CurrentApplyCycle %= ApplyCycle;
        else
            CurrentApplyCycle = 0f;

        // 적용 횟수 소모가 필요하면 현재 적용 횟수 증가
        if (isConsumeApplyCount)
            CurrentApplyCount++;

        // 적용 완료 이벤트 발생
        onApplied?.Invoke(this, CurrentApplyCount);
    }*/
    
    public void Apply(bool isConsumeApplyCount = true)
{
    // 무한 적용 가능하거나, 횟수를 소모하지 않거나, 적용 횟수가 남아있는지 확인
    Debug.Assert(IsInfinitelyApplicable || !isConsumeApplyCount || (CurrentApplyCount < ApplyCount),
        $"Skill({CodeName})의 최대 적용 횟수({ApplyCount})를 초과해서 적용할 수 없습니다.");

    // 현재 적용 횟수에 해당하는 ApplyData를 찾습니다
    ApplyData currentApplyData = null;
    
    // 적용별 데이터 사용이 활성화되었고, 데이터가 존재하는 경우에만 처리
    if (currentData.useApplySpecificData && currentData.applyDatas != null && currentData.applyDatas.Length > 0)
    {
        // 현재 적용 횟수와 정확히 일치하는 applyIndex를 가진 ApplyData를 찾음
        currentApplyData = Array.Find(currentData.applyDatas, data => data.applyIndex == CurrentApplyCount);
        
        // 해당 인덱스가 없으면 가장 가까운 이전 인덱스의 데이터 사용
        if (currentApplyData == null)
        {
            // 현재 적용 횟수보다 작거나 같은 인덱스 중 가장 큰 것을 찾음
            int closestIndex = -1;
            foreach (var data in currentData.applyDatas)
            {
                if (data.applyIndex <= CurrentApplyCount && data.applyIndex > closestIndex)
                    closestIndex = data.applyIndex;
            }
            
            if (closestIndex >= 0)
                currentApplyData = Array.Find(currentData.applyDatas, data => data.applyIndex == closestIndex);
            else if (currentData.applyDatas.Length > 0)
                currentApplyData = currentData.applyDatas[0]; // 적절한 데이터가 없으면 첫 번째 사용
        }
    }

    // 적용할 이펙트 선택기 결정 (ApplyData가 있으면 해당 이펙트 사용, 없으면 기본 이펙트)
    EffectSelector[] effectSelectorsToUse = null;
    if (currentApplyData != null && currentApplyData.effectSelectors != null && currentApplyData.effectSelectors.Length > 0)
    {
        effectSelectorsToUse = currentApplyData.effectSelectors;
        Debug.Log($"Skill {CodeName}: 적용 #{CurrentApplyCount}에 대한 특정 이펙트 사용");
    }

    // 타겟 검색 타이밍이 적용 시점이라면 타겟 검색 실행
    if (targetSearchTimingOption == TargetSearchTimingOption.Apply)
    {
        Debug.Log($"Skill {CodeName}: 적용 시점에 타겟 검색 실행");
        SearchTargets();
    }

    // 커스텀 액션 실행 (ApplyData에 있으면 그것 사용, 없으면 기본 커스텀 액션)
    if (currentApplyData != null && currentApplyData.customActionsOnAction != null && currentApplyData.customActionsOnAction.Length > 0)
    {
        Debug.Log($"Skill {CodeName}: 적용 #{CurrentApplyCount}에 대한 특정 커스텀 액션 실행");
        foreach (var customAction in currentApplyData.customActionsOnAction)
        {
            if (customAction != null)
                customAction.Run(this);
        }
    }
    else
    {
        Debug.Log($"Skill {CodeName}: 기본 커스텀 액션 실행");
        RunCustomActions(SkillCustomActionType.Action);
    }

    // 애니메이션 파라미터 적용 (ApplyData에 정의된 경우)
    if (currentApplyData != null && !string.IsNullOrEmpty(currentApplyData.actionAnimatorParameter.ToString()))
    {
        Debug.Log($"Skill {CodeName}: 적용 #{CurrentApplyCount}에 대한 특정 애니메이션 파라미터 적용");
        // Owner의 Animator 컴포넌트 가져오기
        var animator = Owner.GetComponent<Animator>();
        if (animator != null)
        {
            // 애니메이션 파라미터 타입에 따라 적절한 메소드 호출
            switch (currentApplyData.actionAnimatorParameter.type)
            {
                case AnimatorParameterType.Trigger:
                    animator.SetTrigger(currentApplyData.actionAnimatorParameter.ToString());
                    break;
                case AnimatorParameterType.Bool:
                    animator.SetBool(currentApplyData.actionAnimatorParameter.ToString(), true);
                    break;
            }
        }
    }

    // 이펙트 적용 (ApplyData에 지정된 이펙트 또는 기본 액션)
    if (effectSelectorsToUse != null)
    {
        Debug.Log($"Skill {CodeName}: 적용 #{CurrentApplyCount}를 위한 {effectSelectorsToUse.Length}개의 이펙트 생성 및 적용");
        
        // 각 이펙트 선택기에서 이펙트 생성
        List<Effect> tempEffects = new List<Effect>();
        foreach (var selector in effectSelectorsToUse)
        {
            if (selector != null)
            {
                var effect = selector.CreateEffect(this);
                if (effect != null)
                {
                    tempEffects.Add(effect);
                    // 현재 차지 파워에 맞게 이펙트 스케일 조정
                    effect.Scale = CurrentChargePower;
                    // 이펙트 적용
                    effect.Apply();
                }
            }
        }
        
        // 임시 이펙트 정리 (사용 후 파괴)
        foreach (var effect in tempEffects)
        {
            if (effect != null)
                Destroy(effect);
        }
    }
    else
    {
        // 기본 액션 적용
        Debug.Log($"Skill {CodeName}: 기본 액션 적용");
        Action.Apply(this);
    }

    // 실행 타입에 따라 적용 주기 관리
    if (executionType == SkillExecutionType.Auto)
    {
        CurrentApplyCycle %= ApplyCycle;
        Debug.Log($"Skill {CodeName}: Auto 실행 타입, 현재 적용 주기 = {CurrentApplyCycle}");
    }
    else
    {
        CurrentApplyCycle = 0f;
        Debug.Log($"Skill {CodeName}: Input 실행 타입, 적용 주기 리셋");
    }

    // 적용 횟수를 증가시키기 전에 이벤트를 발생시켜서 현재 적용에 대한 정보 제공
    onApplied?.Invoke(this, CurrentApplyCount);

    // 적용 횟수 소모가 필요하면 현재 적용 횟수 증가
    if (isConsumeApplyCount)
    {
        int prevApplyCount = CurrentApplyCount;
        CurrentApplyCount++;
        Debug.Log($"Skill {CodeName}: 적용 횟수 증가 {prevApplyCount} -> {CurrentApplyCount}");
    }
}

    // IsInState 메소드: 스킬의 상태 기계가 특정 상태인지 확인
    public bool IsInState<T>() where T : State<Skill> => StateMachine.IsInState<T>();

    // 특정 레이어의 상태를 확인하는 오버로드 메소드
    public bool IsInState<T>(int layer) where T : State<Skill> => StateMachine.IsInState<T>(layer);

    // GetCurrentStateType 메소드: 현재 상태의 타입을 반환
    public Type GetCurrentStateType(int layer = 0) => StateMachine.GetCurrentStateType(layer);

    // IsTargetSelectionTiming 메소드: 현재가 타겟 선택 타이밍인지 확인
    // 설정이 'Both'이거나 주어진 옵션과 일치하면 true 반환
    public bool IsTargetSelectionTiming(TargetSelectionTimingOption option)
        => targetSelectionTimingOption == TargetSelectionTimingOption.Both || targetSelectionTimingOption == option;

    // Clone 메소드: 스킬 객체 복제
    // 부모 클래스(IdentifiedObject)의 Clone 메소드 오버라이드
    public override object Clone()
    {
        // Unity의 Instantiate를 사용하여 복제
        var clone = Instantiate(this);

        // 소유자가 있으면 복제된 스킬도 같은 소유자와 레벨로 설정
        if (Owner != null)
            clone.Setup(Owner, level);

        // 복제된 객체 반환
        return clone;
    }
}