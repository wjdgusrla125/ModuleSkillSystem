using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SearchResultMessage
{
    // 기준점을 찾지 못함
    Fail,
    // 기준점이 존재하지만 검색 범위 밖에 있음
    OutOfRange,
    // 기준 Target을 찾음
    FindTarget,
    // 기준 Position을 찾음
    FindPosition,
}
    
public readonly struct TargetSelectionResult
{
    // 목표 대상 (ex. 적 캐릭터, 내 캐릭터, 아군 캐릭터 등)
    public readonly GameObject selectedTarget;
    // 목표 대상의 좌표 혹은 목표 좌표
    public readonly Vector3 selectedPosition;
    public readonly SearchResultMessage resultMessage;

    public TargetSelectionResult(GameObject selectedTarget, SearchResultMessage resultMessage)
        => (this.selectedTarget, selectedPosition, this.resultMessage) = (selectedTarget, selectedTarget.transform.position, resultMessage);

    public TargetSelectionResult(Vector3 selectedPosition, SearchResultMessage resultMessage)
        => (selectedTarget, this.selectedPosition, this.resultMessage) = (null, selectedPosition, resultMessage);
}