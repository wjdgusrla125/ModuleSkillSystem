using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
public abstract class TargetSearchAction : ICloneable
{
    [Header("Indicator")]
    [SerializeField]
    private bool isShowIndicatorPlayerOnly;
    // Indicator를 보여주는 Module
    [SerializeReference, SubclassSelector]
    private IndicatorViewAction indicatorViewAction;

    // Range에 Scale을 적용할지 여부
    [Header("Option")]
    [SerializeField]
    private bool isUseScale;

    // Range에 적용되어 Range 값을 조절할 때 사용되는 변수
    // Skill의 Charge 정도에 따라 검색 범위가 달라질게 할 때 활용할 수 있음
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

    public TargetSearchAction() { }

    public TargetSearchAction(TargetSearchAction copy)
    { 
        indicatorViewAction = copy.indicatorViewAction?.Clone() as IndicatorViewAction;
        isUseScale = copy.isUseScale;
    }

    // selectResult를 기반으로 Target을 찾는 함수, 검색 결과를 즉각 반환함
    public abstract TargetSearchResult Search(TargetSearcher targetSearcher, Entity requesterEntity,
        GameObject requesterObject, TargetSelectionResult selectResult);

    public abstract object Clone();

    public virtual void ShowIndicator(TargetSearcher targetSearcher, GameObject requesterObject, float fillAmount)
    {
        var entity = requesterObject.GetComponent<Entity>();
        if (isShowIndicatorPlayerOnly && (entity == null || !entity.IsPlayer))
            return;

        indicatorViewAction?.ShowIndicator(targetSearcher, requesterObject, Range, Angle, fillAmount);
    }

    public virtual void HideIndicator() => indicatorViewAction?.HideIndicator();

    public string BuildDescription(string description, string prefixKeyword) 
        => TextReplacer.Replace(description, prefixKeyword + ".searchAction", GetStringsByKeyword());

    protected virtual IReadOnlyDictionary<string, string> GetStringsByKeyword() => null;

    // Scale 값이 수정되었을 때의 처리를 하는 함수
    protected virtual void OnScaleChanged(float newScale) { }
}
