using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillTreeView : MonoSingleton<SkillTreeView>
{
    // Slot UI들의 부모 Object
    [SerializeField]
    private Transform root;
    [SerializeField]
    private TextMeshProUGUI titleText;

    [Header("Slot")]
    [SerializeField]
    private GameObject slotViewPrefab;
    [SerializeField]
    private Vector2 slotSize;
    // Slot 사이의 거리
    [SerializeField]
    private float spacing;

    [Header("Line")]
    [SerializeField]
    private GameObject linkLinePrefab;
    [SerializeField]
    private float lineWidth;

    private List<SkillTreeSlotView> slotViews = new();
    private List<RectTransform> linkLines = new();

    private Dictionary<SkillTreeSlotView, List<RectTransform>> linesBySlot = new();

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(Entity entity, SkillTree skillTree)
    {
        titleText.text = skillTree.DisplayName;

        var nodes = skillTree.GetSlotNodes();

        ChangeViewWidth(nodes);
        CreateSlotViews(nodes.Length);
        PlaceSlotViews(entity, nodes);
        LinkSlotViews(entity, nodes);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ChangeViewWidth(SkillTreeSlotNode[] nodes)
    {
        var maxOverIndex = nodes.Max(x => x.Index) + 1;
        var width = ((slotSize.x + spacing) * maxOverIndex) + spacing;
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new(width, rectTransform.sizeDelta.y);
    }

    private void CreateSlotViews(int nodeCount)
    {
        int needSlotCount = nodeCount - slotViews.Count;
        for (int i = 0; i < needSlotCount; i++)
        {
            var slotView = Instantiate(slotViewPrefab, root).GetComponent<SkillTreeSlotView>();

            var rectTransform = slotView.GetComponent<RectTransform>();
            rectTransform.sizeDelta = slotSize;

            slotViews.Add(slotView);
        }
    }

    private RectTransform CreateLinkLine()
    {
        var linkLine = Instantiate(linkLinePrefab, root).GetComponent<RectTransform>();
        linkLine.transform.SetAsFirstSibling();

        linkLines.Add(linkLine);

        return linkLine;
    }

    private void PlaceSlotViews(Entity entity, SkillTreeSlotNode[] nodes)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            var slotView = slotViews[i];
            slotView.gameObject.SetActive(true);
            slotView.SetViewTarget(entity, node);

            // Slot이 위치해야할 좌표를 구함
            var x = (node.Index * slotSize.x);
            var y = (node.Tier * -slotSize.y);
            // 다른 Node와 공간을 두기위해 필요한 spacing 값을 구함
            var xSpacing = spacing * (node.Index + 1);
            var ySpacing = spacing * (node.Tier + 1);
            var position = new Vector3(x + xSpacing, y - ySpacing);

            slotView.transform.localPosition = position;
        }

        for (int i = nodes.Length; i < slotViews.Count; i++)
            slotViews[i].gameObject.SetActive(false);
    }

    private void LinkSlotViews(Entity entity, SkillTreeSlotNode[] nodes)
    {
        int nextLineIndex = 0;
        var halfSlotSize = slotSize * 0.5f;

        foreach ((var slotView, var lines) in linesBySlot)
            slotView.onSkillAcquired -= OnSkillAcquired;

        linesBySlot.Clear();

        for (int i = 1; i < nodes.Length; i++)
        {
            var node = nodes[i];
            var slotView = slotViews[i];

            foreach (var precedingNode in node.GetPrecedingSlotNodes())
            {
                var linkTargetSlot = slotViews.Find(x => x.SlotNode == precedingNode);
                var line = nextLineIndex < linkLines.Count ? linkLines[nextLineIndex] : CreateLinkLine();
                nextLineIndex++;

                if (entity.SkillSystem.Contains(precedingNode.Skill))
                    line.GetComponent<Image>().color = Color.yellow;
                else
                {
                    if (!linesBySlot.ContainsKey(linkTargetSlot))
                        linesBySlot[linkTargetSlot] = new List<RectTransform>();

                    linesBySlot[linkTargetSlot].Add(line);

                    linkTargetSlot.onSkillAcquired += OnSkillAcquired;
                }

                var linePosition = (linkTargetSlot.transform.localPosition + slotView.transform.localPosition) * 0.5f;
                linePosition.x += halfSlotSize.x;
                linePosition.y -= halfSlotSize.y;

                line.localPosition = linePosition;

                var direction = linkTargetSlot.transform.localPosition - slotView.transform.localPosition;
                float zAngle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;
                float lineHeight = direction.y;

                line.sizeDelta = new(lineWidth, lineHeight);
                line.transform.rotation = Quaternion.Euler(0f, 0f, (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f);
            }
        }

        for (int i = nextLineIndex; i < linkLines.Count; i++)
            linkLines[i].gameObject.SetActive(false);
    }

    private void OnSkillAcquired(SkillTreeSlotView slotView, Skill skill)
    {
        if (!linesBySlot.ContainsKey(slotView))
            return;

        foreach (var line in linesBySlot[slotView])
            line.GetComponent<Image>().color = Color.yellow;

        linesBySlot.Remove(slotView);
        slotView.onSkillAcquired -= OnSkillAcquired;
    }
}
