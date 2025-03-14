using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Entity 컴포넌트가 반드시 필요함을 명시
[RequireComponent(typeof(Entity))]
public class SkillSystem : MonoBehaviour
{
    #region Event
    // 스킬 이벤트 delegate 정의 - 스킬 상태 변경과 관련된 이벤트 처리를 위한 함수 포인터
    public delegate void SkillRegisteredHandler(SkillSystem skillSystem, Skill skill);         // 스킬 등록 시 호출
    public delegate void SkillUnregisteredHandler(SkillSystem skillSystem, Skill skill);       // 스킬 해제 시 호출
    public delegate void SkillStateChangedHandler(SkillSystem skillSystem, Skill skill,
        State<Skill> newState, State<Skill> prevState, int layer);                           // 스킬 상태 변경 시 호출
    public delegate void SkillActivatedHandler(SkillSystem skillSystem, Skill skill);          // 스킬 활성화 시 호출
    public delegate void SkillDeactivatedHandler(SkillSystem skillSystem, Skill skill);        // 스킬 비활성화 시 호출
    public delegate void SkillAppliedHandler(SkillSystem skillSystem, Skill skill, int currentApplyCount); // 스킬 적용 시 호출
    public delegate void SkillUsedHandler(SkillSystem skillSystem, Skill skill);               // 스킬 사용 시 호출
    public delegate void SkillCanceledHandler(SkillSystem skillSystem, Skill skill);           // 스킬 취소 시 호출
    public delegate void SkillTargetSelectionCompleted(SkillSystem skillSystem, Skill skill,
        TargetSearcher targetSearcher, TargetSelectionResult result);                         // 스킬 타겟 선택 완료 시 호출

    // 효과 이벤트 delegate 정의 - 효과 상태 변경과 관련된 이벤트 처리
    public delegate void EffectStartedHandler(SkillSystem skillSystem, Effect effect);         // 효과 시작 시 호출
    public delegate void EffectAppliedHandler(SkillSystem skillSystem, Effect effect, int currentApplyCount, int prevApplyCount); // 효과 적용 시 호출
    public delegate void EffectReleasedHandler(SkillSystem skillSystem, Effect effect);        // 효과 해제 시 호출
    public delegate void EffectStackChangedHandler(SkillSystem skillSystem, Effect effect, int currentStack, int prevStack); // 효과 스택 변경 시 호출
    #endregion

    // Unity Inspector에서 설정 가능한 필드
    [SerializeField]
    private SkillTree defaultSkillTree;   // 기본 스킬 트리

    [SerializeField]
    private Skill[] defaultSkills;        // 기본으로 보유할 스킬 목록

    // 내부 관리 필드
    private List<Skill> ownSkills = new();    // 보유 중인 스킬 목록
    private Skill reservedSkill;              // 예약된 스킬 (실행 대기 상태)

    private List<Skill> runningSkills = new(); // 현재 실행 중인 스킬 목록

    private List<Effect> runningEffects = new();      // 현재 실행 중인 효과 목록
    private Queue<Effect> destroyEffectQueue = new(); // 제거 예정인 효과 큐

    private List<SkillTreeSlotNode> autoAcquisitionSlots = new(); // 자동 획득 대기 중인 스킬 슬롯 노드

    // 속성 정의
    public Entity Owner { get; private set; }                     // 스킬 시스템의 소유자(Entity)
    public IReadOnlyList<Skill> OwnSkills => ownSkills;           // 읽기 전용 보유 스킬 목록
    public IReadOnlyList<Skill> RunningSkills => runningSkills;   // 읽기 전용 실행 중인 스킬 목록
    public IReadOnlyList<Effect> RunningEffects => runningEffects.Where(x => !x.IsReleased).ToArray(); // 읽기 전용 실행 중인 효과 목록 (해제되지 않은 것만)
    public SkillTree DefaultSkillTree => defaultSkillTree;        // 기본 스킬 트리

    // 이벤트 정의 - 외부에서 구독 가능한 이벤트
    public event SkillRegisteredHandler onSkillRegistered;              // 스킬 등록 이벤트
    public event SkillUnregisteredHandler onSkillUnregistered;          // 스킬 해제 이벤트
    public event SkillStateChangedHandler onSkillStateChanged;          // 스킬 상태 변경 이벤트
    public event SkillActivatedHandler onSkillActivated;                // 스킬 활성화 이벤트
    public event SkillDeactivatedHandler onSkillDeactivated;            // 스킬 비활성화 이벤트
    public event SkillUsedHandler onSkillUsed;                          // 스킬 사용 이벤트
    public event SkillAppliedHandler onSkillApplied;                    // 스킬 적용 이벤트
    public event SkillCanceledHandler onSkillCanceled;                  // 스킬 취소 이벤트
    public event SkillTargetSelectionCompleted onSkillTargetSelectionCompleted; // 스킬 타겟 선택 완료 이벤트

