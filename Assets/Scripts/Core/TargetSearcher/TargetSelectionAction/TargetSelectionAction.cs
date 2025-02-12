using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class TargetSelectionAction : ICloneable
{
    public delegate void SelectCompletedHandler(TargetSelectionResult result);

    // Indicator를 보여주는 Module
    [Header("Indicator")]
    [SerializeReference, SubclassSelector]
    private IndicatorViewAction indicatorViewAction;

    // Range에 Scale을 적용할지 여부
    [Header("Option")]
    [SerializeField]
    private bool isUseScale;

    // Range에 적용되어 Range 값을 조절할 때 사용되는 변수
    // Skill의 Charge 정도에 따라 검색 범위가 달라w게 할 때 활용할 수 있음
    private float scale;

    public float Scale
    {
        get => scale;
        set
        {
            if (scale == value)
                return;

            scale = value;
            // scale의 Indicator의 FillAmount로 적용
            indicatorViewAction?.SetFillAmount(scale);
            // scale 값 수정에 따른 처리를 함
            OnScaleChanged(scale);
        }
    }

    // 탐색 범위
    // 탐색 범위는 단순히 거리를 나타내는 float형이 될 수도, 공간을 나타내는 Rect나 Vector형이 될 수도 있으므로 object type
    public abstract object Range { get; }
    // 위 Range에 Scale이 적용된 값
    public abstract object ScaledRange { get; }
    // 탐색 각도
    public abstract float Angle { get; }
    // isUseScale 값에 따라, 일반 Range 혹은 ScaledReange를 반환해줌.
    public object ProperRange => isUseScale ? ScaledRange : Range;

    public bool IsUseScale => isUseScale;

    public TargetSelectionAction() { }

    public TargetSelectionAction(TargetSelectionAction copy)
    {
        indicatorViewAction = copy.indicatorViewAction?.Clone() as IndicatorViewAction;
        isUseScale = copy.isUseScale;
    }

    // Player가 검색을 요청했을 때 즉시 기준점을 찾는 함수
    protected abstract TargetSelectionResult SelectImmediateByPlayer(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, Vector3 position);
    // AI가 검색을 요청했을 때 즉시 기준점을 찾는 함수
    protected abstract TargetSelectionResult SelectImmediateByAI(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, Vector3 position);

    // Entity가 Player인지, AI인지에 따라서 위 두 함수를 중 적합한 함수를 실행해줌
    public TargetSelectionResult SelectImmeidiate(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, Vector3 position)
        => requesterEntity.IsPlayer ? SelectImmediateByPlayer(targetSearcher, requesterEntity, requesterObject, position) :
        SelectImmediateByAI(targetSearcher, requesterEntity, requesterObject, position);

    // 비동기적으로 기준점을 찾는 함수
    // 기준점을 찾았거나, 검색에 실패했을 시 onSelectCompleted callback 함수로 결과를 return해줌.
    public abstract void Select(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, SelectCompletedHandler onSelectCompleted);

    // 위의 Select 함수로 기준점을 비동기 검색 중일 때, 검색을 취소함
    public abstract void CancelSelect(TargetSearcher targetSearcher);

    // 인자로 받은 좌표가 기준점 검색 범위 안에 있는지 확인하는 함수
    public abstract bool IsInRange(TargetSearcher targetSearcher, Entity requesterEntity, GameObject requesterObject, Vector3 targetPosition);

    public abstract object Clone();

    public virtual void ShowIndicator(TargetSearcher targetSearcher, GameObject requesterObject, float fillAmount)
        => indicatorViewAction?.ShowIndicator(targetSearcher, requesterObject, Range, Angle, fillAmount);

    public virtual void HideIndicator() => indicatorViewAction?.HideIndicator();
    
    // prefixKeyword.selectionAction.keyword
    // ex. targetSearcher.selectionAction.range
    public string BuildDescription(string description, string prefixKeyword)
        => TextReplacer.Replace(description, prefixKeyword + ".selectionAction", GetStringsByKeyword());

    protected virtual IReadOnlyDictionary<string, string> GetStringsByKeyword() => null;

    // Scale 값이 수정되었을 때의 처리를 하는 함수
    protected virtual void OnScaleChanged(float newScale) { }
}
