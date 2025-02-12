using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Effect : IdentifiedObject
{
    // 몇몇 변수는 값이 0이면 무한을 의미함
    private const int kInfinity = 0;
    
    public delegate void StartedHandler(Effect effect);
    public delegate void AppliedHandler(Effect effect, int currentApplyCount, int prevApplyCount);
    public delegate void ReleasedHandler(Effect effect);
    public delegate void StackChangedHandler(Effect effect, int currentApplyCount, int prevApplyCount);
    
    [SerializeField]
    private EffectType type;
    // Effect의 중복 적용 가능 여부
    [SerializeField]
    private bool isAllowDuplicate = true;
    [SerializeField]
    private EffectRemoveDuplicateTargetOption removeDuplicateTargetOption;

    // UI로 Effect 정보를 보여줄지에 대한 여부
    [SerializeField]
    private bool isShowInUI;

    // maxLevel이 effectDatas의 Length를 초과할 수 있는지 여부
    // 이 Option이 false면 maxLevel은 effectDatas의 Length로 고정됨
    [SerializeField]
    private bool isAllowLevelExceedDatas;
    [SerializeField]
    private int maxLevel;
    // Level별 Data, Level은 1부터 시작하고 Array의 Index는 0부터 시작하므로
    // Level에 맞는 Data를 가져오려면 [현재 Level - 1]번째 Data를 가져와야함
    // ex. Level이 1이라면, 1 - 1 = 0, 0번째 Data를 가져와야함(= effectDatas[0])
    [SerializeField]
    private EffectData[] effectDatas;

    // Level에 맞는 현재 Data
    private EffectData currentData;

    // 현재 Effect Level
    private int level;
    // 현재 쌓인 Stack
    private int currentStack = 1;
    private float currentDuration;
    private int currentApplyCount;
    private float currentApplyCycle;
    // Action의 Apply 함수를 실행하려 시도한 적이 있는지 여부, 이 값에 따라 Apply 성공시에 currentApplyCycle 변수의 값을 다르게 초기화 함.
    // Action의 Apply 함수가 실행될 때 true가되고, Apply 함수가 true를 return하면 false로 초기화 됨.
    private bool isApplyTried;

    // 쌓인 Stack에 따라 현재 적용된 Stack Actions
    private readonly List<EffectStackAction> aplliedStackActions = new();

    
    public EffectType Type => type;
    public bool IsAllowDuplicate => isAllowDuplicate;
    public EffectRemoveDuplicateTargetOption RemoveDuplicateTargetOption => removeDuplicateTargetOption;

    public bool IsShowInUI => isShowInUI;

    public IReadOnlyList<EffectData> EffectDatas => effectDatas;
    public IReadOnlyList<EffectStackAction> StackActions => currentData.stackActions;

    
    public int MaxLevel => maxLevel;
    public int Level
    {
        get => level;
        set
        {
            Debug.Assert(value > 0 && value <= MaxLevel, $"Effect.Rank = {value} - value는 0보다 크고 MaxLevel보다 같거나 작아야합니다.");

            if (level == value)
                return;

            level = value;

            // 현재 Effect Level보다 작으면서 가장 가까운 Level인 Data를 찾아옴
            // 예를 들어, Data가 Level 1, 3, 5 이렇게 있을 때, Effect의 Level이 4일 경우,
            // Level 3의 Data를 찾아옴
            var newData = effectDatas.Last(x => x.level <= level);
            if (newData.level != currentData.level)
                currentData = newData;
        }
    }
    public bool IsMaxLevel => level == maxLevel;
    // 현재 Effect와 EffectData의 Level 차이
    // Action 쪽에서 Bonus Value를 주는데 활용할 수 있음
    // ex. totalValue = defaultValue + (effect.DataBonusLevel * bonusValuePerLevel)
    // Level이 1000까지 있는 Clicker Game의 경우 Data를 1000개 만들지 않아도
    // 위와 같이 BonusLevel을 활용해 Level 당 수치를 조절할 수 있음.
    public int DataBonusLevel => Mathf.Max(level - currentData.level, 0);
    
    // Effect의 지속 시간
    public float Duration => currentData.duration.GetValue(User.Stats);
    // Duration이 0이면 무한 지속
    public bool IsTimeless => Mathf.Approximately(Duration, kInfinity);
    public float CurrentDuration
    {
        get => currentDuration;
        set => currentDuration = Mathf.Clamp(value, 0f, Duration);
    }
    public float RemainDuration => Mathf.Max(0f, Duration - currentDuration);
    
    public int MaxStack => currentData.maxStack;
    public int CurrentStack
    {
        get => currentStack;
        set
        {
            var prevStack = currentStack;
            currentStack = Mathf.Clamp(value, 1, MaxStack);

            // Stack이 쌓이면 currentDuration을 초기화하여 Effect의 지속 시간을 늘려줌
            if (currentStack >= prevStack)
                currentDuration = 0f;

            if (currentStack != prevStack)
            {
                // Action에 쌓인 Stack 수가 바뀌었다고 알려줘서, Stack에 따른 수치를 Update 할 수 있게함
                Action?.OnEffectStackChanged(this, User, Target, level, currentStack, Scale);

                // 바뀐 Stack에 따라 기존에 적용된 Stack 효과를 Release하고, 현재 Stack에 맞는 새로운 Stack 효과들을 Apply함
                TryApplyStackActions();

                // Stack 수가 바뀌었음을 Event를 통해 외부에 알려줌
                onStackChanged?.Invoke(this, currentStack, prevStack);
            }
        }
    }
    
    public int ApplyCount => currentData.applyCount;
    // ApplyCount가 0이면 무한 적용(= 매 프레임마다 적용)
    public bool IsInfinitelyApplicable => ApplyCount == kInfinity;
    public int CurrentApplyCount
    {
        get => currentApplyCount;
        set => currentApplyCount = IsInfinitelyApplicable ? value : Mathf.Clamp(value, 0, ApplyCount);
    }
    // ApplyCycle이 0이고 ApplyCount가 1보다 크면 Effect의 지속시간인 Duration을 나눠서 ApplyCycle을 계산함
    // 예를들어 Duration이 10초고 ApplyCount가 11번이면, 처음 Effect가 적용될 때 Apply가 1번 이뤄져서
    // 남은 ApplyCount = 10, Duration / ApplyCount = 10 / 10 = 1, ApplyCycle = 1초
    public float ApplyCycle => Mathf.Approximately(currentData.applyCycle, 0f) && ApplyCount > 1 ?
        (Duration / (ApplyCount - 1)) : currentData.applyCycle;
    // ApplyCycle을 확인하기 위한 시간 변수.
    // CurrentDuration을 이용해서 확인하지 않고 CurrentApplyCycle을 따로 만든 이유는
    // CurrentDuration은 Effecf의 Stack이 쌓이면 0으로 초기화되기 때문.
    // 예를 들어, ApplyCycle이 1초이고 CurrentDuration이 0.9999초일 때,
    // 원래는 다음 Frame에 CurrentDuration이 1초가 되면서 Effect가 Apply되야하는데,
    // Stack이 쌓여서 CurrentDuration이 0초로 초기화되버리면, 1초를 다시 기다려야 Apply이 되는 상황이 옴.
    // 그래서 따로 Apply 시점 확인하는 CurrentApplyCycle이 있으면 CurrentDuration이 중간에 0이되도
    // CurrentApplyCycle은 계속 시간이 쌓이고 있으니 제때 Apply될 수 있음.
    public float CurrentApplyCycle
    {
        get => currentApplyCycle;
        set => currentApplyCycle = Mathf.Clamp(value, 0f, ApplyCycle);
    }
    
    private EffectAction Action => currentData.action;

    private CustomAction[] CustomActions => currentData.customActions;

    public object Owner { get; private set; }
    public Entity User { get; private set; }
    public Entity Target { get; private set; }
    // Scale 조절을 통해 Effect의 위력을 조절할 수 있음
    // Charge처럼 Casting 시간에 따라 위력이 달라지는 Skill에 활용할 수 있음
    public float Scale { get; set; }
    public override string Description => BuildDescription(base.Description, 0);
    
    private bool IsApplyAllWhenDurationExpires => currentData.isApplyAllWhenDurationExpires;
    private bool IsDurationEnded => !IsTimeless && Mathf.Approximately(Duration, CurrentDuration);
    private bool IsApplyCompleted => !IsInfinitelyApplicable && CurrentApplyCount == ApplyCount;
    // Effect의 완료 여부
    // 지속 시간이 끝났거나, RunningFinishOption이 ApplyCompleted일 때, Apply 횟수가 최대 횟수라면 True
    public bool IsFinished => IsDurationEnded ||
        (currentData.runningFinishOption == EffectRunningFinishOption.FinishWhenApplyCompleted && IsApplyCompleted);
    // Effect의 Release 함수가 실행되면(= Effect가 종료되면) True가 됨
    // IsFinished Property가 Effect가 온전히 완료되어야만 True인반면, IsReleased는 무언가에 의해 Effect가 제거되어도 True됨.
    // 완료 여부와 상관없이 순수히 Effect가 종료되었는지 확인하기 위한 Property
    public bool IsReleased { get; private set; }

    // Effect를 적용할 수 있는가?
    public bool IsApplicable => Action != null &&
        (CurrentApplyCount < ApplyCount || ApplyCount == kInfinity) &&
        CurrentApplyCycle >= ApplyCycle;

    public event StartedHandler onStarted;
    public event AppliedHandler onApplied;
    public event ReleasedHandler onReleased;
    public event StackChangedHandler onStackChanged;
    
    public void Setup(object owner, Entity user, int level, float scale = 1f)
    {
        Owner = owner;
        User  = user;
        Level = level;
        CurrentApplyCycle = ApplyCycle;
        Scale = scale;
    }

    public void SetTarget(Entity target) => Target = target;

    // 현재 적용된 모든 StackAction들을 Release함
    private void ReleaseStackActionsAll()
    {
        aplliedStackActions.ForEach(x => x.Release(this, level, User, Target, Scale));
        aplliedStackActions.Clear();
    }

    // 현재 적용된 StackAction들에서 조건에 맞는 StackAction들을 찾아 Release함
    private void ReleaseStackActions(System.Func<EffectStackAction, bool> predicate)
    {
        var stackActions = aplliedStackActions.Where(predicate).ToList();
        foreach (var stackAction in stackActions)
        {
            stackAction.Release(this, level, User, Target, Scale);
            aplliedStackActions.Remove(stackAction);
        }
    }

    // 현재 적용된 StackAction들 중 더 이상 조건에 맞지 않는 StackAction들은 Release하고,
    // 새롭게 조건에 맞는 StackAction들을 적용하는 함수
    private void TryApplyStackActions()
    {
        // 적용된 StackAction들 중 현재 Stack보다 더 큰 Stack을 요구하는 StackAction들을 Release함.
        // 어떤 이유에 의해 Stack 수가 떨어졌을 때를 위한 처리.
        ReleaseStackActions(x => x.Stack > currentStack);

        // 적용 가능한 StackAction 목록
        // StackAction들 중에서 필요한 Stack 수가 충족되고, 현재 적용중이지 않고, 적용 조건을 만족하는 StackAction들을 찾아옴
        var stackActions = StackActions.Where(x => x.Stack <= currentStack && !aplliedStackActions.Contains(x) && x.IsApplicable);

        // 현재 적용된 StackAction들과 찾아온 StackAction들 중 가장 높은 Stack 값을 찾아옴
        int aplliedStackHighestStack = aplliedStackActions.Any() ? aplliedStackActions.Max(x => x.Stack) : 0;
        int stackActionsHighestStack = stackActions.Any() ? stackActions.Max(x => x.Stack) : 0;
        var highestStack = Mathf.Max(aplliedStackHighestStack, stackActionsHighestStack);
        if (highestStack > 0)
        {
            // 찾아온 StackAction들 중 Stack이 highestStack 보다 낮고, IsReleaseOnNextApply가 true인 StackAction들을 찾아옴
            var except = stackActions.Where(x => x.Stack < highestStack && x.IsReleaseOnNextApply);
            // 바로 위에서 찾아온 stackAction들을 stackActions 목록에서 제외함
            // => IsReleaseOnNextApply가 true인 StackAction은 더 높은 Stack을 가진 StackAction이 존재한다면
            //    Release되야하므로 애초에 적용 대상 목록에서 제거함
            stackActions = stackActions.Except(except);
        }

        if (stackActions.Any())
        {
            // 적용된 StackAction들 중에서 IsReleaseOnNextApply가 true인 StackAction들을 Release함
            // 단, 필요 Stack이 현재 Stack과 동일한 StackAction들은 제외.
            // 왜냐하면 예를 들어, Stack 수가 5 필요한 StackAction이 적용 중이고, 현재 Effect Stack이 6이였다가 5로 떨어졌을 경우
            // 적용 중인 StackAction의 필요 Stack 수 5와 현재 Effect Stack 수 5가 일치하므로 가만 놔두면 되는데,
            // x.Stack < currentStack라는 조건이 없으면, 현재 Effect Stack 수와 일치하는 StackAction들까지 Release 대상으로 포함됨
            ReleaseStackActions(x => x.Stack < currentStack && x.IsReleaseOnNextApply);

            foreach (var stackAction in stackActions)
                stackAction.Apply(this, level, User, Target, Scale);

            aplliedStackActions.AddRange(stackActions);
        }
    }
    
    public void Start()
    {
        Debug.Assert(!IsReleased, "Effect::Start - 이미 종료된 Effect입니다.");

        Action?.Start(this, User, Target, Level, Scale);

        TryApplyStackActions();

        foreach (var customAction in CustomActions)
            customAction.Start(this);

        onStarted?.Invoke(this);
    }

    public void Update()
    {
        CurrentDuration += Time.deltaTime;
        currentApplyCycle += Time.deltaTime;

        if (IsApplicable)
            Apply();

        if (IsApplyAllWhenDurationExpires && IsDurationEnded && !IsInfinitelyApplicable)
        {
            for (int i = currentApplyCount; i < ApplyCount; i++)
                Apply();
        }
    }

    public void Apply()
    {
        Debug.Assert(!IsReleased, "Effect::Apply - 이미 종료된 Effect입니다.");

        if (Action == null)
            return;

        if (Action.Apply(this, User, Target, level, currentStack, Scale))
        {
            foreach (var customAction in CustomActions)
                customAction.Run(this);

            var prevApplyCount = CurrentApplyCount++;

            if (isApplyTried)
                currentApplyCycle = 0f;
            else
                currentApplyCycle %= ApplyCycle;

            isApplyTried = false;

            onApplied?.Invoke(this, CurrentApplyCount, prevApplyCount);
        }
        else
            isApplyTried = true;
    }

    public void Release()
    {
        Debug.Assert(!IsReleased, "Effect::Release - 이미 종료된 Effect입니다.");

        Action?.Release(this, User, Target, level, Scale);
        ReleaseStackActionsAll();

        foreach (var customAction in CustomActions)
            customAction.Release(this);

        IsReleased = true;

        onReleased?.Invoke(this);
    }
    
    public EffectData GetData(int level) => effectDatas[level - 1];

    public string BuildDescription(string description, int effectIndex)
    {
        Dictionary<string, string> stringsByKeyword = new Dictionary<string, string>()
        {
            { "duration", Duration.ToString("0.##") },
            { "applyCount", ApplyCount.ToString() },
            { "applyCycle", ApplyCycle.ToString("0.##") }
        };

        description = TextReplacer.Replace(description, stringsByKeyword, effectIndex.ToString());

        description = Action.BuildDescription(this, description, 0, 0, effectIndex);

        var stackGroups = StackActions.GroupBy(x => x.Stack);
        foreach (var stackGroup in stackGroups)
        {
            int i = 0;
            foreach (var stackAction in stackGroup)
                description = stackAction.BuildDescription(this, description, i++, effectIndex);
        }

        return description;
    }

    public override object Clone()
    {
        var clone = Instantiate(this);

        if (Owner != null)
            clone.Setup(Owner, User, Level, Scale);

        return clone;
    }
}