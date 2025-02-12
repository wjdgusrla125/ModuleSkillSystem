using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class TargetSearcher
{
    public delegate void SelectionCompletedHandler(TargetSearcher targetSearcher, TargetSelectionResult result);

    // Target 검색 기준점을 찾는 Module
    [Header("Select Action")]
    [SerializeReference, SubclassSelector]
    private TargetSelectionAction selectionAction;

    // 위 Module을 통해 찾은 기준점을 토대로 Teget을 찾는 Module 
    [Header("Search Action")]
    [SerializeReference, SubclassSelector]
    private TargetSearchAction searchAction;

    private SelectionCompletedHandler onSelectionCompleted;

    // 검색 범위를 조절하기 위한 변수
    // Skill의 Charge 정도에 따라 검색 범위를 동적을 조절하고 싶을 때 활용할 수 있음
    private float scale = 1f;

    public float Scale
    {
        get => scale;
        set
        {
            scale = Mathf.Clamp01(value);
            selectionAction.Scale = scale;
            searchAction.Scale = scale;
        }
    }

    public object SelectionRange => selectionAction.Range;
    public object SelectionScaledRange => selectionAction.ScaledRange;
    public object SelectionProperRange => selectionAction.ProperRange;
    public float SelectionAngle => selectionAction.Angle;

    public object SearchRange => searchAction.Range;
    public object SearchScaledRange => searchAction.ScaledRange;
    public object SearchProperRange => searchAction.ProperRange;
    public float SearchAngle => searchAction.Angle;

    public bool IsSearching { get; private set; }

    public TargetSelectionResult SelectionResult { get; private set; }

    public TargetSearchResult SearchResult { get; private set; }

    public TargetSearcher() { }
    
    public TargetSearcher(TargetSearcher copy)
    {
        selectionAction = copy.selectionAction?.Clone() as TargetSelectionAction;
        searchAction = copy.searchAction?.Clone() as TargetSearchAction;
        Scale = copy.scale;
    }

    public void SelectTarget(Entity requesterEntity, GameObject requesterObject, SelectionCompletedHandler onSelectionCompleted)
    {
        CancelSelect();

        IsSearching = true;
        this.onSelectionCompleted = onSelectionCompleted;

        selectionAction.Select(this, requesterEntity, requesterObject, OnSelectCompleted);
    }

    public TargetSelectionResult SelectImmediate(Entity requesterEntity, GameObject requesterObject, Vector3 position)
    {
        CancelSelect();

        SelectionResult = selectionAction.SelectImmeidiate(this, requesterEntity, requesterObject, position);
        return SelectionResult;
    }

    public void CancelSelect()
    {
        if (!IsSearching) return;

        IsSearching = false;
        selectionAction.CancelSelect(this);
    }

    public TargetSearchResult SearchTargets(Entity requesterEntity, GameObject requesterObject)
    {
        SearchResult = searchAction.Search(this, requesterEntity, requesterObject, SelectionResult);
        
        return SearchResult;
    }

    public void ShowIndicator(GameObject requesterObject)
    {
        HideIndicator();

        selectionAction.ShowIndicator(this, requesterObject, scale);
        searchAction.ShowIndicator(this, requesterObject, scale);
    }

    public void HideIndicator()
    {
        selectionAction.HideIndicator();
        searchAction.HideIndicator();
    }

    public bool IsInRange(Entity requsterEntity, GameObject requesterObject, Vector3 targetPosition)
        => selectionAction.IsInRange(this, requsterEntity, requesterObject, targetPosition);

    public string BuildDescription(string description, string prefixKeyword = "")
    {
        prefixKeyword += string.IsNullOrEmpty(prefixKeyword) ? "targetSearcher" : ".targetSearcher";
        description = selectionAction.BuildDescription(description, prefixKeyword);
        description = searchAction.BuildDescription(description, prefixKeyword);
        return description;
    }
    
    private void OnSelectCompleted(TargetSelectionResult selectResult)
    {
        IsSearching = false;
        SelectionResult = selectResult;
        onSelectionCompleted.Invoke(this, selectResult);
    }
}
