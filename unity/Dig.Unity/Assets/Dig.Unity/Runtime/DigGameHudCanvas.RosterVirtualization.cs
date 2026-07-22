using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Agents;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private const int ResidentRowPoolCapacity = 16;
    private const float ResidentVerticalPaddingHeight = 7f;
    private const float ResidentElementSpacing = 3f;
    private const float CompactResidentContentHeight = 27f;
    private const float ResidentStatusHeight = 20f;
    private const float ResidentNeedMetricHeight = 18f;
    private const int ResidentNeedMetricCount = 4;
    private const float ResidentTopSkillHeadingHeight = 16f;
    private const float ResidentTopSkillMetricHeight = 13f;
    private const int MaximumVisibleResidentSkills = 5;
    private const float CompactResidentRowHeight =
        ResidentVerticalPaddingHeight + CompactResidentContentHeight;
    private const float ResidentRowSpacing = 4f;

    private readonly List<ResidentRowSlot> _residentRowPool =
        new List<ResidentRowSlot>(ResidentRowPoolCapacity);
    private RectTransform? _residentTopSpacer;
    private RectTransform? _residentBottomSpacer;
    private ScrollRect? _rightScroll;

    private void RefreshResidentRows(ResidentRosterViewModel roster)
    {
        if (roster.Rows.Count == 0)
        {
            const string emptySignature = "residents:empty";
            if (string.Equals(
                emptySignature,
                _lastRosterSignature,
                StringComparison.Ordinal))
            {
                return;
            }

            _lastRosterSignature = emptySignature;
            ResetResidentRowPool();
            ClearChildren(_rightContent!);
            AddEmptyRosterMessage("No dwarfs");
            return;
        }

        int visibleCount = Math.Min(ResidentRowPoolCapacity, roster.Rows.Count);
        int offset = CalculateResidentWindowOffset(roster.Rows.Count, visibleCount);
        IReadOnlyList<ResidentRosterRowViewModel> window = roster.GetWindow(
            offset,
            visibleCount);
        string signature = BuildResidentWindowSignature(roster, window, offset);
        if (string.Equals(signature, _lastRosterSignature, StringComparison.Ordinal))
        {
            return;
        }

        _lastRosterSignature = signature;
        EnsureResidentRowPool();
        SetSpacerHeight(
            _residentTopSpacer!,
            CalculateRangeHeight(roster.Rows, 0, offset));
        SetSpacerHeight(
            _residentBottomSpacer!,
            CalculateRangeHeight(
                roster.Rows,
                offset + window.Count,
                roster.Rows.Count - offset - window.Count));
        for (int index = 0; index < _residentRowPool.Count; index++)
        {
            ResidentRowSlot slot = _residentRowPool[index];
            if (index >= window.Count)
            {
                slot.Root.gameObject.SetActive(false);
                slot.Signature = string.Empty;
                continue;
            }

            ResidentRosterRowViewModel resident = window[index];
            string rowSignature = BuildResidentRowSignature(resident);
            slot.Root.gameObject.SetActive(true);
            if (!string.Equals(slot.Signature, rowSignature, StringComparison.Ordinal))
            {
                BindResidentRow(slot, resident);
                slot.Signature = rowSignature;
            }
        }
    }

    private int CalculateResidentWindowOffset(int totalCount, int visibleCount)
    {
        int maximumOffset = Math.Max(0, totalCount - visibleCount);
        if (maximumOffset == 0 || _rightScroll == null)
        {
            return 0;
        }

        float progress = 1f - Mathf.Clamp01(_rightScroll.verticalNormalizedPosition);
        return Mathf.Clamp(
            Mathf.RoundToInt(progress * maximumOffset),
            0,
            maximumOffset);
    }

    private void EnsureResidentRowPool()
    {
        if (_residentRowPool.Count == ResidentRowPoolCapacity
            && _residentTopSpacer != null
            && _residentBottomSpacer != null)
        {
            return;
        }

        ResetResidentRowPool();
        ClearChildren(_rightContent!);
        _residentTopSpacer = CreateSpacer("Roster Top Spacer");
        for (int index = 0; index < ResidentRowPoolCapacity; index++)
        {
            _residentRowPool.Add(CreateResidentRowSlot(index));
        }

        _residentBottomSpacer = CreateSpacer("Roster Bottom Spacer");
    }

    private ResidentRowSlot CreateResidentRowSlot(int index)
    {
        RectTransform row = CreatePanel(
            $"Resident Pool {index}",
            _rightContent!,
            new Color(0.10f, 0.14f, 0.19f, 0.96f));
        LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
        element.flexibleHeight = 0f;
        VerticalLayoutGroup vertical = row.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(5, 5, 3, 4);
        vertical.spacing = 3f;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;
        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = row.GetComponent<Image>();
        return new ResidentRowSlot(row, element, button);
    }

    private void BindResidentRow(
        ResidentRowSlot slot,
        ResidentRosterRowViewModel resident)
    {
        slot.Root.name = $"Resident {resident.Id}";
        slot.Root.GetComponent<Image>().color = resident.IsExpanded
            ? new Color(0.16f, 0.28f, 0.40f, 0.98f)
            : new Color(0.10f, 0.14f, 0.19f, 0.96f);
        float height = ResidentRowHeight(resident);
        slot.Layout.preferredHeight = height;
        slot.Layout.minHeight = height;
        slot.Button.onClick.RemoveAllListeners();
        string residentId = resident.Id;
        slot.Button.onClick.AddListener(
            () => _interaction!.SelectResidentFromHud(residentId));
        ClearChildren(slot.Root);
        BuildResidentCompactRow(slot.Root, resident);
        if (resident.IsExpanded)
        {
            BuildResidentDetails(slot.Root, resident);
        }
    }

    private RectTransform CreateSpacer(string name)
    {
        RectTransform spacer = CreateRect(name, _rightContent!);
        spacer.gameObject.AddComponent<LayoutElement>();
        return spacer;
    }

    private static void SetSpacerHeight(RectTransform spacer, float height)
    {
        bool active = height > 0f;
        spacer.gameObject.SetActive(active);
        LayoutElement layout = spacer.GetComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
    }

    private static float CalculateRangeHeight(
        IReadOnlyList<ResidentRosterRowViewModel> rows,
        int offset,
        int count)
    {
        float height = 0f;
        for (int index = offset; index < offset + count; index++)
        {
            height += ResidentRowHeight(rows[index]) + ResidentRowSpacing;
        }

        return height;
    }

    private static float ResidentRowHeight(ResidentRosterRowViewModel resident)
    {
        return resident.IsExpanded
            ? CalculateExpandedResidentRowHeight(resident.Skills.TopFive.Count)
            : CompactResidentRowHeight;
    }

    private static float CalculateExpandedResidentRowHeight(int visibleSkillCount)
    {
        int skillCount = Math.Min(
            MaximumVisibleResidentSkills,
            Math.Max(0, visibleSkillCount));
        float height = ResidentVerticalPaddingHeight
            + CompactResidentContentHeight
            + ResidentStatusHeight
            + (ResidentNeedMetricHeight * ResidentNeedMetricCount)
            + (ResidentElementSpacing * ResidentNeedMetricCount + ResidentElementSpacing);
        if (skillCount == 0)
        {
            return height;
        }

        return height
            + ResidentTopSkillHeadingHeight
            + (ResidentTopSkillMetricHeight * skillCount)
            + (ResidentElementSpacing * (skillCount + 1));
    }

    private static string BuildResidentWindowSignature(
        ResidentRosterViewModel roster,
        IReadOnlyList<ResidentRosterRowViewModel> window,
        int offset)
    {
        return $"residents:{roster.Rows.Count}:{offset}:{roster.SelectedResidentId}|"
            + string.Join("|", window.Select(BuildResidentRowSignature));
    }

    private static string BuildResidentRowSignature(ResidentRosterRowViewModel row)
    {
        string skills = string.Join(
            ",",
            row.Skills.All.Select(value => $"{value.SkillId}:{value.Level}"));
        return $"{row.Id}:{row.Name}:{row.Version}:{row.IsExpanded}:"
            + $"{row.Sex}:{row.ScheduledActivity}:{row.IsIdleAtWork}:"
            + $"{row.Health.Value}:{row.Nutrition.Value}:{row.Alertness.Value}:"
            + $"{row.Mood.Value}:{row.Activity.Kind}:"
            + $"{row.Activity.ProgressCurrent}:{row.Activity.BlockReasonCode}:{skills}";
    }

    private void OnRightPanelScrolled(Vector2 position)
    {
        if (_rightTab == RightPanelTab.Residents)
        {
            _lastRosterSignature = string.Empty;
        }
    }

    private void ResetResidentRowPool()
    {
        _residentRowPool.Clear();
        _residentTopSpacer = null;
        _residentBottomSpacer = null;
    }

    private sealed class ResidentRowSlot
    {
        public ResidentRowSlot(
            RectTransform root,
            LayoutElement layout,
            Button button)
        {
            Root = root;
            Layout = layout;
            Button = button;
        }

        public RectTransform Root { get; }
        public LayoutElement Layout { get; }
        public Button Button { get; }
        public string Signature { get; set; } = string.Empty;
    }
}

}
