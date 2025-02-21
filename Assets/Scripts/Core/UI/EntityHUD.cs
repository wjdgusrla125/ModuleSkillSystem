using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EntityHUD : MonoSingleton<EntityHUD>
{
    [SerializeField]
    private TextMeshProUGUI nameText;

    [Header("Stat View")]
    [SerializeField]
    private Image hpFillImage;
    [SerializeField]
    private TextMeshProUGUI hpValueText;
    [SerializeField]
    private Image skillCostFillImage;
    [SerializeField]
    private TextMeshProUGUI skillCostValueText;

    [Header("Effecf List View")]
    [SerializeField]
    private SkillEffectListView effectListView;

    private Entity target;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy() => ReleaseEvents();

    public void Show(Entity target)
    {
        ReleaseEvents();

        this.target = target;
        target.onDead += OnEntityDead;

        nameText.text = target.name;

        var stats = target.Stats;
        stats.HPStat.onValueChanged += OnHPStatChanged;
        stats.SkillCostStat.onValueChanged += OnSkillCostStatChanged;
        
        UpdateStatView(stats.HPStat, hpFillImage, hpValueText);
        UpdateStatView(stats.SkillCostStat, skillCostFillImage, skillCostValueText);

        effectListView.Target = target.SkillSystem;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        ReleaseEvents();

        target = null;
        effectListView.Target = null;

        gameObject.SetActive(false);
    }

    private void UpdateStatView(Stat stat, Image statFillAmount, TextMeshProUGUI statText)
    {
        statFillAmount.fillAmount = stat.Value / stat.MaxValue;
        statText.text = $"{Mathf.RoundToInt(stat.Value)} / {stat.MaxValue}";
    }

    private void ReleaseEvents()
    {
        if (!target)
            return;

        target.onDead -= OnEntityDead;
        target.Stats.HPStat.onValueChanged -= OnHPStatChanged;
        target.Stats.SkillCostStat.onValueChanged -= OnSkillCostStatChanged;
    }

    private void OnHPStatChanged(Stat stat, float currentValue, float prevValue)
        => UpdateStatView(stat, hpFillImage, hpValueText);

    private void OnSkillCostStatChanged(Stat stat, float currentValue, float prevValue)
        => UpdateStatView(stat, skillCostFillImage, skillCostValueText);

    private void OnEntityDead(Entity entity) => Hide();
}
