using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnSkillObjectAction : SkillAction
{
    [SerializeField]
    private GameObject skillObjectPrefab;

    [Header("Target Searcher")]
    [SerializeField]
    private TargetSearcher targetSearcherForSkillObject;

    [Header("Data")]
    [SerializeField]
    private float duration;
    [SerializeField]
    private int applyCount;
    [SerializeField]
    private Vector3 objectScale = Vector3.one;
    
    public override void Apply(Skill skill)
    {
        foreach (var targetPosition in skill.TargetPositions)
        {
            var skillObject = GameObject.Instantiate(skillObjectPrefab).GetComponent<SkillObject>();
            skillObject.transform.position = targetPosition + (Vector3.up * 0.001f);
            skillObject.Setup(skill, targetSearcherForSkillObject, duration, applyCount, objectScale);
        }
    }

    public override object Clone()
    {
        return new SpawnSkillObjectAction()
        {
            applyCount = applyCount,
            duration = duration,
            objectScale = objectScale,
            skillObjectPrefab = skillObjectPrefab,
            targetSearcherForSkillObject = targetSearcherForSkillObject
        };
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword()
    {
        var applyCycle = skillObjectPrefab.GetComponent<SkillObject>()
            .CalculateApplyCycle(duration, applyCount).ToString("0.##");

        var dictionary = new Dictionary<string, string>()
        {
            { "duration", duration.ToString("0.##") },
            { "applyCount", applyCount.ToString() },
            { "applyCountPerSec", (applyCount / duration).ToString() },
            { "applyCycle", applyCycle }
        };
        return dictionary;
    }

    public override string BuildDescription(string description)
    {
        description = base.BuildDescription(description);
        description = targetSearcherForSkillObject.BuildDescription(description, "skillAction");
        return description;
    }
}
