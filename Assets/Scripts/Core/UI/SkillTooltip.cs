using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class SkillTooltip : MonoSingleton<SkillTooltip>
{
    [SerializeField]
    private TextMeshProUGUI acquisitionText;
    [SerializeField]
    private TextMeshProUGUI displayNameText;
    [SerializeField]
    private TextMeshProUGUI skillTypeText;
    [SerializeField]
    private TextMeshProUGUI costText;
    [SerializeField]
    private TextMeshProUGUI cooldownText;
    [SerializeField]
    private TextMeshProUGUI descriptionText;

    private StringBuilder stringBuilder = new();

    private void Start() => gameObject.SetActive(false);

    private void Update() => transform.position = Input.mousePosition;

    public void Show(Skill skill)
    {
        acquisitionText.gameObject.SetActive(false);

        displayNameText.text = skill.DisplayName;
        var skillTypeName = skill.Type == SkillType.Active ? "액티브" : "패시브";
        skillTypeText.text = $"[{skillTypeName}]";
        costText.text = BuildCostText(skill);
        cooldownText.text = $"재사용 대기 시간: {skill.Cooldown:0.##}초";
        descriptionText.text = skill.Description;

        transform.position = Input.mousePosition;

        float xPivot = transform.localPosition.x > 0f ? 1f : 0f;
        float yPivot = transform.localPosition.y > 0f ? 1f : 0f;
        GetComponent<RectTransform>().pivot = new(xPivot, yPivot);

        gameObject.SetActive(true);
    }

    public void Show(SkillTreeSlotView slotView)
    {
        if (slotView.RequesterOwnedSkill)
        {
            Show(slotView.RequesterOwnedSkill);

            var skill = slotView.RequesterOwnedSkill;
            if (!skill.IsMaxLevel)
                TryShowConditionText(skill.Owner, "레벨 업 조건", skill.LevelUpConditions, skill.LevelUpCosts);
        }
        else
        {
            var entity = slotView.Requester;
            var slotSkill = slotView.SlotSkill;
            var temporarySkill = slotSkill.Clone() as Skill;
            temporarySkill.Setup(entity);

            Show(temporarySkill);
            Destroy(temporarySkill);

            TryShowConditionText(entity, "습득 조건", slotSkill.AcquisitionConditions, slotSkill.AcquisitionCosts);
        }
    }

    public void Hide() => gameObject.SetActive(false);

    private void TryShowConditionText(Entity entity, string prefixText,
        IReadOnlyList<EntityCondition> conditions, IReadOnlyList<Cost> costs)
    {
        var isShowable = conditions.Count > 0 || costs.Count > 0;

        if (!isShowable)
            return;

        acquisitionText.gameObject.SetActive(true);
        acquisitionText.text = BuildConditionText(entity, prefixText, conditions, costs);
    }

    private string BuildCostText(Skill skill)
    {
        stringBuilder.Append("비용: ");

        if (skill.IsToggleType)
            stringBuilder.Append("초당 ");

        int costLength = skill.Costs.Count;
        for (int i = 0; i < costLength; i++)
        {
            var cost = skill.Costs[i];
            stringBuilder.Append(cost.Description);
            stringBuilder.Append(' ');
            stringBuilder.Append(cost.GetValue(skill.Owner));

            if (i != costLength - 1)
                stringBuilder.Append(", ");
        }

        var result = stringBuilder.ToString();
        stringBuilder.Clear();

        return result;
    }

    private string BuildConditionText(Entity entity, string prefixText,
    IReadOnlyList<EntityCondition> conditions, IReadOnlyList<Cost> costs)
    {
        stringBuilder.Append(prefixText);
        stringBuilder.Append(": ");

        for (int i = 0; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            stringBuilder.Append("<color=");
            stringBuilder.Append(condition.IsPass(entity) ? "white>" : "red>");
            stringBuilder.Append(condition.Description);
            stringBuilder.Append("</color>");

            if (i != conditions.Count - 1)
                stringBuilder.Append(", ");
        }

        for (int i = 0; i < costs.Count; i++)
        {
            var cost = costs[i];

            stringBuilder.Append(", ");
            stringBuilder.Append("<color=");
            stringBuilder.Append(cost.HasEnoughCost(entity) ? "white>" : "red>");
            stringBuilder.Append(costs[i].Description);
            stringBuilder.Append(' ');
            stringBuilder.Append(costs[i].GetValue(entity));
            stringBuilder.Append("</color>");
        }

        string result = stringBuilder.ToString();
        stringBuilder.Clear();

        return result;
    }
}
