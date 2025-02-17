using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Skill : IdentifiedObject
{
    private const int kInfinity = 0;
    
    public delegate void LevelChangedHandler(Skill skill, int currentLevel, int prevLevel);
    public delegate void StateChangedHandler(Skill skill, State<Skill> newState, State<Skill> prevState, int layer);
    public delegate void AppliedHander(Skill skill, int currentApplyCount);
    public delegate void UsedHandler(Skill skill);
    // Skill이 사용(Use)된 직후 실행되는 Event
    public delegate void ActivatedHandler(Skill skill);
    // Skill이 종료된 직후 실행되는 Event
    public delegate void DeactivatedHandler(Skill skill);
    public delegate void CanceledHandler(Skill skill);
    public delegate void TargetSelectionCompletedHandler(Skill skill, TargetSearcher targetSearcher, TargetSelectionResult result);
    public delegate void CurrentApplyCountChangedHandler(Skill skill, int currentApplyCount, int prevApplyCount);
    
    [SerializeField] private SkillType type;
    [SerializeField] private SkillUseType useType;

    [SerializeField] private SkillExecutionType executionType;
    [SerializeField] private SkillApplyType applyType;

    [SerializeField] private NeedSelectionResultType needSelectionResultType;
    [SerializeField] private TargetSelectionTimingOption targetSelectionTimingOption;
    [SerializeField] private TargetSearchTimingOption targetSearchTimingOption;

    [SerializeReference, SubclassSelector]
    private EntityCondition[] acquisitionConditions;
    [SerializeReference, SubclassSelector]
    private Cost[] acquisitionCosts;

    // Skill을 사용하기 위한 조건들
    [SerializeReference, SubclassSelector]
    private SkillCondition[] useConditions;

    [SerializeField] private bool isAllowLevelExceedDatas;
    [SerializeField] private int maxLevel;
    [SerializeField, Min(1)] 
    private int defaultLevel = 1;
    [SerializeField] private SkillData[] skillDatas;

    private SkillData currentData;

    private int level;

    private int currentApplyCount;
    private float currentCastTime;
    private float currentCooldown;
    private float currentDuration;
    private float currentChargePower;
    private float currentChargeDuration;

    private readonly Dictionary<SkillCustomActionType, CustomAction[]> customActionsByType = new();

    public Entity Owner { get; private set; }
    public StateMachine<Skill> StateMachine { get; private set; }

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

    public int MaxLevel => maxLevel;
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
    public int DataBonusLevel => Mathf.Max(level - currentData.level, 0);
    public bool IsMaxLevel => level == maxLevel;
    // Skill이 최대 Level이 아니고, Level Up 조건을 만족하고, Level Up을 위한 Costs가 충분하다면 True
    public bool IsCanLevelUp => !IsMaxLevel && LevelUpConditions.All(x => x.IsPass(Owner)) &&
        LevelUpCosts.All(x => x.HasEnoughCost(Owner));

    private SkillPrecedingAction PrecedingAction => currentData.precedingAction;
    private SkillAction Action => currentData.action;
    public bool HasPrecedingAction => PrecedingAction != null;

    public InSkillActionFinishOption InSkillActionFinishOption => currentData.inSkillActionFinishOption;
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

    public TargetSearcher TargetSearcher => currentData.targetSearcher;
    public bool IsSearchingTarget => TargetSearcher.IsSearching;
    public TargetSelectionResult TargetSelectionResult => TargetSearcher.SelectionResult;
    public TargetSearchResult TargetSearchResult => TargetSearcher.SearchResult;
    // Skill이 필요로 하는 기준점 Type과 TargetSearcher가 검색한 기준점의 Type이 일치하는가?
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
    // Skill이 기준점 검색중이 아니고, 검색한 기준점이 Skill이 필요로 하는 Type이라면 True 
    public bool IsTargetSelectSuccessful => !IsSearchingTarget && HasValidTargetSelectionResult;

    public IReadOnlyList<Cost> Costs => currentData.costs;
    public bool HasCost => Costs.Count > 0;
    public bool HasEnoughCost => Costs.All(x => x.HasEnoughCost(Owner));

    public float Cooldown => currentData.cooldown.GetValue(Owner.Stats);
    public bool HasCooldown => Cooldown > 0f;
    public float CurrentCooldown
    {
        get => currentCooldown;
        set => currentCooldown = Mathf.Clamp(value, 0f, Cooldown);
    }
    public bool IsCooldownCompleted => Mathf.Approximately(0f, CurrentCooldown);

    public float Duration => currentData.duration;
    private bool IsTimeless => Mathf.Approximately(Duration, kInfinity);
    public float CurrentDuration
    {
        get => currentDuration;
        set => currentDuration = !IsTimeless ? Mathf.Clamp(value, 0f, Duration) : value;
    }

    public SkillRunningFinishOption RunningFinishIption => currentData.runningFinishOption;
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
    
    // currentData의 applyCycle이 0이고 applyCount가 1보다 크면(여러번 적용 가능하면)
    // Skill의 duration을 (ApplyCount - 1)로 나눠서 ApplyCycle을 계산하여 return 함.
    // 아니라면 설정된 currentData의 applyCycle을 그대로 return 함.
    public float ApplyCycle => Mathf.Approximately(currentData.applyCycle, 0f) && ApplyCount > 1 ?
        Duration / (ApplyCount - 1) : currentData.applyCycle;
    public float CurrentApplyCycle { get; set; }

    public bool IsUseCast => currentData.isUseCast;
    public float CastTime => currentData.castTime.GetValue(Owner.Stats);
    public float CurrentCastTime
    {
        get => currentCastTime;
        set => currentCastTime = Mathf.Clamp(value, 0f, CastTime);
    }
    public bool IsCastCompleted => Mathf.Approximately(CastTime, CurrentCastTime);

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

            TargetSearcher.Scale = currentChargePower;

            foreach (var effect in Effects)
                effect.Scale = currentChargePower;
        }
    }
    
    // 충전의 지속 시간
    public float ChargeDuration => currentData.chargeDuration;
    
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

    public bool IsPassive => type == SkillType.Passive;
    public bool IsToggleType => useType == SkillUseType.Toggle;
    public bool IsActivated { get; private set; }
    public bool IsReady => StateMachine.IsInState<ReadyState>();
    // 발동 횟수가 남았고, ApplyCycle만큼 시간이 지났으면 true를 return
    public bool IsApplicable => (CurrentApplyCount < ApplyCount || IsInfinitelyApplicable) &&
    (CurrentApplyCycle >= ApplyCycle);
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

    public IReadOnlyList<Entity> Targets { get; private set; }
    public IReadOnlyList<Vector3> TargetPositions { get; private set; }

    private bool IsDurationEnded => !IsTimeless && Mathf.Approximately(Duration, CurrentDuration);
    private bool IsApplyCompleted => !IsInfinitelyApplicable && CurrentApplyCount == ApplyCount;
    // Skill의 발동이 종료되었는가?
    public bool IsFinished => currentData.runningFinishOption == SkillRunningFinishOption.FinishWhenDurationEnded ?
        IsDurationEnded : IsApplyCompleted;

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

    public event LevelChangedHandler onLevelChanged;
    public event StateChangedHandler onStateChanged;
    public event AppliedHander onApplied;
    public event ActivatedHandler onActivated;
    public event DeactivatedHandler onDeactivated;
    public event UsedHandler onUsed;
    public event CanceledHandler onCanceled;
    public event TargetSelectionCompletedHandler onTargetSelectionCompleted;
    public event CurrentApplyCountChangedHandler onCurrentApplyCountChanged;

    public void OnDestroy()
    {
        foreach (var effect in Effects)
            Destroy(effect);
    }

    public void Setup(Entity owner, int level)
    {
        Debug.Assert(owner != null, $"Skill::Setup - Owner는 Null이 될 수 없습니다.");
        Debug.Assert(level >= 1 && level <= maxLevel, $"Skill::Setup - {level}이 1보다 작거나 {maxLevel}보다 큽니다.");
        Debug.Assert(Owner == null, $"Skill::Setup - 이미 Setup하였습니다.");

        Owner = owner;
        Level = level;

        SetupStateMachine();
    }

    public void Setup(Entity owner)
        => Setup(owner, defaultLevel);

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

    public void ResetProperties()
    {
        CurrentCastTime = 0f;
        CurrentCooldown = 0f;
        CurrentDuration = 0f;
        CurrentApplyCycle = 0f;
        CurrentChargeDuration = 0f;
        CurrentApplyCount = 0;
    }

    public void Update() => StateMachine.Update();

    private void UpdateCustomActions()
    {
        customActionsByType[SkillCustomActionType.Cast] = currentData.customActionsOnCast;
        customActionsByType[SkillCustomActionType.Charge] = currentData.customActionsOnCharge;
        customActionsByType[SkillCustomActionType.PrecedingAction] = currentData.customActionsOnPrecedingAction;
        customActionsByType[SkillCustomActionType.Action] = currentData.customActionsOnAction;
    }

    private void UpdateCurrentEffectLevels()
    {
        int bonusLevel = DataBonusLevel;
        foreach (var effect in Effects)
            effect.Level = Mathf.Min(effect.Level + bonusLevel, effect.MaxLevel);
    }

    private void ChangeData(SkillData newData)
    {
        foreach (var effect in Effects)
            Destroy(effect);

        currentData = newData;

        Effects = currentData.effectSelectors.Select(x => x.CreateEffect(this)).ToArray();
        // Skill의 현재 Level이 data의 Level보다 크면, 둘의 Level 차를 Effect의 Bonus Level 줌.
        // 만약 Skill이 2 Level이고, data가 1 level이라면, effect들은 2-1해서 1의 Bonus Level을 받게 됨.
        if (level > currentData.level)
            UpdateCurrentEffectLevels();

        UpdateCustomActions();
    }

    public void LevelUp()
    {
        Debug.Assert(IsCanLevelUp, "Skill::LevelUP - Level Up 조건을 충족하지 못했습니다.");

        foreach (var cost in LevelUpCosts)
            cost.UseCost(Owner);

        Level++;
    }

    public bool HasEnoughAcquisitionCost(Entity entity)
        => acquisitionCosts.All(x => x.HasEnoughCost(entity));

    public bool IsAcquirable(Entity entity)
        => acquisitionConditions.All(x => x.IsPass(entity)) && HasEnoughAcquisitionCost(entity);

    public void UseAcquisitionCost(Entity entity)
    {
        foreach (var cost in acquisitionCosts)
            cost.UseCost(entity);
    }

    public void ShowIndicator()
        => TargetSearcher.ShowIndicator(Owner.gameObject);

    public void HideIndicator()
        => TargetSearcher.HideIndicator();

    public void SelectTarget(Action<Skill, TargetSearcher, TargetSelectionResult> onSelectCompletedOrNull, bool isShowIndicator = true)
    {
        CancelSelectTarget();

        if (isShowIndicator)
            ShowIndicator();

        TargetSearcher.SelectTarget(Owner, Owner.gameObject, (targetSearcher, result) =>
        {
            if (isShowIndicator)
                HideIndicator();

            // Skill이 필요로 하는 Type의 기준점 검색에 성공했고,
            // SearchTiming이 기준점 검색 직후라면(TargetSelectionCompleted) Target 검색 실행
            if (IsTargetSelectSuccessful && targetSearchTimingOption == TargetSearchTimingOption.TargetSelectionCompleted)
                SearchTargets();

            onSelectCompletedOrNull?.Invoke(this, targetSearcher, result);
            onTargetSelectionCompleted?.Invoke(this, targetSearcher, result);
        });
    }

    public void SelectTarget(bool isShowIndicator = true) => SelectTarget(null, isShowIndicator);

    public void CancelSelectTarget(bool isHideIndicator = true)
    {
        if (!TargetSearcher.IsSearching)
            return;

        TargetSearcher.CancelSelect();

        if (isHideIndicator)
            HideIndicator();
    }

    public void SearchTargets()
    {
        var result = TargetSearcher.SearchTargets(Owner, Owner.gameObject);
        Targets = result.targets.Select(x => x.GetComponent<Entity>()).ToArray();
        TargetPositions = result.positions;
    }

    public TargetSelectionResult SelectTargetImmediate(Vector3 position)
    {
        CancelSelectTarget();

        var result = TargetSearcher.SelectImmediate(Owner, Owner.gameObject, position);
        if (IsTargetSelectSuccessful && targetSearchTimingOption == TargetSearchTimingOption.TargetSelectionCompleted)
            SearchTargets();

        return result;
    }

    public bool IsInRange(Vector3 position)
        => TargetSearcher.IsInRange(Owner, Owner.gameObject, position);

    public bool Use()
    {
        Debug.Assert(IsUseable, "Skill::Use - 사용 조건을 만족하지 못했습니다.");

        bool isUsed = StateMachine.ExecuteCommand(SkillExecuteCommand.Use) || StateMachine.SendMessage(SkillStateMessage.Use);
        if (isUsed)
            onUsed?.Invoke(this);

        return isUsed;
    }

    public bool UseImmediately(Vector3 position)
    {
        Debug.Assert(IsUseable, "Skill::UseImmediately - 사용 조건을 만족하지 못했습니다.");

        SelectTargetImmediate(position);

        bool isUsed = StateMachine.ExecuteCommand(SkillExecuteCommand.UseImmediately) || StateMachine.SendMessage(SkillStateMessage.Use);
        if (isUsed)
            onUsed?.Invoke(this);

        return isUsed;
    }

    public bool Cancel(bool isForce = false)
    {
        Debug.Assert(!IsPassive, "Skill::Cancel - Passive Skill은 Cancel 할 수 없습니다.");

        var isCanceled = isForce ? StateMachine.ExecuteCommand(SkillExecuteCommand.CancelImmediately) :
            StateMachine.ExecuteCommand(SkillExecuteCommand.Cancel);

        if (isCanceled)
            onCanceled?.Invoke(this);

        return isCanceled;
    }

    public void UseCost()
    {
        Debug.Assert(HasEnoughCost, "Skill::UseCost - 사용할 Cost가 부족합니다.");

        foreach (var cost in Costs)
            cost.UseCost(Owner);
    }

    public void UseDeltaCost()
    {
        Debug.Assert(HasEnoughCost, "Skill::UseDeltaCost - 사용할 Cost가 부족합니다.");

        foreach (var cost in Costs)
            cost.UseDeltaCost(Owner);
    }

    public void Activate()
    {
        Debug.Assert(!IsActivated, "Skill::Activate - 이미 활성화되어 있습니다.");

        UseCost();

        IsActivated = true;
        onActivated?.Invoke(this);
    }

    public void Deactivate()
    {
        Debug.Assert(IsActivated, "Skill::Activate - Skill이 활성화되어있지 않습니다.");

        IsActivated = false;
        onDeactivated?.Invoke(this); 
    }

    public void StartCustomActions(SkillCustomActionType type)
    {
        foreach (var customAction in customActionsByType[type])
            customAction.Start(this);
    }

    public void RunCustomActions(SkillCustomActionType type)
    {
        foreach (var customAction in customActionsByType[type])
            customAction.Run(this);
    }

    public void ReleaseCustomActions(SkillCustomActionType type)
    {
        foreach (var customAction in customActionsByType[type])
            customAction.Release(this);
    }

    public void StartPrecedingAction()
    {
        StartCustomActions(SkillCustomActionType.PrecedingAction);
        PrecedingAction.Start(this);
    }

    public bool RunPrecedingAction()
    {
        RunCustomActions(SkillCustomActionType.PrecedingAction);
        return PrecedingAction.Run(this);
    }

    public void ReleasePrecedingAction()
    {

        ReleaseCustomActions(SkillCustomActionType.PrecedingAction);
        PrecedingAction.Release(this);
    }

    public void StartAction()
    {
        StartCustomActions(SkillCustomActionType.Action);
        Action.Start(this); 
    }

    public void ReleaseAction()
    {
        ReleaseCustomActions(SkillCustomActionType.Action);
        Action.Release(this);
    }

    public void Apply(bool isConsumeApplyCount = true)
    {
        Debug.Assert(IsInfinitelyApplicable || !isConsumeApplyCount || (CurrentApplyCount < ApplyCount),
            $"Skill({CodeName})의 최대 적용 횟수({ApplyCount})를 초과해서 적용할 수 없습니다.");

        if (targetSearchTimingOption == TargetSearchTimingOption.Apply)
            SearchTargets();

        RunCustomActions(SkillCustomActionType.Action);

        Action.Apply(this);

        // Auto일 때는 Duration과의 오차 값을 남기기 위해 ApplyCycle로 나눈 나머지로 값을 설정함
        // Ex. Duration = 1.001, CurrentApplyCycle = 1.001
        //     => Duration = 1.001, CurrentApplyCycle = 0.001
        if (executionType == SkillExecutionType.Auto)
            CurrentApplyCycle %= ApplyCycle;
        else
            CurrentApplyCycle = 0f;

        if (isConsumeApplyCount)
            CurrentApplyCount++;

        onApplied?.Invoke(this, CurrentApplyCount);
    }

    public bool IsInState<T>() where T : State<Skill> => StateMachine.IsInState<T>();
    public bool IsInState<T>(int layer) where T : State<Skill> => StateMachine.IsInState<T>(layer);

    public Type GetCurrentStateType(int layer = 0) => StateMachine.GetCurrentStateType(layer);

    public bool IsTargetSelectionTiming(TargetSelectionTimingOption option)
        => targetSelectionTimingOption == TargetSelectionTimingOption.Both || targetSelectionTimingOption == option;

    public override object Clone()
    {
        var clone = Instantiate(this);

        if (Owner != null)
            clone.Setup(Owner, level);

        return clone;
    }
}