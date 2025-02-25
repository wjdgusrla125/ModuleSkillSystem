using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SkillTreeSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    #region Events
    public delegate void SkillAcquiredHandler(SkillTreeSlotView slotView, Skill skill);
    #endregion

    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private TextMeshProUGUI levelText;
    [SerializeField]
    private GameObject normalBorder;
    [SerializeField]
    private GameObject acquiredBorder;
    [SerializeField]
    private GameObject blind;

    private Entity requester;
    private Skill requesterSkill;

    public Entity Requester => requester;
    // Requester의 SkillSystem에서 찾아온 SlotSkill => Entity가 소유한 Skill
    public Skill RequesterOwnedSkill => requesterSkill;
    public Skill SlotSkill => SlotNode.Skill;
    public SkillTreeSlotNode SlotNode { get; private set; }

    public event SkillAcquiredHandler onSkillAcquired;

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        if (!requester)
            return;

        requester.SkillSystem.onSkillRegistered -= OnSkillRegistered;
    }

    private void Update()
    {
        if (requesterSkill)
            return;

        bool isAcquirable = SlotNode.IsSkillAcquirable(requester);
        blind.SetActive(!isAcquirable);
    }

    public void SetViewTarget(Entity requester, SkillTreeSlotNode slotNode)
    {
        if (requester)
            requester.SkillSystem.onSkillRegistered -= OnSkillRegistered;

        this.requester = requester;
        SlotNode = slotNode;

        var skill = slotNode.Skill;

        requesterSkill = requester.SkillSystem.Find(skill);
        if (!requesterSkill)
            requester.SkillSystem.onSkillRegistered += OnSkillRegistered;

        iconImage.sprite = skill.Icon;

        UpdateAcquisitionUI();
        UpdateLevelText();
    }

    private void UpdateAcquisitionUI()
    {
        normalBorder.SetActive(!requesterSkill);
        acquiredBorder.SetActive(requesterSkill);
        blind.SetActive(!requesterSkill && !SlotNode.IsSkillAcquirable(requester));
    }

    private void UpdateLevelText()
    {
        int level = requesterSkill ? requesterSkill.Level : 0;
        levelText.color = (requesterSkill && requesterSkill.IsMaxLevel) ? Color.yellow : Color.white;
        levelText.text = $"{level} / {SlotNode.Skill.MaxLevel}";
    }

    private void ShowTooltip()
    {
        SkillTooltip.Instance.Show(this);
    }

    private void HideTooltip()
    {
        SkillTooltip.Instance.Hide();
    }

    private void OnSkillRegistered(SkillSystem skillSystem, Skill skill)
    {
        if (skill.ID != SlotNode.Skill.ID)
            return;

        requesterSkill = skill;

        UpdateAcquisitionUI();
        UpdateLevelText();

        skillSystem.onSkillRegistered -= OnSkillRegistered;

        onSkillAcquired?.Invoke(this, requesterSkill);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        => ShowTooltip();

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        => HideTooltip();

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        // Entity가 skill을 소유했고, Level Up 조건을 달성 했다면 Level Up을 시킴
        if (requesterSkill && requesterSkill.IsCanLevelUp)
        {
            requesterSkill.LevelUp();
            UpdateLevelText();
        }
        // Entity가 Skill을 소유하지 않았고, Skill을 습득할 수 있는 상태라면 습득함
        else if (!requesterSkill && SlotNode.IsSkillAcquirable(requester))
        {
            SlotNode.AcquireSkill(requester);
            SkillTooltip.Instance.Show(requesterSkill);
        }
    }
}