    public event EffectStartedHandler onEffectStarted;                  // 효과 시작 이벤트
    public event EffectAppliedHandler onEffectApplied;                  // 효과 적용 이벤트
    public event EffectReleasedHandler onEffectReleased;                // 효과 해제 이벤트
    public event EffectStackChangedHandler onEffectStackChanged;        // 효과 스택 변경 이벤트

    // 컴포넌트 파괴 시 호출 - 메모리 정리
    private void OnDestroy()
    {
        // 모든 보유 스킬 제거
        foreach (var skill in ownSkills)
            Destroy(skill);

        // 모든 실행 중인 효과 제거
        foreach (var effect in runningEffects)
            Destroy(effect);
    }

    // 매 프레임 호출되는 업데이트 함수
    private void Update()
    {
        UpdateSkills();             // 모든 보유 스킬 업데이트
        UpdateRunningEffects();     // 실행 중인 효과 업데이트
        DestroyReleasedEffects();   // 해제된 효과 제거
        UpdateReservedSkill();      // 예약된 스킬 처리
        TryAcquireSkills();         // 자동 획득 스킬 확인 및 획득
    }

    // 스킬 시스템 초기화 함수
    public void Setup(Entity entity)
    {
        Owner = entity;  // 소유자 설정
        Debug.Assert(Owner != null, "SkillSystem::Awake - Owner는 null이 될 수 없습니다.");
        SetupSkills();   // 스킬 초기화
    }

    // 기본 스킬 및 스킬트리 초기화
    private void SetupSkills()
    {
        // 기본 스킬 등록
        foreach (var skill in defaultSkills)
            RegisterWithoutCost(skill);

        // 스킬 트리가 없으면 종료
        if (!defaultSkillTree)
            return;

        // 자동 습득 스킬 처리
        foreach (var skillSlotNode in defaultSkillTree.GetSlotNodes())
        {
            // 자동 습득 설정이 아니면 스킵
            if (!skillSlotNode.IsSkillAutoAcquire)
                continue;

            // 습득 가능하면 즉시 습득, 아니면 자동 습득 목록에 추가
            if (skillSlotNode.IsSkillAcquirable(Owner))
                skillSlotNode.AcquireSkill(Owner);
            else
                autoAcquisitionSlots.Add(skillSlotNode);
        }
    }

    // 비용 없이 스킬 등록 (복제본 생성)
    public Skill RegisterWithoutCost(Skill skill, int level = 0)
    {
        // 이미 존재하는 스킬인지 확인
        Debug.Assert(!ownSkills.Exists(x => x.ID == skill.ID), "SkillSystem::Register - 이미 존재하는 Skill입니다.");

        // 스킬 복제 및 초기화
        var clone = skill.Clone() as Skill;
        if (level > 0)
            clone.Setup(Owner, level);  // 레벨 지정 시
        else
            clone.Setup(Owner);         // 기본 레벨

        // 이벤트 핸들러 등록
        clone.onStateChanged += OnSkillStateChanged;
        clone.onActivated += OnSkillActivated;
        clone.onDeactivated += OnSkillDeactivated;
        clone.onApplied += OnSkillApplied;
        clone.onUsed += OnSkillUsed;
        clone.onCanceled += OnSkillCanceled;
        clone.onTargetSelectionCompleted += OnSkillTargetSelectionCompleted;

        // 스킬 목록에 추가
        ownSkills.Add(clone);

        // 스킬 등록 이벤트 발생
        onSkillRegistered?.Invoke(this, clone);

        return clone;
    }

    // 비용을 소모하여 스킬 등록
    public Skill Register(Skill skill, int level = 0)
    {
        // 이미 존재하는 스킬인지 확인
        Debug.Assert(!ownSkills.Exists(x => x.ID == skill.ID), "SkillSystem::Register - 이미 존재하는 Skill입니다.");
        // 습득 비용이 충분한지 확인
        Debug.Assert(skill.HasEnoughAcquisitionCost(Owner), "SkillSystem::Register - 습득을 위한 Cost가 부족합니다.");

        // 습득 비용 사용
        skill.UseAcquisitionCost(Owner);
        // 비용 없이 등록하는 메서드 활용
        skill = RegisterWithoutCost(skill, level);

        return skill;
    }

