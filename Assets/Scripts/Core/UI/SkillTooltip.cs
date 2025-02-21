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

    public void Hide() => gameObject.SetActive(false);

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
}
