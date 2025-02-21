using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class FloatingTextView : MonoSingleton<FloatingTextView>
{
    private class FloatingTextData
    {
        public TextMeshProUGUI TextMesh { get; private set; }
        public float CurrentDuration { get; set; }

        public FloatingTextData(TextMeshProUGUI textMesh)
            => TextMesh = textMesh;
    }

    private class FloatingTextGroup
    {
        public List<FloatingTextData> textDatas = new();

        public Transform TraceTarget { get; private set; }
        public RectTransform GroupTransform { get; private set; }
        public IReadOnlyList<FloatingTextData> TextDatas => textDatas;

        public FloatingTextGroup(Transform traceTarget, RectTransform groupTransform)
            => (TraceTarget, GroupTransform) = (traceTarget, groupTransform);

        public void AddData(FloatingTextData textData)
            => textDatas.Add(textData);

        public void RemoveData(FloatingTextData textData)
            => textDatas.Remove(textData);
    }

    [SerializeField]
    private RectTransform canvasTransform;

    [Space]
    [SerializeField]
    private GameObject textGroupPrefab;
    [SerializeField]
    private GameObject floatingTextPrefab;

    [Space]
    [SerializeField]
    private float floatingDuration;

    private readonly Dictionary<Transform, FloatingTextGroup> textGroupsByTarget = new();
    private readonly Queue<Transform> removeTargetQueue = new();
    private readonly Queue<FloatingTextData> removeTextDataQueue = new();

    private void LateUpdate()
    {
        foreach ((var traceTarget, var textGroup) in textGroupsByTarget)
        {
            UpdatePosition(textGroup);

            foreach (var textData in textGroup.TextDatas)
            {
                textData.CurrentDuration += Time.deltaTime;

                var color = textData.TextMesh.color;
                color.a = Mathf.Lerp(1f, 0f, textData.CurrentDuration / floatingDuration);
                textData.TextMesh.color = color;

                if (textData.CurrentDuration >= floatingDuration)
                    removeTextDataQueue.Enqueue(textData);
            }

            while (removeTextDataQueue.Count > 0)
            {
                var targetTextData  = removeTextDataQueue.Dequeue();

                Destroy(targetTextData.TextMesh.gameObject);

                textGroup.RemoveData(targetTextData);
            }

            if (textGroup.textDatas.Count == 0)
                removeTargetQueue.Enqueue(traceTarget);
        }

        while (removeTargetQueue.Count > 0)
        {
            var removeTarget = removeTargetQueue.Dequeue();

            Destroy(textGroupsByTarget[removeTarget].GroupTransform.gameObject);

            textGroupsByTarget.Remove(removeTarget);
        }
    }

    private void UpdatePosition(FloatingTextGroup group)
    {
        Vector2 viewportPosition = Camera.main.WorldToViewportPoint(group.TraceTarget.position);
        Vector2 uiPosition = (viewportPosition * canvasTransform.sizeDelta) - (canvasTransform.sizeDelta * 0.5f);

        group.GroupTransform.anchoredPosition = uiPosition;
    }

    public void Show(Transform traceTarget, string text, Color color)
    {
        var textGroup = CreateCachedGroup(traceTarget);

        var textMesh = Instantiate(floatingTextPrefab, textGroup.GroupTransform).GetComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.color = color;

        var newTextData = new FloatingTextData(textMesh);
        textGroup.AddData(newTextData);
    }

    public void Show(Transform traceTarget, string text)
        => Show(traceTarget, text, Color.white);

    private FloatingTextGroup CreateCachedGroup(Transform traceTarget)
    {
        if (!textGroupsByTarget.ContainsKey(traceTarget))
        {
            var group = Instantiate(textGroupPrefab, transform);
            var newTextGroup = new FloatingTextGroup(traceTarget, group.GetComponent<RectTransform>());
            textGroupsByTarget[traceTarget] = newTextGroup;

            UpdatePosition(newTextGroup);
        }

        return textGroupsByTarget[traceTarget];
    }
}