    // 스킬 해제
    public bool Unregister(Skill skill)
    {
        // 스킬 찾기
        skill = Find(skill);
        if (skill == null)
            return false;

        // 스킬 취소 및 목록에서 제거
        skill.Cancel();
        ownSkills.Remove(skill);

        // 스킬 해제 이벤트 발생
        onSkillUnregistered?.Invoke(this, skill);

        // 스킬 객체 파괴
        Destroy(skill);

        return true;
    }

    // 모든 보유 스킬 업데이트
    private void UpdateSkills()
    {
        foreach (var skill in ownSkills)
            skill.Update();
    }

    // 실행 중인 효과 업데이트
    private void UpdateRunningEffects()
    {
        for (int i = 0; i < runningEffects.Count; i++)
        {
            var effect = runningEffects[i];
            // 이미 해제된 효과는 스킵
            if (effect.IsReleased)
                continue;

            // 효과 업데이트
            effect.Update();

            // 효과가 완료되면 제거
            if (effect.IsFinished)
                RemoveEffect(effect);
        }
    }

    // 해제된 효과 파괴 (메모리 정리)
    private void DestroyReleasedEffects()
    {
        while (destroyEffectQueue.Count > 0)
        {
            var effect = destroyEffectQueue.Dequeue();
            runningEffects.Remove(effect);
            Destroy(effect);
        }
    }

    // 예약된 스킬 업데이트 - 타겟이 범위 내에 들어오면 사용
    private void UpdateReservedSkill()
    {
        // 예약된 스킬이 없으면 종료
        if (!reservedSkill)
            return;

        // 타겟 위치 계산 (선택된 타겟 또는 선택된 위치)
        var selectionResult = reservedSkill.TargetSelectionResult;
        var targetPosition = selectionResult.selectedTarget?.transform.position ?? selectionResult.selectedPosition;
        
        // 대상이 범위 내에 있고 사용 가능하면 즉시 사용
        if (reservedSkill.IsInRange(targetPosition))
        {
            if (reservedSkill.IsUseable)
                reservedSkill.UseImmediately(targetPosition);
            reservedSkill = null;  // 예약 해제
        }
    }

    // 자동 획득 스킬 확인 및 획득 시도
    private void TryAcquireSkills()
    {
        // 자동 획득 대기 슬롯이 없으면 종료
        if (autoAcquisitionSlots.Count == 0)
            return;

        // 뒤에서부터 확인 (제거 시 인덱스 문제 방지)
        for (int i = autoAcquisitionSlots.Count - 1; i >= 0; i--)
        {
            var node = autoAcquisitionSlots[i];
            // 스킬 획득 가능하면 획득 후 목록에서 제거
            if (node.IsSkillAcquirable(Owner))
            {
                node.AcquireSkill(Owner);
                autoAcquisitionSlots.RemoveAt(i);
            }
        }
    }

    // 스킬 예약 - 사용 조건이 충족되면 사용됨
    public void ReserveSkill(Skill skill) => reservedSkill = skill;
    
    // 예약된 스킬을 취소하는 메서드
    public void CancelReservedSkill() => reservedSkill = null;

    // 새로운 효과를 적용하는 내부 메서드
    private void ApplyNewEffect(Effect effect)
    {
        // 효과 복제 및 대상 설정
        var newEffect = effect.Clone() as Effect;
        newEffect.SetTarget(Owner);
        
        // 효과 이벤트 핸들러 등록
        newEffect.onStarted += OnEffectStarted;
        newEffect.onApplied += OnEffectApplied;
        newEffect.onReleased += OnEffectReleased;
        newEffect.onStackChanged += OnEffectStackChanged;

        // 효과 시작
        newEffect.Start();
        
        // 적용 가능한 상태면 적용
        if (newEffect.IsApplicable)
            newEffect.Apply();

        // 효과가 이미 완료된 상태면 즉시 해제 및 파괴
        if (newEffect.IsFinished)
        {
            newEffect.Release();
            Destroy(newEffect);
        }
        else
            // 아직 진행 중인 효과는 목록에 추가
            runningEffects.Add(newEffect);
    }

    // 효과를 적용하는 메서드
    public void Apply(Effect effect)
    {
        // 이미 실행 중인 동일한 효과 확인
        var runningEffect = Find(effect);
        
        // 실행 중인 효과가 없거나 중복 허용이면 새로 적용
        if (runningEffect == null || effect.IsAllowDuplicate)
            ApplyNewEffect(effect);
        else
        {
            // 스택 가능한 효과면 스택 증가
            if (runningEffect.MaxStack > 1)
                runningEffect.CurrentStack++;
            // 중복 시 이전 효과 제거 설정이면 이전 효과 제거 후 새로 적용
            else if (runningEffect.RemoveDuplicateTargetOption == EffectRemoveDuplicateTargetOption.Old)
            {
                RemoveEffect(runningEffect);
                ApplyNewEffect(effect);
            }
        }
    }

