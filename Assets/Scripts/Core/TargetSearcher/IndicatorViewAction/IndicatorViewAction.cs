using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public abstract class IndicatorViewAction : ICloneable
{
    public abstract void ShowIndicator(TargetSearcher targetSearcher, GameObject requesterObject,
        object range, float angle, float fillAmount);
    public abstract void HideIndicator();
    
    // 생성된 Indicator의 FillAmount를 수정할 때 사용
    public abstract void SetFillAmount(float fillAmount);
    public abstract object Clone();
}