using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillEffectView : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private Image blindImage;
    [SerializeField]
    private TextMeshProUGUI stackText;

    public Effect Target { get; private set; }

    private void OnDestroy()
    {
        if (!Target)
            return;
        
        Target.onStackChanged -= OnEffectStackChanged;
        Target.onReleased -= OnEffectReleased;
    }

    private void Update()
    {
        blindImage.fillAmount = Target.CurrentDuration / Target.Duration;
    }

    public void Setup(Effect viewEffect)
    {
        Target = viewEffect;
        iconImage.sprite = viewEffect.Icon;
        blindImage.fillAmount = 0f;
        stackText.gameObject.SetActive(false);

        Target.onStackChanged += OnEffectStackChanged;
        Target.onReleased += OnEffectReleased;

        OnEffectStackChanged(viewEffect, viewEffect.CurrentStack, 0);
    }

    private void OnEffectStackChanged(Effect effect, int currentStack, int prevStack)
    {
        if (effect.MaxStack == 1)
            return;

        stackText.text = currentStack.ToString();
        stackText.gameObject.SetActive(true);
    }

    private void OnEffectReleased(Effect effect)
        => Destroy(gameObject);
}