    // 여러 효과를 한번에 적용하는 메서드
    public void Apply(IReadOnlyList<Effect> effects)
    {
        // 모든 효과를 순차적으로 적용
        foreach (var effect in effects)
            Apply(effect);
    }

    // 스킬의 모든 효과를 적용하는 메서드
    public void Apply(Skill skill)
    {
        // 스킬이 가진 모든 효과 적용
        Apply(skill.Effects);
    }

    // 스킬 사용 메서드
    public bool Use(Skill skill)
    {
        // 등록된 스킬인지 확인
        skill = Find(skill);

        // 스킬이 등록되어 있는지 검증
        Debug.Assert(skill != null,
            $"SkillSystem::IncreaseStack({skill.CodeName}) - Skill이 System에 등록되지 않았습니다.");

        // 스킬 사용 요청
        return skill.Use();
    }

    // 스킬 취소 메서드
    public bool Cancel(Skill skill)
    {
        // 실행 중인 스킬 목록에서 ID로 찾기
        skill = runningSkills.FirstOrDefault(x => x.ID == skill.ID);
        // 스킬 취소 요청
        return skill.Cancel();
    }

    // 모든 스킬 취소 메서드
    public void CancelAll(bool isForce = false)
    {
        // 타겟 탐색 중인 스킬 취소
        CancelTargetSearching();

        // 모든 실행 중인 스킬 순차적으로 취소
        foreach (var skill in runningSkills.ToArray())
            skill.Cancel();
    }

    // ID로 스킬 찾기 메서드
    public Skill Find(Skill skill)
        // 이미 현재 소유자의 스킬이면 그대로 반환, 아니면 ID로 찾기
        => skill.Owner == Owner ? skill : ownSkills.Find(x => x.ID == skill.ID);

    // 조건에 맞는 스킬 찾기 메서드
    public Skill Find(System.Predicate<Skill> match)
        // 조건에 맞는 첫 번째 스킬 반환
        => ownSkills.Find(match);

    // ID로 효과 찾기 메서드
    public Effect Find(Effect effect)
        // 이미 현재 대상의 효과면 그대로 반환, 아니면 ID로 찾기
        => effect.Target == Owner ? effect : runningEffects.Find(x => x.ID == effect.ID);

    // 조건에 맞는 효과 찾기 메서드
    public Effect Find(System.Predicate<Effect> match)
        // 조건에 맞는 첫 번째 효과 반환
        => runningEffects.Find(match);

    // 조건에 맞는 모든 스킬 찾기 메서드
    public List<Skill> FindAll(System.Predicate<Skill> match)
        // 조건에 맞는 모든 스킬 목록 반환
        => ownSkills.FindAll(match);

    // 조건에 맞는 모든 효과 찾기 메서드
    public List<Effect> FindAll(System.Predicate<Effect> match)
        // 조건에 맞는 모든 효과 목록 반환
        => runningEffects.FindAll(match);

    // 스킬 보유 여부 확인 메서드
    public bool Contains(Skill skill)
        // 스킬 검색 결과가 있으면 true 반환
        => Find(skill) != null;

    // 효과 적용 여부 확인 메서드
    public bool Contains(Effect effect)
        // 효과 검색 결과가 있으면 true 반환
        => Find(effect) != null;

    // 효과 제거 메서드
    public bool RemoveEffect(Effect effect)
    {
        // 효과 찾기
        effect = Find(effect);

        // 효과가 없거나 이미 제거 대기 상태면 false 반환
        if (effect == null || destroyEffectQueue.Contains(effect))
            return false;

        // 효과 해제 처리
        effect.Release();

        // 제거 대기열에 추가
        destroyEffectQueue.Enqueue(effect);

        return true;
    }

    // 조건에 맞는 효과 제거 메서드
    public bool RemoveEffect(System.Predicate<Effect> predicate)
    {
        // 조건에 맞는 효과 찾기
        var target = runningEffects.Find(predicate);
        // 찾은 효과 제거
        return target != null && RemoveEffect(target);
    }

    // 특정 카테고리 효과 제거 메서드
    public bool RemoveEffect(Category category)
        // 카테고리에 속한 효과 제거
        => RemoveEffect(x => x.HasCategory(category));

