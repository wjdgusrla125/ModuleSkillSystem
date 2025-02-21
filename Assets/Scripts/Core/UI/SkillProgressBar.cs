using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillProgressBar : MonoBehaviour
{
    [SerializeField]
    private Image fillImage;
    [SerializeField]
    private TextMeshProUGUI timeText;

    private SkillSystem skillSystem;

    private void Start()
    {
        skillSystem = GameObject.FindGameObjectWithTag("Player").GetComponent<SkillSystem>();
        skillSystem.onSkillStateChanged += OnSkillStateChanged;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        skillSystem.onSkillStateChanged -= OnSkillStateChanged;
    }

    private void InfoUpdate(float currentTime, float maxTime)
    {
        fillImage.fillAmount = currentTime / maxTime;
        timeText.text = $"{currentTime:F1} : {maxTime:F1}";
    }

    private void OnSkillStateChanged(SkillSystem skillSystem, Skill skill,
        State<Skill> currentSkill, State<Skill> prevState, int layer)
    {
        if (skill.IsInState<CastingState>())
        {
            gameObject.SetActive(true);
            StartCoroutine("CastingProgressUpdate", skill);
            
        }
        else if (skill.IsInState<ChargingState>())
        {
            gameObject.SetActive(true);
            StartCoroutine("ChargingProgressUpdate", skill);
        }
    }

    private IEnumerator CastingProgressUpdate(Skill skill)
    {
        while (skill.IsInState<CastingState>())
        {
            InfoUpdate(skill.CurrentCastTime, skill.CastTime);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator ChargingProgressUpdate(Skill skill)
    {
        var defaultColor = fillImage.color;
        while (skill.IsInState<ChargingState>())
        {
            InfoUpdate(skill.CurrentChargeDuration, skill.ChargeDuration);
            if (skill.IsMinChargeCompleted)
                fillImage.color = Color.yellow;

            yield return null;
        }

        fillImage.color = defaultColor;
        gameObject.SetActive(false);
    }
}
