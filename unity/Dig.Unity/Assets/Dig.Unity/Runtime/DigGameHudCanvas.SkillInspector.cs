using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Presentation.Agents;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private static void BuildTopSkillList(
        Transform parent,
        ResidentSkillSetViewModel skills)
    {
        if (skills.TopFive.Count == 0)
        {
            return;
        }

        Text heading = CreateText(
            "Top Skills",
            parent,
            "TOP 5 SKILLS",
            10,
            TextAnchor.MiddleLeft);
        LayoutElement headingLayout = heading.gameObject.AddComponent<LayoutElement>();
        headingLayout.preferredHeight = ResidentTopSkillHeadingHeight;
        headingLayout.flexibleHeight = 0f;

        foreach (ResidentSkillViewModel skill in skills.TopFive)
        {
            CreateSkillMetric(parent, skill, "Top Skill ", useShortName: true);
        }
    }

    private static void BuildSkillInspector(
        Transform parent,
        ResidentSkillSetViewModel skills)
    {
        string capacity = $"Capacity {FormatPoints(skills.UsedCapacityUnits)} / "
            + $"{FormatPoints(skills.TotalCapacityUnits)} · University "
            + $"{FormatPoints(skills.UniversityProgressUnits)}/100";
        Text capacityText = CreateText(
            "Skill Capacity", parent, capacity, 10, TextAnchor.MiddleLeft);
        capacityText.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;

        foreach (ResidentSkillViewModel skill in skills.All)
        {
            CreateSkillMetric(parent, skill, "Skill ", useShortName: false);
        }

        Text report = CreateText(
            "Skill Report",
            parent,
            FormatSkillReport(skills.LastReport),
            9,
            TextAnchor.UpperLeft);
        report.horizontalOverflow = HorizontalWrapMode.Wrap;
        report.verticalOverflow = VerticalWrapMode.Truncate;
        report.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
    }

    private static void CreateSkillMetric(
        Transform parent,
        ResidentSkillViewModel skill,
        string rowPrefix,
        bool useShortName)
    {
        RectTransform row = CreateRect(rowPrefix + skill.SkillId, parent);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = ResidentTopSkillMetricHeight;
        rowLayout.flexibleHeight = 0f;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        Text name = CreateText(
            "Name",
            row,
            useShortName ? ShortSkill(skill.SkillId) : skill.SkillId,
            useShortName ? 9 : 7,
            TextAnchor.MiddleLeft);
        name.gameObject.AddComponent<LayoutElement>().preferredWidth = 135f;
        RectTransform bar = CreatePanel("Bar", row, new Color(0.03f, 0.05f, 0.10f, 1f));
        bar.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        float progress = skill.Level / (float)AgentSkillCatalog.IndividualMaximumUnits;
        Color color = Color.Lerp(
            new Color(0.05f, 0.14f, 0.42f, 1f),
            new Color(0.18f, 0.78f, 0.30f, 1f),
            progress);
        RectTransform fill = CreatePanel("Fill", bar, color);
        Anchor(fill, 0f, 0f, progress, 1f, 0f, 1f, 0f, -1f);
        Text value = CreateText(
            "Value", bar, $"{FormatPoints(skill.Level)}/100", 8,
            TextAnchor.MiddleCenter);
        Stretch(value.rectTransform, 0f, 0f, 0f, 0f);
        value.raycastTarget = false;
    }

    private static string FormatSkillReport(SkillRedistributionReport? report)
    {
        if (report is null)
        {
            return "Last result: none · Stonework thresholds 20/40/60";
        }

        string grants = string.Join(", ", report.Grants.Select(value =>
            $"{value.SkillId} "
            + $"req {FormatDiagnosticUnits(value.RequestedUnits)}/"
            + $"applied {FormatDiagnosticUnits(value.AppliedUnits)} "
            + $"free {FormatDiagnosticUnits(value.FreeCapacityUnits)}/"
            + $"rejected {FormatDiagnosticUnits(value.RejectedUnits)}"
            + (value.ReceivedRoundingUnit ? " +round" : string.Empty)));
        string donors = string.Join(", ", report.DonorLosses.Select(value =>
            $"{value.SkillId} "
            + $"weight {FormatDiagnosticUnits(value.ValueBeforeUnits)}/"
            + $"loss {FormatDiagnosticUnits(value.LossUnits)} "
            + $"remainder {value.FractionalRemainder}"
            + (value.ReceivedRoundingUnit ? " +round" : string.Empty)));
        return $"{report.SourceKind}:{report.SourceId} · {grants} · "
            + $"overflow {FormatDiagnosticUnits(report.OverflowUnits)} · "
            + $"donors [{donors}] · thresholds 20/40/60";
    }

    private static string FormatPoints(int units)
    {
        return (units / (float)AgentSkillCatalog.UnitsPerPoint).ToString("0.##");
    }

    private static string FormatDiagnosticUnits(int units)
    {
        return $"{FormatPoints(units)}p/{units}u";
    }
}

}