    // 모든 효과 제거 메서드
    public void RemoveEffectAll()
    {
        // 모든 효과를 제거 대기열에 추가
        foreach (var target in runningEffects)
            RemoveEffect(target);
    }

    // 조건에 맞는 모든 효과 제거 메서드
    public void RemoveEffectAll(System.Func<Effect, bool> predicate)
    {
        // 조건에 맞는 모든 효과 찾기
        var targets = runningEffects.Where(predicate);
        // 찾은 모든 효과 제거
        foreach (var target in targets)
            RemoveEffect(target);
    }

    // 특정 효과 ID와 동일한 모든 효과 제거 메서드
    public void RemoveEffectAll(Effect effect) => RemoveEffectAll(x => x.ID == effect.ID);

    // 특정 카테고리의 모든 효과 제거 메서드
    public void RemoveEffectAll(Category category) => RemoveEffectAll(x => x.HasCategory(category));

    // 타겟 탐색 중인 스킬 취소 메서드
    public void CancelTargetSearching()
        // SearchingTargetState 상태의 스킬을 찾아 취소
        => ownSkills.Find(x => x.IsInState<SearchingTargetState>())?.Cancel();

    // 현재 실행 중인 스킬 적용 메서드
    private void ApplyCurrentRunningSkill()
    {
        // 소유자가 스킬 액션 상태일 때
        if (Owner.StateMachine.GetCurrentState() is InSkillActionState ownerState)
        {
            // 실행 중인 스킬 가져오기
            var runnsingSkill = ownerState.RunningSkill;
            // 스킬 적용 (입력 기반 실행이 아닌 경우에만 적용)
            runnsingSkill.Apply(runnsingSkill.ExecutionType != SkillExecutionType.Input);
        }
    }

    #region Event Callbacks
    // 스킬 상태 변경 이벤트 처리 메서드
    private void OnSkillStateChanged(Skill skill, State<Skill> newState, State<Skill> prevState, int layer)
        // 외부 이벤트 발생
        => onSkillStateChanged?.Invoke(this, skill, newState, prevState, layer);

    // 스킬 활성화 이벤트 처리 메서드
    private void OnSkillActivated(Skill skill)
    {
        // 실행 중인 스킬 목록에 추가
        runningSkills.Add(skill);

        // 외부 이벤트 발생
        onSkillActivated?.Invoke(this, skill);
    }

    // 스킬 비활성화 이벤트 처리 메서드
    private void OnSkillDeactivated(Skill skill)
    {
        // 실행 중인 스킬 목록에서 제거
        runningSkills.Remove(skill);

        // 외부 이벤트 발생
        onSkillDeactivated?.Invoke(this, skill);
    }

    // 스킬 사용 이벤트 처리 메서드
    private void OnSkillUsed(Skill skill) => onSkillUsed?.Invoke(this, skill);

    // 스킬 취소 이벤트 처리 메서드
    private void OnSkillCanceled(Skill skill) => onSkillCanceled?.Invoke(this, skill);

    // 스킬 적용 이벤트 처리 메서드
    private void OnSkillApplied(Skill skill, int currentApplyCount)
        => onSkillApplied?.Invoke(this, skill, currentApplyCount);

    // 스킬 타겟 선택 완료 이벤트 처리 메서드
    private void OnSkillTargetSelectionCompleted(Skill skill, TargetSearcher targetSearcher, TargetSelectionResult result)
    {
        // 타겟이나 위치를 찾았으면 예약된 스킬 해제
        if (result.resultMessage == SearchResultMessage.FindTarget || result.resultMessage == SearchResultMessage.FindPosition)
            reservedSkill = null;

        // 외부 이벤트 발생
        onSkillTargetSelectionCompleted?.Invoke(this, skill, targetSearcher, result);
    }

    // 효과 시작 이벤트 처리 메서드
    private void OnEffectStarted(Effect effect) => onEffectStarted?.Invoke(this, effect);

    // 효과 적용 이벤트 처리 메서드
    private void OnEffectApplied(Effect effect, int currentApplyCount, int prevApplyCount)
        => onEffectApplied?.Invoke(this, effect, currentApplyCount, prevApplyCount);

    // 효과 해제 이벤트 처리 메서드
    private void OnEffectReleased(Effect effect) => onEffectReleased?.Invoke(this, effect);

    // 효과 스택 변경 이벤트 처리 메서드
    private void OnEffectStackChanged(Effect effect, int currentStack, int prevStack)
        => onEffectStackChanged?.Invoke(this, effect, currentStack, prevStack);
    #endregion
}
