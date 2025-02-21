using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public class SkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private Image blindImage;
    [SerializeField]
    private Image usingBorderImage;
    [SerializeField]
    private TextMeshProUGUI cooldownText;
    [SerializeField]
    private TextMeshProUGUI remainInputCountText;
    [SerializeField]
    private TextMeshProUGUI keyCodeText;

    private Skill skill;
    private KeyCode useKeyCode;

    public Skill Skill
    {
        get => skill;
        set
        {
            if (skill)
                skill.onStateChanged -= OnSkillStateChanged;

            skill = value;

            if (skill != null)
            {
                skill.onStateChanged += OnSkillStateChanged;

                iconImage.gameObject.SetActive(true);
                iconImage.sprite = skill.Icon;
            }
            else
                SetSkillUIActive(false);
        }
    }

    private void Awake()
    {
        SetSkillUIActive(false);
    }

    private void OnDestroy()
    {
        if (skill)
            skill.onStateChanged -= OnSkillStateChanged;
    }

    private void Update()
    {
        if (!skill)
            return;

        UpdateBlindImage();
        UpdateInput();
    }

    public void Setup(KeyCode useKeyCode)
    {
        this.useKeyCode = useKeyCode;
        keyCodeText.text = ((int)useKeyCode - 48).ToString();
    }

    private void UpdateBlindImage()
    {
        if (skill.IsInState<ReadyState>())
        {
            if (!skill.IsUseable)
                blindImage.gameObject.SetActive(true);
            else
                blindImage.gameObject.SetActive(false);
        }
    }

    private void UpdateInput()
    {
        if (skill.IsUseable && Input.GetKeyDown(useKeyCode))
        {
            skill.Owner.SkillSystem.CancelTargetSearching();
            skill.Use();
        }
    }

    private void SetSkillUIActive(bool isOn)
    {
        cooldownText.gameObject.SetActive(isOn);
        blindImage.gameObject.SetActive(isOn);
        iconImage.gameObject.SetActive(isOn);
        remainInputCountText.gameObject.SetActive(isOn);
        usingBorderImage.gameObject.SetActive(isOn);
    }

    private void OnSkillStateChanged(Skill skill, State<Skill> currentState, State<Skill> prevState, int layer)
    {
        var stateType = currentState.GetType();

        if (layer == 0)
        {
            if (stateType != typeof(ReadyState))
                usingBorderImage.gameObject.SetActive(true);
            else
                usingBorderImage.gameObject.SetActive(false);
        }

        if (stateType == typeof(CooldownState))
            StartCoroutine("ShowCooldown");
        else if (stateType == typeof(InActionState))
            StartCoroutine("ShowActionInfo");
    }

    private IEnumerator ShowActionInfo()
    {
        if (skill.ApplyCycle > 0f)
            blindImage.gameObject.SetActive(true);

        if (skill.ExecutionType == SkillExecutionType.Input)
        {
            remainInputCountText.gameObject.SetActive(true);
            skill.onCurrentApplyCountChanged += OnSkillCurrentApplyCountChanged;
            OnSkillCurrentApplyCountChanged(skill, skill.CurrentApplyCount, 0);
        }

        while (skill.IsInState<InActionState>())
        {
            if (blindImage.gameObject.activeSelf)
                blindImage.fillAmount = 1f - (skill.CurrentApplyCycle / skill.ApplyCycle);

            if (skill.Duration > 0f)
                usingBorderImage.fillAmount = 1f - (skill.CurrentDuration / skill.Duration);

            yield return null;
        }

        if (!skill.IsInState<CooldownState>())
            blindImage.gameObject.SetActive(false);

        skill.onCurrentApplyCountChanged -= OnSkillCurrentApplyCountChanged;

        remainInputCountText.gameObject.SetActive(false);
        usingBorderImage.gameObject.SetActive(false);
        usingBorderImage.fillAmount = 1f;
    }

    private IEnumerator ShowCooldown()
    {
        blindImage.gameObject.SetActive(true);
        cooldownText.gameObject.SetActive(true);

        while (skill.IsInState<CooldownState>())
        {
            cooldownText.text = skill.CurrentCooldown.ToString("F1");
            blindImage.fillAmount = skill.CurrentCooldown / skill.Cooldown;
            yield return null;
        }

        blindImage.gameObject.SetActive(false);
        blindImage.fillAmount = 1f;

        cooldownText.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skill)
            SkillTooltip.Instance.Show(skill);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (skill)
            SkillTooltip.Instance.Hide();
    }

    private void OnSkillCurrentApplyCountChanged(Skill skill, int currentApplyCount, int prevApplyCount)
        => remainInputCountText.text = (skill.ApplyCount - currentApplyCount).ToString();
}
