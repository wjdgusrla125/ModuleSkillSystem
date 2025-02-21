using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillEffectListView : MonoBehaviour
{
    [SerializeField]
    private GameObject effectViewPrefab;

    private SkillSystem target;
    private List<SkillEffectView> effectViews = new();

    public SkillSystem Target
    {
        get => target;
        set
        {
            if (target == value)
                return;

            ReleaseEvents();
            DestroyEffectViews();

            target = value;
            if (target == null)
                return;

            target.onEffectStarted += OnEffectStarted;
            target.onEffectReleased += OnEffectReleased;

            foreach (var effect in target.RunningEffects)
            {
                if (!effect.IsReleased)
                    OnEffectStarted(target, effect);
            }
        }
    }

    private void OnDestroy()
    {
        ReleaseEvents();
        DestroyEffectViews();
    }

    private void ReleaseEvents()
    {
        if (target == null)
            return;

        target.onEffectStarted -= OnEffectStarted;
        target.onEffectReleased -= OnEffectReleased;
    }

    private void DestroyEffectViews()
    {
        foreach (var effectView in effectViews)
            Destroy(effectView.gameObject);

        effectViews.Clear();
    }

    private void OnEffectStarted(SkillSystem skillSystem, Effect effect)
    {
        if (!effect.IsShowInUI)
            return;

        var effectView = Instantiate(effectViewPrefab, transform).GetComponent<SkillEffectView>();
        effectView.Setup(effect);

        effectViews.Add(effectView);
    }

    private void OnEffectReleased(SkillSystem skillSystem, Effect effect)
    {
        int targetIndex = effectViews.FindIndex(x => x.Target == effect);
        if (targetIndex != -1)
        {
            Destroy(effectViews[targetIndex].gameObject);
            effectViews.RemoveAt(targetIndex);
        }
    }
}