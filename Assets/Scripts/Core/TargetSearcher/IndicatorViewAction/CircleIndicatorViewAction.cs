using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class CircleIndicatorViewAction : IndicatorViewAction
{
    [SerializeField]
    private GameObject indicatorPrefab;
    
    // 생성한 indicator의 radius 값
    // 만약 0이면 targetSearcher의 range를 대신 사용함
    [SerializeField]
    private float indicatorRadiusOverride;
    
    // 생성한 indicator의 angle 값
    // 만약 0이면 targetSearcher의 angle을 대신 사용함
    [SerializeField]
    private float indicatorAngleOverride;
    
    // Indicator의 속을 채우는 fillAmount Property를 사용할 것인가?
    [SerializeField]
    private bool isUseIndicatorFillAmount;
    
    // Indicator를 requsterObject의 자식 Object로 만들 것인가?
    [SerializeField]
    private bool isAttachIndicatorToRequester;

    // ShowIndicator 함수로 생성한 Indicator
    private Indicator spawnedRangeIndicator;

    public override void ShowIndicator(TargetSearcher targetSearcher, GameObject requesterObject,
        object range, float angle, float fillAmount)
    {
        Debug.Assert(range is float, "CircleIndicatorViewAction::ShowIndicator - range는 null 또는 float형만 허용됩니다.");

        // 이미 Indicator를 보여주고 있다면 먼저 Hide 처리를 해줌
        HideIndicator();

        // isUseIndicatorFillAmount Option이 true가 아니면 fillAmount 값으로 0을 씀
        fillAmount = isUseIndicatorFillAmount ? fillAmount : 0f;
        // isAttachIndicatorToRequester Option이 true라면 requesterObject의 transform을 가져옴
        var attachTarget = isAttachIndicatorToRequester ? requesterObject.transform : null;
        // indicatorRadiusOverride가 0이라면 인자로 받은 targetSearcher의 range를,
        // 아니라면 indicatorRadiusOverride를 Indicator의 radius로 씀
        float radius = Mathf.Approximately(indicatorRadiusOverride, 0f) ? (float)range : indicatorRadiusOverride;
        // indicatorAngleOverride가 0이라면 인자로 받은 targetSearcher의 angle을,
        // 아니라면 indicatorAngleOverride를 Indicator의 angle로 씀
        angle = Mathf.Approximately(indicatorAngleOverride, 0f) ? angle : indicatorAngleOverride;

        // Indicator를 생성하고, Setup 함수로 위에서 정한 값들을 Setting해줌
        spawnedRangeIndicator = GameObject.Instantiate(indicatorPrefab).GetComponent<Indicator>();
        spawnedRangeIndicator.Setup(angle, radius, fillAmount, attachTarget);
    }

    public override void HideIndicator()
    {
        if (!spawnedRangeIndicator)
            return;

        GameObject.Destroy(spawnedRangeIndicator.gameObject);
    }

    public override void SetFillAmount(float fillAmount)
    {
        if (!isUseIndicatorFillAmount || spawnedRangeIndicator == null)
            return;

        spawnedRangeIndicator.FillAmount = fillAmount;
    }

    public override object Clone()
    {
        return new CircleIndicatorViewAction()
        {
            indicatorPrefab = indicatorPrefab,
            indicatorAngleOverride = indicatorAngleOverride,
            indicatorRadiusOverride = indicatorRadiusOverride,
            isUseIndicatorFillAmount = isUseIndicatorFillAmount,
            isAttachIndicatorToRequester = isAttachIndicatorToRequester
        };
    }
}
